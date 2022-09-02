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
        private readonly string PENDING_DIRECTORY = @"C:\Program Files\PrivacyIDEA Smartcard Client\Pending\";
        private IPersistenceService _PersistenceService = new PersistenceService();

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
            string tokenSerial = "TESTSERIAL001";
            string deviceSerial = "263423421";
            string deviceManufacturer = "TestDeviceManufacturer";
            var slot = PIVSlot.Authentication;

            var csrCert = TestCertUtil.SelfSignedCert("CSRCert");
            var attCert = TestCertUtil.SelfSignedCert("AttCert");
            string csrString = CertUtil.FormatCertBytesForFile(csrCert.RawData, true);
            string attString = CertUtil.FormatCertBytesForFile(attCert.RawData);

            PIPendingCertificateRequest data = new(slot, deviceSerial, deviceManufacturer, username, tokenSerial, csrString);

            bool res = _PersistenceService.SaveCSR(data);
            res.Should().BeTrue();

            var loadedList = _PersistenceService.LoadData("testUser");
            loadedList.Count.Should().Be(1);

            var loadedData = loadedList[0];
            loadedData.User.Should().Be(username);
            loadedData.DeviceSerial.Should().Be(deviceSerial);
            loadedData.CertificateRequest.Should().Be(csrString);
            loadedData.Slot.Should().Be(slot);
            loadedData.TokenSerial.Should().Be(tokenSerial);
            loadedData.CreationTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));

            res = _PersistenceService.Remove(loadedData);

            res.Should().BeTrue();

            File.Exists(PENDING_DIRECTORY + loadedData.User).Should().BeFalse();
        }

        [TestMethod]
        public void RemoveDataFailure()
        {
            // Try to remove a file that with a too long path, the path is PENDING_DIRECTORY + user
            PIPendingCertificateRequest data = new(PIVSlot.Authentication, "deviceSerial", "deviceManufacturer", user: Get250CharString(), "tokenSerial", "csr");
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
            PIPendingCertificateRequest data = new(PIVSlot.Authentication, "deviceSerial", "deviceManufacturer", Get250CharString(), "tokenSerial", "certificateRequest");

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
            string path = PENDING_DIRECTORY + Get250CharString() + "\\test.pem";

            bool res = _PersistenceService.ExportCertificate(cert, path);
            res.Should().BeFalse();
            // Can't check that file does not exists because the path causes an exception
        }

        private static string Get250CharString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < 250; i++)
            {
                sb.Append('a');
            }
            return sb.ToString();
        }
    }
}
