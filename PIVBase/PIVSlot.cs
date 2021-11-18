using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIVBase
{
    public enum PIVSlot
    {
        None,
        Authentication,
        DigitalSignature,
        KeyManagement,
        CardAuthentication
    }

    /*public class PIVSlot
    {
        public const byte Pin = 128;
        public const byte Retired20 = 149;
        public const byte Retired19 = 148;
        public const byte Retired18 = 147;
        public const byte Retired17 = 146;
        public const byte Retired16 = 145;
        public const byte Retired15 = 144;
        public const byte Retired14 = 143;
        public const byte Retired13 = 142;
        public const byte Retired12 = 141;
        public const byte Retired11 = 140;
        public const byte Retired10 = 139;
        public const byte Retired9 = 138;
        public const byte Retired7 = 136;
        public const byte Retired8 = 137;
        public const byte KeyManagement = 157;
        public const byte Retired5 = 134;
        public const byte Retired4 = 133;
        public const byte Retired3 = 132;
        public const byte Retired2 = 131;
        public const byte Retired1 = 130;
        public const byte Attestation = 249;
        public const byte CardAuthentication = 158;
        public const byte Retired6 = 135;
        public const byte Signing = 156;
        public const byte Authentication = 154;
        public const byte Management = 155;
        public const byte Puk = 129;
    }*/
}
