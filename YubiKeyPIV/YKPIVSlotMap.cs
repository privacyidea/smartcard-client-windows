using System.Collections.Generic;
using PIVBase;
using Yubico.YubiKey.Piv;

namespace YubiKeyPIV
{
    class YKPIVSlotMap
    {
        public static readonly Dictionary<PIVSlot, byte> Map = new()
        {
            { PIVSlot.Authentication, PivSlot.Authentication },
            { PIVSlot.CardAuthentication, PivSlot.CardAuthentication },
            { PIVSlot.DigitalSignature, PivSlot.Signing },
            { PIVSlot.KeyManagement, PivSlot.KeyManagement }
        };

        public static readonly Dictionary<byte, PIVSlot> Reverse = new()
        {
            { PivSlot.Authentication, PIVSlot.Authentication },
            { PivSlot.Signing, PIVSlot.DigitalSignature },
            { PivSlot.CardAuthentication, PIVSlot.CardAuthentication },
            { PivSlot.KeyManagement, PIVSlot.KeyManagement },
        };
    }
}
