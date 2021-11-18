using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PIVBase
{
    public class UnsupportedOpException : Exception
    {
    }

    public interface IPIVDevice
    {
        public string ManufacturerName() => "Unknown";
        public string DeviceType() => "Unknown";
        public string DeviceVersion() => "Unkown";
        public string Serial() => "Unknown";
        public CngKey GenerateNewKeyInSlot(PIVSlot slot, PIVAlgorithm algorithm);
        public X509Certificate2 GetRootAttestationCertificate();
        public X509Certificate2 GetAttestationForSlot(PIVSlot slot);
        public bool ImportCertificate(PIVSlot slot, X509Certificate2 certificate);
        public PIVSlotInfo GetSlotInfo(PIVSlot slot);
        public X509Certificate2 GetCertificate(PIVSlot slot);
        public X509SignatureGenerator GetX509SignatureGenerator(PIVSlot slot, PIVAlgorithm algorithm);
        public void Disconnect();
        public bool ChangePIN();
        public bool ChangePUK();
        public bool ChangeManagementKey();
    }
}
