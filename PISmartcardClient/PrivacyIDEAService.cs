using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PrivacyIDEAClient;
using static PIVBase.Utilities;

namespace PISmartcardClient
{
    public class PIConfigurationException : Exception { }
    public sealed class PrivacyIDEAService : IPrivacyIDEAService, PILog
    {
        private static readonly string USER_AGENT = "privacyIDEA Smartcard Client";
        private readonly IWindowService _WindowService;
        private readonly ISettingsService _SettingsService;

        private string? _CurrentAuthToken;
        private string? _CurrentUsername;
        
        private Action<string?>? _UpdateStatusField;

        private PrivacyIDEA? _PrivacyIDEA;

        public PrivacyIDEAService(IWindowService windowService, ISettingsService settingsService)
        {
            _WindowService = windowService;
            _SettingsService = settingsService;

            string? url = _SettingsService.GetStringProperty("url");
            if (url is not null)
            {
                bool? disableSSL = _SettingsService.GetBoolProperty("disable_ssl");
                _PrivacyIDEA = new(url, USER_AGENT, !disableSSL ?? true)
                {
                    Logger = this
                };
                if (settingsService.GetStringProperty("realm") is string realm)
                {
                    Log($"Setting realm to {realm}");
                    _PrivacyIDEA.Realm = realm;
                }
            }
        }

        async Task<bool> IPrivacyIDEAService.DoUserAuthentication()
        {
            Log("PrivacyIDEA Service: DoUserAuthentication");
            EnsurePISetup();
            (bool success, string? user, string? secondInput) = _WindowService.AuthenticationPrompt();

            if (!success)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(user))
            {
                CancellationToken cToken = _WindowService.StartLoadingWindow("Authenticating...");
                Exception? exception = null;
                try
                {
                    string authToken = await Task.Run(() => _PrivacyIDEA!.Auth(user, secondInput ?? "", cancellationToken: cToken));
                    if (!string.IsNullOrEmpty(authToken))
                    {
                        _CurrentAuthToken = authToken;
                        _CurrentUsername = user;
                        _PrivacyIDEA!.SetAuthorizationHeader(_CurrentAuthToken);
                        return true;
                    }
                }
                catch (TaskCanceledException)
                {
                    Log("Authentication cancelled!");
                }
                catch (HttpRequestException e)
                {
                    Error(e);
                    exception = e;
                }
                catch (UriFormatException)
                {
                    Error("PrivacyIDEA URL is empty or in wrong format");
                    UpdateStatus("PrivacyIDEA URL is empty or in wrong format!");
                }
                finally
                {
                    _WindowService.StopLoadingWindow();
                }

                if (exception is not null)
                {
                    throw exception;
                }
            }
            else
            {
                Log("Authentication: Username input was empty.");
                UpdateStatus("Cannot authenticate with emtpy username!");
            }
            return false;
        }

        async Task<PIResponse?> IPrivacyIDEAService.SendCSR(string csr, string attestation, string? description)
        {
            EnsurePISetup();
            if (string.IsNullOrEmpty(_CurrentUsername))
            {
                throw new PIConfigurationException();
            }

            CancellationToken cToken = _WindowService.StartLoadingWindow("Sending the request to privacyIDEA...");
            PIResponse? response = null;
            Exception? exception = null;
            try
            {
                response = await Task.Run(() => _PrivacyIDEA!.CertInit(_CurrentUsername, csr, attestation, description, cToken));
            }
            catch (TaskCanceledException tce)
            {
                Log("Sending of the request cancelled!");
                exception = tce;
            }
            catch (HttpRequestException e)
            {
                Error(e);
                exception = e;
            }
            finally
            {
                _WindowService.StopLoadingWindow();
            }

            if (exception is not null)
            {
                throw exception;
            }

            return response;
        }

        async Task<List<PIToken>> IPrivacyIDEAService.GetTokenForCurrentUser(Dictionary<string, string> parameters)
        {
            string? response = null;
            Exception? exception = null;
            try
            {
                response = await Task.Run(() => _PrivacyIDEA!.GetToken(parameters));
            }
            catch (TaskCanceledException tce)
            {
                Log("Sending of the request cancelled!");
                exception = tce;
            }
            catch (HttpRequestException e)
            {
                Error(e);
                exception = e;
            }
            finally
            {
                _WindowService.StopLoadingWindow();
            }

            if (exception is not null)
            {
                throw exception;
            }

            var ret = new List<PIToken>();
            if (response is not null)
            {
                try
                {
                    JObject jobj = JObject.Parse(response);
                    JToken? jarr = jobj["result"]?["value"]?["tokens"];
                    if (jarr is not null)
                    {
                        JArray? token = jarr as JArray;
                        if (token is not null)
                        {
                            foreach (var element in token)
                            {
                                var tmp = PIToken.FromJSON(element.ToString(), Error);
                                if (tmp is not null)
                                {
                                    ret.Add(tmp);
                                }
                                else
                                {
                                    Error($"Could not parse input {element} to token, skipping");
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Error(e);
                }
            }

            return ret;
        }

        string? IPrivacyIDEAService.CurrentUser() => _CurrentUsername;

        void IPrivacyIDEAService.Logout()
        {
            _CurrentUsername = null;
            _CurrentAuthToken = null;
        }

        bool IPrivacyIDEAService.IsConfigured()
        {
            return _PrivacyIDEA is not null;
        }

        private void EnsurePISetup()
        {
            if (_PrivacyIDEA is null)
            {
                throw new PIConfigurationException();
            }
        }

        void IDisposable.Dispose()
        {
            _PrivacyIDEA?.Dispose();
        }

        void IPrivacyIDEAService.RegisterUpdateStatus(Action<string?> updateStatus)
        {
            _UpdateStatusField = updateStatus;
        }

        private void UpdateStatus(string message)
        {
            if (_UpdateStatusField is not null)
            {
                _UpdateStatusField.Invoke(message);
            }
        }

        #region PI_LOG
        public void PILog(string message)
        {
            Log(message);
        }

        public void PIError(string message)
        {
            Log("ERROR: " + message);
        }

        public void PIError(Exception exception)
        {
            Error(exception);
        }

        #endregion PI_LOG
    }
}
