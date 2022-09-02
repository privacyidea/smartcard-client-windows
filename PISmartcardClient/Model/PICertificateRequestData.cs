using System;
using PIVBase;

namespace PISmartcardClient.Model
{
    [Serializable]
    public class PIPendingCertificateRequest
    {
        public PIVSlot Slot { get; }
        public string DeviceSerial { get; }
        public string DeviceManufacturer { get; }
        public string User { get; }
        public string TokenSerial { get; }
        public DateTime CreationTime { get; }
        public string CertificateRequest { get; }

        public PIPendingCertificateRequest(PIVSlot slot, string deviceSerial, string deviceManufacturer, string user, string tokenSerial, string certificateRequest)
        {
            Slot = slot;
            DeviceSerial = deviceSerial;
            User = user;
            TokenSerial = tokenSerial;
            CreationTime = DateTime.Now;
            CertificateRequest = certificateRequest;
            DeviceManufacturer = deviceManufacturer;
        }
    }

    public class PICertificateRequestData
    {
        public string CSR { get; }
        public string Attestation { get; }

        public PICertificateRequestData(string csr, string attestation)
        {
            Attestation = attestation;
            CSR = csr;
        }
    }
}
