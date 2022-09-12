using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PrivacyIDEAClient
{
    public class PrivacyIDEA : IDisposable
    {
        public string Url { get; set; } = "";
        public string Realm { get; set; } = "";

        private bool _SSLVerify = true;
        public bool SSLVerify
        {
            get => _SSLVerify;
            set
            {
                if (value != _SSLVerify)
                {
                    _HttpClientHandler = new HttpClientHandler();
                    if (!value)
                    {
                        _HttpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                        _HttpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }
                    _HttpClient = new HttpClient(_HttpClientHandler);
                    _HttpClient.DefaultRequestHeaders.Add("User-Agent", _Useragent);
                    _SSLVerify = value;
                }
            }
        }

        private HttpClientHandler _HttpClientHandler;
        private HttpClient _HttpClient;
        private bool _DisposedValue;
        private string _Serviceuser, _Servicepass, _Servicerealm, _Useragent;
        private bool _LogServerResponse = true;
        public PILog Logger { get; set; }

        // The webauthn parameters should not be url encoded because they already have the correct format.
        private static readonly List<string> exludeFromURIEscape = new(new string[]
           { "credentialid", "clientdata", "signaturedata", "authenticatordata", "userhandle", "assertionclientextensions" });

        public List<string> logExcludedEndpoints = new(new string[]
           { "/validate/polltransaction", "/auth" });

        public PrivacyIDEA(string url, string useragent, bool sslVerify = true)
        {
            Url = url;
            _Useragent = useragent;

            _HttpClientHandler = new HttpClientHandler();
            if (!sslVerify)
            {
                _HttpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                _HttpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            _HttpClient = new HttpClient(_HttpClientHandler);
            _HttpClient.DefaultRequestHeaders.Add("User-Agent", useragent);
        }

        /// <summary>
        /// Trigger challenges for the given user using a service account.
        /// </summary>
        /// <param name="username">username to trigger challenges for</param>
        /// <param name="domain">optional domain which can be mapped to a privacyIDEA realm</param>
        /// <returns>PIResponse object or null on error</returns>
        public async Task<PIResponse> TriggerChallenges(string username, string domain = null, CancellationToken cancellationToken = default)
        {
            if (!GetAuthToken())
            {
                Error("Unable to trigger challenges without an auth token!");
                return null;
            }

            Dictionary<string, string> parameters = new()
            {
                { "user", username }
            };


            var response = await SendRequest("/validate/triggerchallenge", parameters, cancellationToken);
            PIResponse ret = PIResponse.FromJSON(response, this);
            return ret;
        }

        public async Task<string> GetToken(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
        {
            Log($"Getting token info with params:\n{string.Join(",", parameters)}");
            if (_HttpClient.DefaultRequestHeaders.Authorization == null)
            {
                Log("Unable to get token info without authorization header.");
                return null;
            }

            //if (!string.IsNullOrEmpty(realm))
            //{
            //    parameters.Add("realm", realm);
            //}

            var response = await SendRequest("/token", parameters, cancellationToken, method: "GET");
            return response;
        }

        public async Task<string> Auth(string user, string pass, string realm = default, CancellationToken cancellationToken = default)
        {
            Log("PrivacyIDEA Client Auth: user=" + user + ", pass=" + pass + ", realm=" + realm);
            Dictionary<string, string> dict = new()
            {
                { "username", user },
                { "password", pass }
            };

            if (!string.IsNullOrEmpty(realm))
            {
                dict.Add("realm", realm);
            }

            var response = await SendRequest("/auth", dict, cancellationToken);

            if (string.IsNullOrEmpty(response))
            {
                Error("/auth did not respond!");
                return null;
            }

            string token = "";
            try
            {
                dynamic root = JsonConvert.DeserializeObject(response);
                token = root.result.value.token;
            }
            catch (Exception)
            {
                Error("/auth response did not have the correct format or did not contain a token.\n" + response);
            }
            //Log("auth token: " + token);
            return token;
        }

        public void SetAuthorizationHeader(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token);
            }
            else
            {
                Log("Cannot set auth token to null!");
            }
        }

        public async Task<PIResponse> CertInit(string user, string csr, string attestation, string description = default,
            CancellationToken cancellationToken = default)
        {
            if (_HttpClient.DefaultRequestHeaders.Authorization == null)
            {
                Log("Cannot enroll Cert without authorization header.");
                return null;
            }

            Dictionary<string, string> parameters = new()
            {
                { "type", "certificate" },
                { "user", user },
                { "ca", "ca" },
                //{ "template", "user" },
                { "request", csr },
                { "attestation", attestation }
            };

            if (!string.IsNullOrEmpty(description))
            {
                parameters.Add("description", description);
            }

            var response = await SendRequest("/token/init", parameters, cancellationToken);
            var piresponse = PIResponse.FromJSON(response, this);
            return piresponse;
        }

        /// <summary>
        /// Check if the challenge for the given transaction id has been answered yet. This is done using the /validate/polltransaction endpoint.
        /// </summary>
        /// <param name="transactionid"></param>
        /// <returns>true if challenge was answered. false if not or error</returns>
        public async Task<bool> PollTransaction(string transactionid)
        {
            if (!string.IsNullOrEmpty(transactionid))
            {
                Dictionary<string, string> map = new()
                {
                    { "transaction_id", transactionid }
                };

                var response = await SendRequest("/validate/polltransaction", map, default(CancellationToken), new List<KeyValuePair<string, string>>(), "GET");
                if (string.IsNullOrEmpty(response))
                {
                    Error("/validate/polltransaction did not respond!");
                    return false;
                }
                bool ret = false;
                try
                {
                    dynamic root = JsonConvert.DeserializeObject(response);
                    ret = (bool)root?.result.value;
                }
                catch (Exception)
                {
                    Error("/validate/polltransaction response has wrong format or does not contain 'value'.\n" + response);
                }

                return ret;
            }
            Error("PollTransaction called with empty transaction id!");
            return false;
        }

        /// <summary>
        /// Authenticate using the /validate/check endpoint with the username and OTP value. 
        /// Optionally, a transaction id can be provided if authentication is done using challenge-response.
        /// </summary>
        /// <param name="user">username</param>
        /// <param name="otp">OTP</param>
        /// <param name="transactionid">optional transaction id to refer to a challenge</param>
        /// <param name="domain">optional domain which can be mapped to a privacyIDEA realm</param>
        /// <returns>PIResponse object or null on error</returns>
        public PIResponse ValidateCheck(string user, string otp, string transactionid = null, string domain = null)
        {
            Dictionary<string, string> parameters = new()
            {
                { "user", user },
                { "pass", otp }
            };

            if (transactionid != null)
            {
                parameters.Add("transaction_id", transactionid);
            }

            var tResponse = SendRequest("/validate/check", parameters, default(CancellationToken), new List<KeyValuePair<string, string>>());
            var response = tResponse.GetAwaiter().GetResult();
            return PIResponse.FromJSON(response, this);
        }

        /// <summary>
        /// Authenticate at the /validate/check endpoint using a WebAuthn token instead of the usual OTP value.
        /// This requires the WebAuthnSignResponse and the Origin from the browser.
        /// </summary>
        /// <param name="user">username</param>
        /// <param name="transactionid">transaction id of the webauthn challenge</param>
        /// <param name="webAuthnSignResponse">the WebAuthnSignResponse string in json format as returned from the browser</param>
        /// <param name="origin">origin also returned by the browser</param>
        /// <param name="domain">optional domain which can be mapped to a privacyIDEA realm</param>
        /// <returns>PIResponse object or null on error</returns>
        public PIResponse ValidateCheckWebAuthn(string user, string transactionid, string webAuthnSignResponse, string origin, string domain = null)
        {
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(transactionid) || string.IsNullOrEmpty(webAuthnSignResponse) || string.IsNullOrEmpty(origin))
            {
                Log("ValidateCheckWebAuthn called with missing parameter: user=" + user + ", transactionid=" + transactionid
                    + ", WebAuthnSignResponse=" + webAuthnSignResponse + ", origin=" + origin);
                return null;
            }

            // Parse the WebAuthnSignResponse and add mandatory parameters
            JToken root;
            try
            {
                root = JToken.Parse(webAuthnSignResponse);
            }
            catch (JsonReaderException jex)
            {
                Error("WebAuthnSignRequest does not have the required format! " + jex.Message);
                return null;
            }

            string credentialid = (string)root["credentialid"];
            string clientdata = (string)root["clientdata"];
            string signaturedata = (string)root["signaturedata"];
            string authenticatordata = (string)root["authenticatordata"];

            Dictionary<string, string> parameters = new()
            {
                { "user", user },
                { "pass", "" },
                { "transaction_id", transactionid },
                { "credentialid", credentialid },
                { "clientdata", clientdata },
                { "signaturedata", signaturedata },
                { "authenticatordata", authenticatordata }
            };

            // Optionally add UserHandle and AssertionClientExtensions
            string userhandle = (string)root["userhandle"];
            if (!string.IsNullOrEmpty(userhandle))
            {
                parameters.Add("userhandle", userhandle);
            }

            string ace = (string)root["assertionclientextensions"];
            if (!string.IsNullOrEmpty(ace))
            {
                parameters.Add("assertionclientextensions", ace);
            }

            // The origin has to be set in the header for WebAuthn authentication
            List<KeyValuePair<string, string>> headers = new()
            {
                new KeyValuePair<string, string>("Origin", origin)
            };

            var tResponse = SendRequest("/validate/check", parameters, default(CancellationToken), headers);
            var response = tResponse.GetAwaiter().GetResult();
            return PIResponse.FromJSON(response, this);
        }

        /// <summary>
        /// Gets an auth token from the privacyIDEA server using the service account.
        /// Afterward, the token is set as the default authentication header for the HttpClient.
        /// </summary>
        /// <returns>true if success, false otherwise</returns>
        private bool GetAuthToken()
        {
            if (!ServiceAccountAvailable())
            {
                Error("Unable to fetch auth token without service account!");
                return false;
            }

            Dictionary<string, string> map = new()
            {
                { "username", _Serviceuser },
                { "password", _Servicepass }
            };

            if (!string.IsNullOrEmpty(_Servicerealm))
            {
                map.Add("realm", _Servicerealm);
            }

            var tResponse = SendRequest("/auth", map, default(CancellationToken));
            var response = tResponse.GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(response))
            {
                Error("/auth did not respond!");
                return false;
            }

            string token = "";
            try
            {
                dynamic root = JsonConvert.DeserializeObject(response);
                token = root.result.value.token;
            }
            catch (Exception)
            {
                Error("/auth response did not have the correct format or did not contain a token.\n" + response);
            }

            if (!string.IsNullOrEmpty(token))
            {
                _HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token);
                return true;
            }
            return false;
        }

        public void SetServiceAccount(string user, string pass, string realm = "")
        {
            _Serviceuser = user;
            _Servicepass = pass;
            if (!string.IsNullOrEmpty(realm))
            {
                _Servicerealm = realm;
            }
        }

        private Task<string> SendRequest(string endpoint, Dictionary<string, string> parameters, CancellationToken cancellationToken,
            List<KeyValuePair<string, string>> headers = null, string method = "POST")
        {
            Log("Sending [" + string.Join(" , ", parameters) + "] to [" + Url + endpoint + "] with method [" + method + "]");

            StringContent stringContent = DictToEncodedStringContent(parameters);

            HttpRequestMessage request = new();
            if (method == "POST")
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(Url + endpoint);
                request.Content = stringContent;
            }
            else
            {
                string s = stringContent.ReadAsStringAsync().GetAwaiter().GetResult();
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(Url + endpoint + "?" + s);
            }

            if (headers != null && headers.Count > 0)
            {
                foreach (KeyValuePair<string, string> element in headers)
                {
                    request.Headers.Add(element.Key, element.Value);
                }
            }

            var awaiter = _HttpClient.SendAsync(request, cancellationToken).GetAwaiter();

            HttpResponseMessage responseMessage = awaiter.GetResult();

            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                Error("The request to " + endpoint + " returned HttpStatusCode " + responseMessage.StatusCode);
                //return "";
            }

            string ret = "";
            try
            {
                ret = responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Error(e.Message);
            }

            if (_LogServerResponse && !string.IsNullOrEmpty(ret) && !logExcludedEndpoints.Contains(endpoint))
            {
                Log(endpoint + " response:\n" + JToken.Parse(ret).ToString(Formatting.Indented));
            }

            return Task.FromResult(ret);
        }

        internal StringContent DictToEncodedStringContent(Dictionary<string, string> dict)
        {
            StringBuilder sb = new();

            foreach (var element in dict)
            {
                sb.Append(element.Key).Append("=");
                sb.Append(exludeFromURIEscape.Contains(element.Key) ? element.Value : Uri.EscapeDataString(element.Value));
                sb.Append("&");
            }
            // Remove trailing &
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            string ret = sb.ToString();
            //Log("Built string: " + ret);
            return new StringContent(ret, Encoding.UTF8, "application/x-www-form-urlencoded");
        }

        internal bool ServiceAccountAvailable()
        {
            return !string.IsNullOrEmpty(_Serviceuser) && !string.IsNullOrEmpty(_Servicepass);
        }

        internal void Log(string message)
        {
            if (Logger is not null)
            {
                Logger.PILog(message);
            }
        }

        internal void Error(string message)
        {
            if (Logger is not null)
            {
                Logger.PIError(message);
            }
        }

        internal void Error(Exception exception)
        {
            if (Logger is not null)
            {
                Logger.PIError(exception);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_DisposedValue)
            {
                if (disposing)
                {
                    // Managed
                    _HttpClient.Dispose();
                    _HttpClientHandler.Dispose();
                }
                // Unmanaged
                _DisposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
