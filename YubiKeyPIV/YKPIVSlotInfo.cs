using System.Security.Cryptography;
using PIVBase;

namespace YubiKeyPIV
{
    class YKPIVSlotInfo : PIVSlotInfo
    {
        public YKPIVSlotInfo(PIVSlot slot, bool isDefault, PIVAlgorithm pivAlgorithm,
                                  bool isImported, CngKey publicKey)
                                  :
                                  base(slot, isDefault, pivAlgorithm, isImported, publicKey)
        {
        }
    }
}
