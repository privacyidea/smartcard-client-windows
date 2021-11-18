using PISmartcardClient;
using PISmartcardClient.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PIVBase;
using System.Collections.Generic;
using FluentAssertions;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using PISmartcardClient.Model;
using System.Security.Cryptography;
using Tests.TestUtils;
using PISmartcardClient.Utilities;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class MainVMSingleDevice
    {
        private Mock<IWindowService> _WindowServiceMock;
        private Mock<IPrivacyIDEAService> _PrivacyIDEAServiceMock;
        private Mock<IDeviceService> _DeviceServiceMock;
        private Mock<IPersistenceService> _PersistenceServiceMock;
        private Mock<IPIVDevice> _PIVDeviceMock;
        private MainVM _VM;
        private readonly List<IPIVDevice> _DeviceList = new();

        [TestInitialize]
        public void BasicSetup()
        {
            _WindowServiceMock = new Mock<IWindowService>();
            _PrivacyIDEAServiceMock = new Mock<IPrivacyIDEAService>();
            _DeviceServiceMock = new Mock<IDeviceService>();
            _PersistenceServiceMock = new Mock<IPersistenceService>();
            // Basic device behavior
            _PIVDeviceMock = new Mock<IPIVDevice>();
            _PIVDeviceMock.Setup(d => d.DeviceType()).Returns("MockDevice");
            _PIVDeviceMock.Setup(d => d.DeviceVersion()).Returns("1.0.0");
            _PIVDeviceMock.Setup(d => d.Serial()).Returns("123456");
            _PIVDeviceMock.Setup(d => d.ManufacturerName()).Returns("Mock Inc.");

            _DeviceList.Add(_PIVDeviceMock.Object);
            _DeviceServiceMock.Setup(obj => obj.GetAllDevices()).Returns(_DeviceList);

            _VM = new(_WindowServiceMock.Object, _PrivacyIDEAServiceMock.Object, _PersistenceServiceMock.Object,
                _DeviceServiceMock.Object, new TestDispatcher());

            // Assign manually because ComboBox is not doing it
            _VM.SelectedDevice = new(_DeviceList[0]);
        }

        [TestMethod]
        public void VerifyStartupState()
        {
            _VM.BtnChangeMgmtKey.Should().NotBeNull();
            _VM.BtnChangePIN.Should().NotBeNull();
            _VM.BtnChangePUK.Should().NotBeNull();
            _VM.BtnChangeSlot.Should().NotBeNull();
            _VM.BtnChangeUser.Should().NotBeNull();
            _VM.BtnComplete.Should().NotBeNull();
            _VM.BtnExport.Should().NotBeNull();
            _VM.BtnCreate.Should().NotBeNull();
            _VM.BtnReloadDevices.Should().NotBeNull();
            _VM.BtnSettings.Should().NotBeNull();

            _VM.ShowCenterControls.Should().BeFalse();
            _VM.ShowCompleteBtn.Should().BeFalse();
            _VM.ShowCreateBtn.Should().BeFalse();
            _VM.CurrentUserLabel.Should().Be("User: None");

            _VM.BtnChangeMgmtKey.CanExecute(null).Should().BeTrue();
        }

        [TestMethod]
        public void LoadCertificateFromSlot()
        {
            var cert = TestCertUtil.SelfSignedCert("CN=Test Certificate");
            _PIVDeviceMock.Setup(d => d.GetCertificate(PIVSlot.Authentication)).Returns(cert);

            // Verify device is set internally
            _VM.SelectedDevice.Device.Should().BeSameAs(_VM.CurrentDevice);
            _VM.NoSlotOrCertText.Should().Be("Please select a slot.");
            // This is what is shown in the combo box device selection
            _VM.SelectedDevice.Description.Should().Be("MockDevice 1.0.0 (123456)");
            _VM.SelectedDevice.Manufacturer.Should().Be("Mock Inc.");

            // Load a cert and verify the state of the controls
            _VM.BtnChangeSlot.Execute("Authentication");
            Thread.Sleep(500);
            _VM.CurrentSlotData.Should().NotBeNull();
            _VM.ShowCreateBtn.Should().BeTrue();
            _VM.ShowCenterControls.Should().BeTrue();
            _WindowServiceMock.Verify(m => m.StartLoadingWindow(It.IsAny<string>()), Times.Once);
            _WindowServiceMock.Verify(m => m.StartLoadingWindow(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void LoadEmptySlot()
        {
            _PIVDeviceMock.Setup(d => d.GetCertificate(PIVSlot.Authentication)).Returns((X509Certificate2)null);

            _VM.BtnChangeSlot.Execute("Authentication");
            Thread.Sleep(500);
            _VM.CurrentSlotData.Should().BeNull();
            _VM.ShowCreateBtn.Should().BeTrue();
            _VM.ShowCenterControls.Should().BeFalse();
            _VM.NoSlotOrCertText.Should().Be("There is currently no certificate in this slot.");

            _WindowServiceMock.Verify(m => m.StartLoadingWindow(It.IsAny<string>()), Times.Once);
            _WindowServiceMock.Verify(m => m.StartLoadingWindow(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void LoginAndLogoutUserWithPending()
        {
            _PrivacyIDEAServiceMock.Setup(m => m.IsConfigured()).Returns(true);
            _PrivacyIDEAServiceMock.SetupSequence(m => m.CurrentUser())
                                   .Returns((string)null) // First check if a user is already authenticated
                                   .Returns("TestUser") // Get the user that just authenticated
                                   .Returns("TestUser"); // Get user again for logout
            _PrivacyIDEAServiceMock.Setup(m => m.DoUserAuthentication()).ReturnsAsync(true);


            List<PICertificateRequestData> list = new()
            {
                new PICertificateRequestData(PIVSlot.Authentication, "123456", "TestUser", "CSRString", "AttestationString")
            };
            _PersistenceServiceMock.Setup(m => m.LoadData("TestUser")).Returns(list);

            _VM.PendingRolloutText.Should().BeNull();

            // Execute login button action
            _VM.CurrentSlot = PIVSlot.Authentication;
            _VM.BtnChangeUser.Execute(null);
            Thread.Sleep(500);
            _VM.CurrentUserLabel.Should().Be("User: TestUser");
            _VM.LoginSwitchBtnText.Should().Be("Logout");

            // Verify the pending rollout is shown
            _VM.PendingRolloutText.Should().NotBeNullOrEmpty();
            _VM.ShowCompleteBtn.Should().BeTrue();

            // Logout
            _VM.BtnChangeUser.Execute(null);
            Thread.Sleep(500);
            _VM.CurrentUserLabel.Should().Be("User: None");
            _VM.LoginSwitchBtnText.Should().Be("Login");
            _VM.PendingRolloutText.Should().BeNull();
        }

        [TestMethod]
        public void GenerateNewCertificate()
        {
            _WindowServiceMock.Setup(m => m.EnrollmentForm()).Returns((true, "testSubject", "EccP256"));

            string strCertFromResponse = "-----BEGIN CERTIFICATE-----\n" +
                         "MIIDrzCCAZegAwIBAgICEBkwDQYJKoZIhvcNAQELBQAwDTELMAkGA1UEAxMCY2Ew" +
                         "HhcNMjEwODIwMDgyNjA4WhcNMjIwODIwMDgyNjA4WjAPMQ0wCwYDVQQDEwRxcXFx" +
                         "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEcvH+Tq+sHxfy8fDTm+xpR6ZvfORR" +
                         "vOMlG4KtdM7n5F896mv0s+EkUlSCqS3sR0EY/+Q2eP76rt1aq/7HkDexR6OB4TCB" +
                         "3jALBgNVHQ8EBAMCBaAwCQYDVR0TBAIwADARBglghkgBhvhCAQEEBAMCBkAwMwYJ" +
                         "YIZIAYb4QgENBCYWJE9wZW5TU0wgR2VuZXJhdGVkIFNlcnZlciBDZXJ0aWZpY2F0" +
                         "ZTAdBgNVHQ4EFgQUZGgtSFfyyxdpZXBI56HikFMNEg0wSAYDVR0jBEEwP4AU2Kj+" +
                         "6Z4ubLGlZebDtmq/VVGMooShEaQPMA0xCzAJBgNVBAMTAmNhghR5jVM0zdN7ghvF" +
                         "DuRBHzWpYXpRkzATBgNVHSUEDDAKBggrBgEFBQcDATANBgkqhkiG9w0BAQsFAAOC" +
                         "AgEAvtyryJnBP80G9LWzTX8nEBWmF1uAq6GSqB4THMkd2lCunDaJini5R1imQEXG" +
                         "bI1dB5co3isG5+Hm1bBNGpTCgeSvs2AjKcATEtfm0TYh9p/s8xL+ubtYQo9ZBoQs" +
                         "VHbFYI5CYxDzR4gtABumrnJ2eCdELsLjlsOR3qzRNg92oiPAEz6eWnvM2bYOBXUU" +
                         "oeEmOdO4o42bFEjK0qOt34OZRhOEQXedAx5TbfAnocm6OS0xx15ikQRm5+z6V4ha" +
                         "aDdnV4/So4hJrnNKWwlY6d1k9ysk58Xyb6yR2yY1ee8LIuKbvsSDDLBnDQ5Fgrft" +
                         "FtbaLsKnFTyltYU4Smye3r07oCN9Xz8Cg5ac37JwSvCheXCU/jHKyv0YdK2iHcZ/" +
                         "WR43lRK5z/v5DolsJoFVfpt/YaZS1O3eq8J3tUvEpW18l9M73zGi8SHgtGAvjrYC" +
                         "dOgjYIZUPhpM/EjZbMrNLBXwmpP+dm9QhxlPO5P89uWiUVdERQZnyvKx9W7IhKvT" +
                         "b+nCCPvIBwRuceo/WpVyrWVXdgz6W87WtKdK5i8Zz3uShD72xOt42nSG2AIc3QFD" +
                         "lpNPT5swmMN+qFUlsbKVMo11yzfV1hFqukFdhMLlOIJtAvbd7/tpqIDJf3Ztkwrc" +
                         "PeS43DWrXA5EHpVCVu33W4hGW1KL9HJpUOPiBxSapY6PxTU=" +
                         "\n-----END CERTIFICATE-----";

            X509Certificate2 certFromResponse = CertUtil.ExtractCertificateFromResponse(strCertFromResponse);

            _PIVDeviceMock.SetupSequence(m => m.GetCertificate(PIVSlot.Authentication))
                           .Returns((X509Certificate2)null) // when loading first time
                           .Returns(certFromResponse); // after importing

            _PIVDeviceMock.Setup(m => m.GenerateNewKeyInSlot(PIVSlot.Authentication, PIVAlgorithm.EccP256))
                          .Returns(CngKey.Create(CngAlgorithm.ECDsaP256));

            _PIVDeviceMock.Setup(m => m.GetX509SignatureGenerator(PIVSlot.Authentication, PIVAlgorithm.EccP256))
                          .Returns(new EccP256SignatureGenerator());

            _PIVDeviceMock.Setup(m => m.GetAttestationForSlot(PIVSlot.Authentication))
                          .Returns(TestCertUtil.SelfSignedCert("AttestationDummy"));

            _PIVDeviceMock.Setup(m => m.ImportCertificate(PIVSlot.Authentication, It.IsAny<X509Certificate2>()))
                          .Returns(true);

            _PrivacyIDEAServiceMock.Setup(m => m.CurrentUser()).Returns("TestUser");
            _PrivacyIDEAServiceMock.Setup(m => m.IsConfigured()).Returns(true);
            _PrivacyIDEAServiceMock.Setup(m => m.SendCSR(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                                   .Returns(Task.FromResult(strCertFromResponse));

            // Select slot authentication which is currently empty
            _VM.BtnChangeSlot.Execute("Authentication");
            _VM.CurrentSlotData.Should().BeNull();

            // Generate a new certificate for the slot
            //_VM.CurrentSlot = PIVSlot.Authentication;
            _VM.BtnCreate.Execute(null);
            Thread.Sleep(1000);

            _PersistenceServiceMock.Verify(m => m.SaveCSR(It.IsAny<PICertificateRequestData>()));
            _VM.CurrentSlotData.Should().NotBeNull();
        }

        [TestMethod]
        public void AppClosing()
        {
            _VM.OnWindowClosing(new(), new());
            _PIVDeviceMock.Verify(m => m.Disconnect());
        }

        [TestMethod]
        public void ExportCert()
        {

        }
    }
}
