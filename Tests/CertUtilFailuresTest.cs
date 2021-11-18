using PISmartcardClient.Utilities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PIVBase;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Tests
{
    [TestClass]
    public class CertUtilFailuresTest
    {
        private Mock<IPIVDevice> _DeviceMock;

        [TestInitialize]
        public void Setup()
        {
            _DeviceMock = new Mock<IPIVDevice>();
        }
        
        [TestMethod]
        public void CreateCSRGenerationFailure()
        {
            _DeviceMock.Setup(m => m.GenerateNewKeyInSlot(PIVSlot.Authentication, PIVAlgorithm.EccP256)).Returns((CngKey)null);
            var ret = CertUtil.CreateCSRData(_DeviceMock.Object, "testSubject", PIVAlgorithm.EccP256, PIVSlot.Authentication, "testUser");
            ret.Should().BeNull();
        }

        [TestMethod]
        public void FormatCertBytesZeroLength()
        {
            byte[] b = new byte[0];
            var ret = CertUtil.FormatCertBytesForFile(b);
            ret.Should().Be("");
        }

        [TestMethod]
        public void ExtractCertFailure()
        {
            string s = "something that is not a certificate";
            var cert = CertUtil.ExtractCertificateFromResponse(s);
            cert.Should().BeNull();
        }

        /*[TestMethod]
        public void SignatureGeneratorFailureWithRSA ()
        {
            CngKeyCreationParameters ckcParams = new()
            {
                ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                KeyCreationOptions = CngKeyCreationOptions.None,
                KeyUsage = CngKeyUsages.AllUsages,
            };
            ckcParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));

            CngKey rsaKey = CngKey.Create(CngAlgorithm.Rsa, null, ckcParams);
            _DeviceMock.Setup(m => m.GenerateNewKeyInSlot(PIVSlot.Authentication, PIVAlgorithm.Rsa2048))
                       .Returns(rsaKey);
            _DeviceMock.Setup(m => m.GetX509SignatureGenerator(PIVSlot.Authentication, PIVAlgorithm.Rsa2048))
                       .Returns(new MockSignatureGenerator());

            var csrData = CertUtil.CreateCSRData(_DeviceMock.Object, "testSubject", PIVAlgorithm.Rsa2048, PIVSlot.Authentication, "testUser");

            csrData.Should().BeNull();
        }*/
    }

    /*public class MockSignatureGenerator : X509SignatureGenerator
    {
        public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
        {
            return new byte[] { 48, 13, 6, 9, 42, 134, 72, 134, 247, 13, 1, 1, 11, 5, 0 };
        }

        public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            // Return broken signature to cause error
            return new byte[0];
        }

        protected override PublicKey BuildPublicKey()
        {
            return null;
        }
    } */
}
