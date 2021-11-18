using PISmartcardClient;
using PISmartcardClient.Model;
using PISmartcardClient.Utilities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PIVBase;
using System;
using System.IO;
using System.Text;
using Tests.TestUtils;

namespace Tests
{
    [TestClass]
    public class PersistenceTest
    {
        private readonly string PENDING_DIRECTORY = @"C:\Program Files\PrivacyIDEA PIV Enrollment\Pending\";
        private IPersistenceService _PersistenceService = new PersistenceService();
        private string tooLongString = Get250CharString();

        [TestCleanup]
        public void ClearPendingDirectory()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(PENDING_DIRECTORY);

            foreach (FileInfo file in dirInfo.GetFiles())
            {
                file.Delete();
            }
        }

        [TestMethod]
        public void WriteReadRemoveTest()
        {
            string username = "testUser";
            string serial = "TESTSERIAL001";
            var slot = PIVSlot.Authentication;

            var csrCert = TestCertUtil.SelfSignedCert("CSRCert");
            var attCert = TestCertUtil.SelfSignedCert("AttCert");
            string csrString = CertUtil.FormatCertBytesForFile(csrCert.RawData, true);
            string attString = CertUtil.FormatCertBytesForFile(attCert.RawData);

            PICertificateRequestData data = new(slot, serial, username, csrString, attString);

            bool res = _PersistenceService.SaveCSR(data);
            res.Should().BeTrue();

            var loadedList = _PersistenceService.LoadData("testUser");
            loadedList.Count.Should().Be(1);

            var loadedData = loadedList[0];
            loadedData.User.Should().Be(username);
            loadedData.Attestation.Should().Be(attString);
            loadedData.CSR.Should().Be(csrString);
            loadedData.Slot.Should().Be(slot);
            loadedData.DeviceSerial.Should().Be(serial);
            loadedData.CreationTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));

            res = _PersistenceService.Remove(loadedData);

            res.Should().BeTrue();

            File.Exists(PENDING_DIRECTORY + loadedData.User).Should().BeFalse();
        }

        [TestMethod]
        public void RemoveDataFailure()
        {
            PICertificateRequestData data = new(PIVSlot.Authentication, "serial", tooLongString, "csr", "att");
            // Try to remove a file that with a too long path
            bool res = _PersistenceService.Remove(data);
            res.Should().BeFalse();
        }

        [TestMethod]
        public void ReadBrokenDataTest()
        {
            string username = "testUser";
            File.WriteAllText(PENDING_DIRECTORY + username, "");

            var loadedList = _PersistenceService.LoadData(username);
            loadedList.Count.Should().Be(0);

            File.Delete(PENDING_DIRECTORY + username);

            File.WriteAllText(PENDING_DIRECTORY + username, "this is not formatted properly");

            loadedList = _PersistenceService.LoadData(username);
            loadedList.Count.Should().Be(0);
        }

        [TestMethod]
        public void SaveDataTriggerLengthException()
        {
            // Cause a PathTooLongException with a path that is >250 chars
            PICertificateRequestData data = new(PIVSlot.Authentication, "serial", Get250CharString(), "csr", "att");

            bool res = _PersistenceService.SaveCSR(data);

            res.Should().BeFalse();
        }

        [TestMethod]
        public void ExportCert()
        {
            var cert = TestCertUtil.SelfSignedCert("test");
            string path = PENDING_DIRECTORY + "testCert.pem";
            bool res = _PersistenceService.ExportCertificate(cert, path);

            res.Should().BeTrue();
            File.Exists(path).Should().BeTrue();

            string content = File.ReadAllText(path);
            content.Should().Be(CertUtil.FormatCertBytesForFile(cert.RawData));
        }

        [TestMethod]
        public void ExportCertFailure()
        {
            var cert = TestCertUtil.SelfSignedCert("test");
            string path = PENDING_DIRECTORY + tooLongString + "\\test.pem";

            bool res = _PersistenceService.ExportCertificate(cert, path);
            res.Should().BeFalse();
            // Can't check that file does not exists because the path causes an exception
        }

        private static string Get250CharString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < 250; i++)
            {
                sb.Append("a");
            }
            return sb.ToString();
        }
    }
}
