using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PIVBase;
using Yubico.YubiKey.Piv;

namespace YubiKeyPIV
{
    public class YKPIVAlgorithmMap
    {
        public static readonly Dictionary<PIVAlgorithm, PivAlgorithm> map = new()
        {
            //{ PIVAlgorithm.None, PivAlgorithm.None},
            //{ PIVAlgorithm.TripleDes, PivAlgorithm.TripleDes},
            { PIVAlgorithm.EccP256, PivAlgorithm.EccP256 },
            { PIVAlgorithm.EccP384, PivAlgorithm.EccP384 },
            { PIVAlgorithm.Rsa2048, PivAlgorithm.Rsa2048 }
        };

        public static readonly Dictionary<PivAlgorithm, PIVAlgorithm> reverse = new()
        {
            //{ PIVAlgorithm.None, PivAlgorithm.None},
            //{ PIVAlgorithm.TripleDes, PivAlgorithm.TripleDes},
            { PivAlgorithm.EccP256, PIVAlgorithm.EccP256},
            { PivAlgorithm.EccP384, PIVAlgorithm.EccP384 },
            { PivAlgorithm.Rsa2048, PIVAlgorithm.Rsa2048 }
        };
    }
}

