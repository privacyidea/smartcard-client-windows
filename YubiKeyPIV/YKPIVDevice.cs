using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PIVBase;
using Yubico.YubiKey;
using Yubico.YubiKey.Piv;
using static PIVBase.Utilities;
using BerTlv;
using System.Threading;

namespace YubiKeyPIV
{
    // TODO ADD EXCEPTIONS
    public sealed class YKPIVDevice : IPIVDevice, IDisposable
    {
        private readonly IYubiKeyDevice _YK;
        private PivSession? _Session;
        private Func<KeyEntryData, bool> _KeyCollector;
        private readonly string _Manufacturer = "Yubico";
        private readonly string _DeviceType = "YubiKey";
        private readonly string _DeviceVersion;
        private readonly string _Serial;

        public YKPIVDevice(IYubiKeyDevice yubiKey, Func<KeyEntryData, bool> keyCollector)
        {
            _YK = yubiKey;
            _KeyCollector = keyCollector;
            _Serial = Convert.ToString(_YK.SerialNumber ?? 0);
            _DeviceVersion = _YK.FirmwareVersion.ToString();
        }

        string IPIVDevice.ManufacturerName() => _Manufacturer;
        string IPIVDevice.DeviceType() => _DeviceType;
        string IPIVDevice.DeviceVersion() => _DeviceVersion;
        string IPIVDevice.Serial() => _Serial;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility",
                                                         Justification = "Target is Windows only")]
        private CngKey? BuildRSAKey(Tlv tlv)
        {
            // TLV of a RSA Key (YubiKey encoding)
            // 7F49 L1 { 81 length modulus || 82 length public exponent }
            IEnumerable<byte> keyData = tlv.Value;

            IEnumerable<byte> keyLengthRSA2048 = new byte[] { 0x00, 0x08, 0x00, 0x00 }; // 256 byte
            IEnumerable<byte> moreMagic =
                new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01 };

            IEnumerable<byte> toImport = Values.BCRYPT_RSAPUBLIC_MAGIC.Concat(keyLengthRSA2048).Concat(moreMagic).Concat(keyData);
            byte[] keyBlob = toImport.ToArray();
            CngKey? cngKey = null;
            try
            {
                cngKey = CngKey.Import(keyBlob, format: CngKeyBlobFormat.GenericPublicBlob);
            }
            catch (Exception e)
            {
                Log("Importing PivPublicKey into CngKey failed!\n" + e.Message);
            }
            return cngKey;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility",
                                                         Justification = "Target is Windows only")]
        private CngKey? BuildEccKey(Tlv tlv, PivAlgorithm pivAlgorithm)
        {
            // TLV of an ECC Key (YubiKey encoding)
            // 7F49 L1 { 86 length public point }
            // where the public point is 04 || x-coordinate || y-coordinate

            // Skip the '04' (indicates the coordinates are signed values)
            IEnumerable<byte> keyData = tlv.Value.Skip(1);
            IEnumerable<byte> toImport;

            // Prepend the actual keyData with the "magic" that identifies the key type, also add the length
            if (pivAlgorithm == PivAlgorithm.EccP256)
            {
                IEnumerable<byte> keyLengthP256 = new byte[] { 0x20, 0x00, 0x00, 0x00 }; // 32 byte
                toImport = Values.BCRYPT_ECDSA_PUBLIC_P256_MAGIC.Concat(keyLengthP256);
            }
            else
            {
                IEnumerable<byte> keyLengthP384 = new byte[] { 0x30, 0x00, 0x00, 0x00 }; // 48 byte
                toImport = Values.BCRYPT_ECDSA_PUBLIC_P384_MAGIC.Concat(keyLengthP384);
            }

            toImport = toImport.Concat(keyData);

            CngKey? cngKey = null;
            try
            {
                byte[] keyBlob = toImport.ToArray();
                cngKey = CngKey.Import(keyBlob, format: CngKeyBlobFormat.EccPublicBlob);
            }
            catch (Exception e)
            {
                Log("Importing PivPublicKey into CngKey failed!\n" + e.Message);
            }
            return cngKey;
        }

        private static Tlv? ParseTLVEncodedData(byte[] ba)
        {
            try
            {
                var tlvs = Tlv.Parse(ba);
                return tlvs.ElementAt(0);
            }
            catch (ArgumentException)
            {
                Log("Data (" + ByteArrayToHexString(ba, true) + ") not in TLV format.");
            }
            return null;
        }

        private PivPublicKey? GenerateKeyPair(byte pivSlot, PivAlgorithm pivAlgorithm)
        {
            PivPublicKey? publicKey = null;
            try
            {
                // TODO PIN policies
                publicKey = _Session!.GenerateKeyPair(pivSlot, pivAlgorithm);
            }
            catch (ArgumentException arge)
            {
                // The slot or algorithm specified is not valid for generating a key pair.
                Log(arge.Message);
            }
            catch (InvalidOperationException ive)
            {
                // There is no KeyCollector loaded, the key provided was not a valid Triple-DES key,
                // or the YubiKey had some other error, such as unreliable connection.
                Log(ive.Message);
            }
            catch (OperationCanceledException oce)
            {
                // The user canceled management key collection.
                Log(oce.Message);
            }
            catch (System.Security.SecurityException se)
            {
                // Mutual authentication was performed and the YubiKey was not authenticated.
                Log(se.Message);
            }

            return publicKey;
        }

        private CngKey? PivPublicKeyToCngKey(PivPublicKey pivPublicKey, PivAlgorithm algorithm)
        {
            Log("YK PivPublicKeyToCngKey...");
            CngKey? cngKey = null;
            // YubiKey encoding does NOT have the nested TLV tag and is therefore 2 bytes shorter than PIV encoding
            var tlv = ParseTLVEncodedData(pivPublicKey.YubiKeyEncodedPublicKey.ToArray());

            if (tlv != null)
            {
                switch (algorithm)
                {
                    case PivAlgorithm.EccP256:
                    case PivAlgorithm.EccP384:
                        cngKey = BuildEccKey(tlv, algorithm);
                        break;
                    case PivAlgorithm.Rsa2048:
                        cngKey = BuildRSAKey(tlv);
                        break;
                    default:
                        Log("Cannot convert PivPublicKey with algorithm " + algorithm + " to CngKey!");
                        break;
                }
            }
            else
            {
                Log("Key data is not in TLV format!\n" + ByteArrayToHexString(pivPublicKey.PivEncodedPublicKey.ToArray(), true));
            }

            return cngKey;
        }

        bool IPIVDevice.ImportCertificate(PIVSlot slot, X509Certificate2 certificate)
        {
            Log("YK ImportCertificate...");
            EnsurePivSession();
            bool ret = false;
            try
            {
                _Session!.ImportCertificate(YKPIVSlotMap.Map[slot], certificate);
                ret = true;
            }
            catch (ArgumentException arge)
            {
                Log("The certificate argument is null");
                Error(arge);
            }
            catch (InvalidOperationException ive)
            {
                Log("There is no KeyCollector loaded, the key provided was not a valid Triple-DES key," +
                    " or the YubiKey had some other error, such as unreliable connection.");
                Error(ive);
            }
            catch (OperationCanceledException oce)
            {
                Log("The user canceled management key collection.");
                Error(oce);
            }
            catch (System.Security.SecurityException se)
            {
                Log("Mutual authentication was performed and the YubiKey was not authenticated.");
                Error(se);
            }
            return ret;
        }

        private void EnsurePivSession()
        {
            if (_Session is null)
            {
                _Session = new(_YK)
                {
                    KeyCollector = _KeyCollector
                };
                Log("Starting session with YubiKey " + _Serial);
            }
        }

        public void Dispose()
        {
            if (_Session != null)
            {
                try
                {
                    _Session.Dispose();
                }
                catch (Yubico.PlatformInterop.SCardException)
                {
                    Log("YubiKey was removed before session could be closed!");
                }
                _Session = null;
                Log("Closing session with YubiKey " + _Serial);
            }
        }

        CngKey? IPIVDevice.GenerateNewKeyInSlot(PIVSlot slot, PIVAlgorithm algorithm)
        {
            Log("YK GenerateNewKeyInSlot for slot " + slot.ToString("G") + ", algorithm: " + algorithm);
            EnsurePivSession();
            PivAlgorithm pivAlgorithm;
            try
            {
                pivAlgorithm = YKPIVAlgorithmMap.map[algorithm];
            }
            catch (Exception)
            {
                Log("No algorithm mapping found for PIVAlgorithm " + algorithm);
                return null;
            }

            PivPublicKey? pivPublicKey = GenerateKeyPair(YKPIVSlotMap.Map[slot], pivAlgorithm);
            if (pivPublicKey is null)
            {
                Log("YK Generating " + algorithm.ToString("G") + " Key in slot " + slot.ToString("G") + " failed!");
                return null;
            }

            // The data of PivPublicKey is TLV encoded and has to be manipulated
            // to be able to import it into a CngKey object.
            CngKey? cngKey = null;
            Tlv? tlv = ParseTLVEncodedData(pivPublicKey.YubiKeyEncodedPublicKey.ToArray());
            if (tlv is null)
            {
                Error("Unable to parse TLV of yubikey encoded public key!");
                return null;
            }

            if (pivAlgorithm is PivAlgorithm.EccP256 or PivAlgorithm.EccP384)
            {
                cngKey = BuildEccKey(tlv, pivAlgorithm);
            }
            else if (pivAlgorithm is PivAlgorithm.Rsa2048)
            {
                cngKey = BuildRSAKey(tlv);
            }
            else
            {
                Log("PIV algorithm used for which raw key data can not be transformed");
            }

            return cngKey;
        }

        public byte[]? Sign(byte[] data, PIVSlot slot)
        {
            EnsurePivSession();
            byte[]? ret = null;
            try
            {
                ret = _Session!.Sign(YKPIVSlotMap.Map[slot], data);
            }
            catch (ArgumentException ae)
            {
                Log(ae.Message);
            }
            catch (InvalidOperationException ioe)
            {
                Log(ioe.Message);
            }
            catch (OperationCanceledException oce)
            {
                Log(oce.Message);
            }

            return ret;
        }

        X509Certificate2? IPIVDevice.GetRootAttestationCertificate()
        {
            Log("YK GetAttestationCertificate...");
            EnsurePivSession();
            X509Certificate2? cert = null;
            try
            {
                cert = _Session!.GetAttestationCertificate();
                Log("YK Received Attestation Cert.");
            }
            catch (InvalidOperationException ioe)
            {
                // The YubiKey is pre-4.3, or there is no attestation certificate,
                // or it could not complete the task for some reason such as unreliable connection.
                // Also TLV exception possible if the AttestationCert has been replaced.
                Error(ioe);
            }
            return cert;
        }

        PIVSlotInfo? IPIVDevice.GetSlotInfo(PIVSlot slot)
        {
            Log("YK GetSlotInfo...");
            EnsurePivSession();

            if (_YK.FirmwareVersion < new FirmwareVersion(5, 3, 0))
            {
                Error($"YubiKey Firmware version is lower than 5.3.0. Actual version: {_YK.FirmwareVersion}. This fireware does not support getting information about slots.");
                return null;
            }

            PivMetadata meta;
            try
            {
                meta = _Session!.GetMetadata(YKPIVSlotMap.Map[slot]);
            }
            catch (ArgumentException)
            {
                Log("The slot specified is not valid for getting metadata.");
                return null;
            }
            catch (InvalidOperationException)
            {
                Log("The YubiKey queried does not support metadata," +
                    " or the operation could not be completed because of some error such as unreliable connection.");
                return null;
            }

            PivPublicKey? pivPublicKey = null;
            CngKey? cngKey = null;
            try
            {
                pivPublicKey = PivPublicKey.Create(meta.PublicKey.YubiKeyEncodedPublicKey);
            }
            catch (Exception)
            {
                Log("PublicKey from metadata has unsupported encoding!");
            }

            if (pivPublicKey != null)
            {
                cngKey = PivPublicKeyToCngKey(pivPublicKey, meta.Algorithm);
            }

            if (cngKey is null)
            {
                Log($"Unable to get public key for slot {slot:G}");
                return null;
            }
            YKPIVSlotInfo ykInfo = new(slot,
                                       meta.KeyStatus == PivKeyStatus.Default,
                                       YKPIVAlgorithmMap.reverse[meta.Algorithm],
                                       meta.KeyStatus == PivKeyStatus.Imported,
                                       cngKey);
            return ykInfo;
        }

        X509Certificate2? IPIVDevice.GetCertificate(PIVSlot slot)
        {
            Log("YK GetCertificate for slot " + slot.ToString("G"));
            EnsurePivSession();
            X509Certificate2? cert = null;
            try
            {
                cert = _Session!.GetCertificate(YKPIVSlotMap.Map[slot]);
            }
            catch (ArgumentException ae)
            {
                Log("The slot specified is not valid for getting a certificate.");
                Error(ae);
            }
            catch (InvalidOperationException)
            {
                Log("The slot did not contain a cert, or the YubiKey had some other error, such as unreliable connection.");
                //Log(ioe);
            }

            return cert;
        }

        X509Certificate2? IPIVDevice.GetAttestationForSlot(PIVSlot slot)
        {
            Log("GetAttestationForSlot " + slot.ToString("G"));
            EnsurePivSession();
            X509Certificate2? ret = null;

            try
            {
                ret = _Session!.CreateAttestationStatement(YKPIVSlotMap.Map[slot]);
                Log("Attestation received.");
            }
            catch (ArgumentException)
            {
                Log("The slot specified is not valid for creating an attestation statement.");
            }
            catch (InvalidOperationException)
            {
                Log("The YubiKey is pre-4.3, or there is no YubiKey-generated key in the slot, or the attestation key" +
                    "and cert were replaced with invalid values, or the YubiKey could not complete the task for some reason such as unreliable connection.");
            }

            return ret;
        }

        X509SignatureGenerator IPIVDevice.GetX509SignatureGenerator(PIVSlot slot, PIVAlgorithm algorithm)
        {
            return new YKSignatureGenerator(this, slot, algorithm);
        }

        void IPIVDevice.Disconnect()
        {
            Dispose();
        }

        bool IPIVDevice.ChangePIN()
        {
            EnsurePivSession();
            try
            {
                return _Session!.TryChangePin();
            }
            catch (Exception e)
            {
                Log(e.Message);
                return false;
            }
        }

        bool IPIVDevice.ChangePUK()
        {
            EnsurePivSession();
            try
            {
                return _Session!.TryChangePuk();
            }
            catch (Exception e)
            {
                Log(e.Message);
                return false;
            }
        }

        bool IPIVDevice.ChangeManagementKey()
        {
            EnsurePivSession();
            try
            {
                return _Session!.TryChangeManagementKey();
            }
            catch (Exception e)
            {
                Log(e.Message);
                return false;
            }
        }
    }
}
