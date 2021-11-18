using System;
using System.Collections.Generic;

namespace PIVBase
{
    public class Values
    {
        // From BCrypt.Interop.Blobs
        public static readonly IEnumerable<byte> BCRYPT_ECDSA_PUBLIC_P256_MAGIC = BitConverter.GetBytes(0x31534345);
        public static readonly IEnumerable<byte> BCRYPT_ECDSA_PRIVATE_P256_MAGIC = BitConverter.GetBytes(0x32534345);
        public static readonly IEnumerable<byte> BCRYPT_ECDSA_PUBLIC_P384_MAGIC = BitConverter.GetBytes(0x33534345);
        public static readonly IEnumerable<byte> BCRYPT_ECDSA_PRIVATE_P384_MAGIC = BitConverter.GetBytes(0x34534345);
        public static readonly IEnumerable<byte> BCRYPT_ECDSA_PUBLIC_P521_MAGIC = BitConverter.GetBytes(0x35534345);
        public static readonly IEnumerable<byte> BCRYPT_ECDSA_PRIVATE_P521_MAGIC = BitConverter.GetBytes(0x36534345);

        public static readonly IEnumerable<byte> BCRYPT_RSAPUBLIC_MAGIC = BitConverter.GetBytes(0x31415352);
        public static readonly IEnumerable<byte> BCRYPT_RSAPRIVATE_MAGIC = BitConverter.GetBytes(0x32415352);
        public static readonly IEnumerable<byte> BCRYPT_RSAFULLPRIVATE_MAGIC = BitConverter.GetBytes(0x33415352);
        public static readonly IEnumerable<byte> BCRYPT_KEY_DATA_BLOB_MAGIC = BitConverter.GetBytes(0x4d42444b);

        public const string OID_DSA = "1.2.840.10040.4.1";
        public const string OID_RSA = "1.2.840.113549.1.1.1";
        public const string OID_ECC = "1.2.840.10045.2.1";
    }
}
