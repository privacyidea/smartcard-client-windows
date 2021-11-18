using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PIVBase
{
    public abstract class PIVSlotInfo
    {
        public readonly PIVSlot PIVSlot;
        public readonly bool IsDefault;
        public readonly PIVAlgorithm PIVAlgorithm;
        public readonly bool IsImported;
        public readonly CngKey PublicKey;
        public PIVSlotInfo (PIVSlot pivSlot, bool isDefault, PIVAlgorithm pivAlgorithm, bool isImported, CngKey publicKey)
        {
            PIVSlot = pivSlot;
            IsDefault = isDefault;
            PIVAlgorithm = pivAlgorithm;
            IsImported = isImported;
            PublicKey = publicKey;
        }
    }
}
