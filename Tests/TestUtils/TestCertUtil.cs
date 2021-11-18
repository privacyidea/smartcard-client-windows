using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Tests.TestUtils
{
    public class TestCertUtil
    {
        public static X509Certificate2 SelfSignedCert(string cn)
        {
            if (!cn.StartsWith("CN="))
            {
                cn = "CN=" + cn;
            }
            var certRequest = new CertificateRequest(cn, ECDsa.Create(), HashAlgorithmName.SHA256);
            return certRequest.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(10));
        }
    }
}
