using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PIVBase;
using Yubico.YubiKey.Cryptography;
using static PIVBase.Utilities;

namespace YubiKeyPIV
{
    public class YKSignatureGenerator : X509SignatureGenerator
    {
        private YKPIVDevice _YK;
        private PIVSlot _Slot;
        private PIVAlgorithm _PIVAlgorithm;

        public YKSignatureGenerator(YKPIVDevice yk, PIVSlot slot, PIVAlgorithm algorithm)
        {
            _YK = yk;
            _Slot = slot;
            _PIVAlgorithm = algorithm;
        }

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            Log("GetSignatureAlgorithmIdentifier for " + _PIVAlgorithm.ToString("G"));
            byte[] oidSequence;

            // 48 (0x30) = SEQUENCE followed by length
            // 6 (0x06) = OID followed by length

            //const string RsaPkcs1Sha384 = "1.2.840.113549.1.1.12";
            //byte[] rsaPkcs1Sha384 = new byte[] { 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 12, 5, 0 };

            //const string RsaPkcs1Sha256 = "1.2.840.113549.1.1.11";
            byte[] rsaPkcs1Sha256 = new byte[] { 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 11, 5, 0 };

            //const string RsaPkcs1Sha512 = "1.2.840.113549.1.1.13";
            //byte[] rsaPkcs1Sha512 = new byte[] { 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 13, 5, 0 };

            // const string ecdsaWithSHA256 = "1.2.840.10045.4.3.2";
            byte[] ecdsaSHA256 = new byte[] { 0x30, 0x0A, 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03, 0x02 };

            // const string ecdsaWithSHA384 = "1.2.840.10045.4.3.3";
            byte[] ecdsaSHA384 = new byte[] { 0x30, 0x0A, 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03, 0x03 };

            if (_PIVAlgorithm is PIVAlgorithm.EccP256)
            {
                oidSequence = ecdsaSHA256;
            }
            else if (_PIVAlgorithm is PIVAlgorithm.EccP384)
            {
                oidSequence = ecdsaSHA384;
            }
            else if (_PIVAlgorithm is PIVAlgorithm.Rsa2048)
            {
                oidSequence = rsaPkcs1Sha256;
            }
            else
            {
                throw new InvalidOperationException("The PIV algorithm " + _PIVAlgorithm.ToString("G") + " has no matching signature algorithm OID.");
            }

            return oidSequence;
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            Log("SignData data=\n" + ByteArrayToHexString(data) + "\nData size=" + data.Length + ", PivAlgortithm=" + _PIVAlgorithm.ToString("G"));

            // Prepare the digest that should be signed according to the algorithm of the key
            byte[] digest;
            if (_PIVAlgorithm is PIVAlgorithm.Rsa2048)
            {
                digest = RsaFormat.FormatPkcs1Sign(SHA256.Create().ComputeHash(data), RsaFormat.Sha256, RsaFormat.KeySizeBits2048);
            }
            else if (_PIVAlgorithm is PIVAlgorithm.EccP256)
            {
                digest = SHA256.Create().ComputeHash(data);
            }
            else if (_PIVAlgorithm is PIVAlgorithm.EccP384)
            {
                digest = SHA384.Create().ComputeHash(data);
            }
            else
            {
                throw new ArgumentOutOfRangeException("The PIV algorithm " + _PIVAlgorithm.ToString("G") + " has no matching digest algorithm.");
            }

            Log("Digest=\n" + ByteArrayToHexString(digest) + "\nSize=" + digest.Length);

            byte[] signature = _YK.Sign(digest, _Slot);
            Log("Created signature:\n" + ByteArrayToHexString(signature));
            return signature;
        }

        protected override PublicKey BuildPublicKey()
        {
            Log("BuildPublicKey");
            return null;
        }
    }
}
