using System;
using PIVBase;

namespace PISmartcardClient.Model
{
    [Serializable]
    public class PICertificateRequestData
    {
        public PIVSlot Slot { get; }
        public string DeviceSerial { get; }
        public string User { get; }
        public string CSR { get; }
        public string Attestation { get; }
        public DateTime CreationTime { get; }

        // TODO add signature from device
        public PICertificateRequestData(PIVSlot slot, string deviceSerial, string user, string csr, string attestation)
        {
            Slot = slot;
            DeviceSerial = deviceSerial;
            User = user;
            CSR = csr;
            Attestation = attestation;
            CreationTime = DateTime.Now;
        }
    }
}
