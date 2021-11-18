using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Tests.TestUtils
{
    class EccP256SignatureGenerator : X509SignatureGenerator
    {
        ECDsa ecdsa = ECDsa.Create();

        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            return new byte[] { 0x30, 0x0A, 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03, 0x02 };
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            byte[] digest = SHA256.Create().ComputeHash(data);
            return ecdsa.SignData(digest, hashAlgorithm);
        }

        protected override PublicKey BuildPublicKey()
        {
            return null;
        }
    }
}
