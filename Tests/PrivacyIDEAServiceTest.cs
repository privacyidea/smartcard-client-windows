using PISmartcardClient;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Tests
{
    [TestClass]
    public class PrivacyIDEAServiceTest
    {
        private Mock<IWindowService> _WindowServiceMock;
        private Mock<ISettingsService> _SettingsServiceMock;
        private IPrivacyIDEAService _PrivacyIDEAService;
        private WireMockServer _WireMockServer;

        private string _Username = "Testuser";
        private string _Password = "Topsecret";
        private string _AuthToken = "ThisIsAShortendToken";

        [TestInitialize]
        public void Initialize()
        {
            _SettingsServiceMock = new();
            _WindowServiceMock = new();
            _WireMockServer = WireMockServer.Start();
            string url = _WireMockServer.Urls[0];
            _SettingsServiceMock.Setup(m => m.GetStringProperty("url"))
                                .Returns(url);

            _SettingsServiceMock.Setup(m => m.GetBoolProperty("sslverify"))
                                .Returns(false);

            _PrivacyIDEAService = new PrivacyIDEAService(_WindowServiceMock.Object, _SettingsServiceMock.Object);

            _SettingsServiceMock.Verify(m => m.GetStringProperty("url"), Times.Once);
            _SettingsServiceMock.Verify(m => m.GetBoolProperty("sslverify"), Times.Once);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _WireMockServer.Stop();
        }

        [TestMethod]
        public void TestUserAuthentication()
        {
            _WindowServiceMock.Setup(m => m.AuthenticationPrompt()).Returns((true, _Username, _Password));
            PrepAuthResponse();

            var task = _PrivacyIDEAService.DoUserAuthentication();
            bool res = task.GetAwaiter().GetResult();

            res.Should().BeTrue();
            _PrivacyIDEAService.CurrentUser().Should().Be(_Username);
        }

        private void PrepAuthResponse()
        {
            _WireMockServer.Given(
                   Request.Create()
                   .WithPath("/auth")
                   .UsingPost()
                   .WithBody(string.Format("username={0}&password={1}", _Username, _Password))
                   .WithHeader("Content-Type", "application/x-www-form-urlencoded; charset=utf-8"))
               .RespondWith(
                   Response.Create()
                   .WithStatusCode(200)
                   .WithBody("{\n" +
                               "    \"id\": 1,\n" +
                               "    \"jsonrpc\": \"2.0\",\n" +
                               "    \"result\": {\n" +
                               "        \"status\": true,\n" +
                               "        \"value\": {\n" +
                               "            \"log_level\": 20,\n" +
                               "            \"menus\": [\n" +
                               "                \"components\",\n" +
                               "                \"machines\"\n" +
                               "            ],\n" +
                               "            \"realm\": \"\",\n" +
                               "            \"rights\": [\n" +
                               "                \"policydelete\",\n" +
                               "                \"resync\"\n" +
                               "            ],\n" +
                               "            \"role\": \"admin\",\n" +
                               "            \"token\": \"" + _AuthToken + "\",\n" +
                               "            \"username\": \"admin\",\n" +
                               "            \"logout_time\": 120,\n" +
                               "            \"default_tokentype\": \"hotp\",\n" +
                               "            \"user_details\": false,\n" +
                               "            \"subscription_status\": 0\n" +
                               "        }\n" +
                               "    },\n" +
                               "    \"time\": 1589446794.8502703,\n" +
                               "    \"version\": \"privacyIDEA 3.2.1\",\n" +
                               "    \"versionnumber\": \"3.2.1\",\n" +
                               "    \"signature\": \"rsa_sha256_pss:\"\n" +
                               "}"));
        }

        private void PrepValidateCheckResponse()
        {
            _WireMockServer
               .Given(
                   Request.Create()
                   .WithPath("/validate/check")
                   .UsingPost()
                   .WithBody(string.Format("user={0}&pass={1}", _Username, _Password))
                   .WithHeader("Content-Type", "application/x-www-form-urlencoded; charset=utf-8"))
               .RespondWith(
                   Response.Create()
                   .WithStatusCode(200)
                   .WithBody("{\n" +
                       "\"detail\":" +
                       " {\n" +
                           "\"message\": \"matching 1 tokens\",\n" +
                           "\"otplen\": 6,\n" +
                           "\"serial\": \"PISP0001C673\",\n" +
                           "\"threadid\": 140536383567616,\n" +
                           "\"type\": \"totp\"\n" +
                       "},\n" +
                       "\"id\": 1,\n" +
                       "\"jsonrpc\": \"2.0\",\n" +
                       "\"result\": " +
                       "{\n" +
                           "\"status\": true,\n" +
                           "\"value\": true\n" +
                       "},\n" +
                       "\"time\": 1589276995.4397042,\n" +
                       "\"version\": \"privacyIDEA 3.2.1\",\n" +
                       "\"versionnumber\": \"3.2.1\",\n" +
                       "\"signature\": \"rsa_sha256_pss:AAAAAAAAAAA\"}"));
        }

        private void PrepCertInitResponse()
        {
            _WireMockServer
               .Given(
                   Request.Create()
                   .WithPath("/validate/check")
                   .UsingPost()
                   .WithBody(string.Format("user={0}&pass={1}", _Username, _Password))
                   .WithHeader("Content-Type", "application/x-www-form-urlencoded; charset=utf-8"))
               .RespondWith(
                   Response.Create()
                   .WithStatusCode(200)
                   .WithBody("{"
                            + "\"detail\": {"
                            + "\"certificate\": \"-----BEGIN CERTIFICATE-----\nMIIDszCCAZugAwIBAgICEDMwDQYJKoZIhvcNAQELBQAwDTELMAkGA1UEAxMCY2Ew\nHhcNMjExMDI2MTE0MDExWhcNMjIxMDI2MTE0MDExWjATMREwDwYDVQQDEwh0ZWFz\nZGFhYTBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABNm5EpleUQg1DFJG8c0lRY+z\nXBluH3H8hBKdM7dTJpRbsyX7A3x2dtAYz1yRzPdWgcq77D8+6pWZ1S3dKW67Hluj\ngeEwgd4wCwYDVR0PBAQDAgWgMAkGA1UdEwQCMAAwEQYJYIZIAYb4QgEBBAQDAgZA\nMDMGCWCGSAGG+EIBDQQmFiRPcGVuU1NMIEdlbmVyYXRlZCBTZXJ2ZXIgQ2VydGlm\naWNhdGUwHQYDVR0OBBYEFGodclsn0nZzkn4O7DisOy3cd5elMEgGA1UdIwRBMD+A\nFNio/umeLmyxpWXmw7Zqv1VRjKKEoRGkDzANMQswCQYDVQQDEwJjYYIUeY1TNM3T\ne4IbxQ7kQR81qWF6UZMwEwYDVR0lBAwwCgYIKwYBBQUHAwEwDQYJKoZIhvcNAQEL\nBQADggIBALPjuLdSMd3CzkwsAnPcjiZAwcpOD2v8Jv9RZGQMBP51O05wDYYiZZEe\nQlY1QvnevGfQtnFTVDT0D7cX1wqBhgZmkyVzOOUMnt2Zp2W1aZRui+UVgqRk2Taw\n3ANf6EvoXDnBDlrUkFy/pvyOzx2ivHYynquNTxtbo5OjazVC6RSmPsfkYciCuXKM\n45msw1NhUJyZul6rIQu4vFSoqzbh4PqD42fFZkJQgJxEDEDoyqWJ/AOBMCI/K8Si\nAeWBk4GvcvrnGBMqFoWUz9d1hAj9I4sD6HHtSVZGmn7ciFNHPtTPjP6GYPzr7C44\n8QByA7n8Rtv5FiYiOMFNj6V0W4HsKBqSplcBT9cbMBBC+MLssCWOJ4BwXGe+hPwn\nM+Ph6T08JpemoWWONvE1n0wA+h0/OohhMzyoYazkQRzBOY8+PXNVlo4VRfvykqC3\nTwQVoIlkArj+iuuUFpCF98FRCRvV4vUqCDEHfn6KHN5z6d87ZFQ1BrgjeCA8/By+\nwq4XGkKPpTCD3lkU23QJmniE7VoWPVnKWMBeOuBypH9Hh40yHrKYiPJF37FZTYV4\n+4BpMsNQc5wWG+0M5iyAbF3ifGu+DW7Lxn5vYA+QVAUNZm1FS3q0ZoUuiicSMdMT\nrpObwI878U/tpwk30M95/WCcc331/TfL1oV6uWEglQJK0VqoDcIs\n-----END CERTIFICATE-----\n\","
                            + "\"serial\": \"CRT00027C3C\","
                            + "\"threadid\": 139687338682112"
                            + "},"
                            + "\"id\": 1,"
                            + "\"jsonrpc\": \"2.0\","
                            + "\"result\": {"
                            + "\"status\": true,"
                            + "\"value\": true"
                            + "},"
                            + "\"time\": 1635248411.8678606,"
                            + "\"version\": \"privacyIDEA 3.6.2\","
                            + "\"versionnumber\": \"3.6.2\","
                            + "\"signature\": \"rsa_sha256_pss:\""
                            + "}"));
        }
    }
}
