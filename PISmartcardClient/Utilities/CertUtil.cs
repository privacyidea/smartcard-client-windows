using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PISmartcardClient.Model;
using PIVBase;
using static PIVBase.Utilities;

namespace PISmartcardClient.Utilities
{
    public class CertUtil
    {
        private static readonly string BEGIN_CERTIFICATE = "-----BEGIN CERTIFICATE-----\n";
        private static readonly string END_CERTIFICATE = "\n-----END CERTIFICATE-----";

        private static readonly string BEGIN_CSR = "-----BEGIN CERTIFICATE REQUEST-----\n";
        private static readonly string END_CSR = "\n-----END CERTIFICATE REQUEST-----";

        public static string FormatCertBytesForFile(byte[] certBytes, bool csr = false)
        {
            string ret = "";
            if (certBytes.Length > 0)
            {
                ret = (csr ? BEGIN_CSR : BEGIN_CERTIFICATE)
                    + InsertPeriodically(Convert.ToBase64String(certBytes), 64, "\n")
                    + (csr ? END_CSR : END_CERTIFICATE);
            }
            else
            {
                Log("FormatCertBytesForFile input was 0 bytes.");
            }
            return ret;
        }

        public static PICertificateRequestData? CreateCSRData(
            IPIVDevice device, string subjectName, PIVAlgorithm algorithm, PIVSlot slot)
        {
            CngKey? publicKey = device.GenerateNewKeyInSlot(slot, algorithm);
            if (publicKey == null)
            {
                Log("Failed to generate key on the device!");
                return null;
            }

            CertificateRequest csr;
            if (algorithm == PIVAlgorithm.Rsa2048)
            {
                RSACng rsa = new(publicKey);
                csr = new("cn=" + subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            else
            {
                ECDsaCng ecdsa = new(publicKey);
                csr = new("cn=" + subjectName, ecdsa, HashAlgorithmName.SHA256);
            }
           
            X509SignatureGenerator signatureGenerator = device.GetX509SignatureGenerator(slot, algorithm);
            byte[]? pkcs10Bytes = csr.CreateSigningRequest(signatureGenerator);
          
            string strCSR = FormatCertBytesForFile(pkcs10Bytes, true);
            if (string.IsNullOrEmpty(strCSR))
            {
                Log("Could not create CSR.");
                return null;
            }
            
            X509Certificate2 attCert = device.GetAttestationForSlot(slot);
            string attestation = new(PemEncoding.Write("CERTIFICATE", attCert.RawData));

            return new PICertificateRequestData(strCSR, attestation);
        }

        public static X509Certificate2 SelfSignedCert(string cn)
        {
            if (!cn.StartsWith("CN="))
            {
                cn = "CN=" + cn;
            }
            var certRequest = new CertificateRequest(cn, RSA.Create(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = certRequest.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(10));
            Log($"Generated Cert has private key: {cert.HasPrivateKey}");
            return cert;
        }

        public static X509Certificate2? ExtractCertificateFromResponse(string response)
        {
            response = response.Replace(BEGIN_CERTIFICATE, "")
                               .Replace(END_CERTIFICATE, "")
                               .Trim();
            try
            {
                X509Certificate2 cert = new(Convert.FromBase64String(response));
                return cert;
            }
            catch (Exception e)
            {
                Log("Failed to convert string to X509Certificate.");
                Error(e);
            }
            return null;
        }
    }
}
