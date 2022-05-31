using System;
using System.Configuration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PrivacyIDEASDK;
using static PIVBase.Utilities;

namespace PISmartcardClient
{
    // TODO bundle all PI-SDK calls to catch exceptions
    public class PIConfigurationException : Exception { }
    public sealed class PrivacyIDEAService : IPrivacyIDEAService, PILog
    {
        private static readonly string USER_AGENT = "privacyIDEA-Enrollment-Tool";
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
                bool? tmp = _SettingsService.GetBoolProperty("sslverify");
                bool sslverify = true;
                if (tmp.HasValue && !tmp.Value)
                {
                    sslverify = false;
                }
                _PrivacyIDEA = new(url, USER_AGENT, sslverify);
                _PrivacyIDEA.Logger = this;
            }
        }

        private void SettingChanging(object sender, SettingChangingEventArgs e)
        {
            Log("SettingChanging: " + e.SettingName + " to " + e.NewValue);

            if (e.SettingName is "url")
            {
                if (_PrivacyIDEA is null)
                {
                    _PrivacyIDEA = new((string)e.NewValue, USER_AGENT, _SettingsService.GetBoolProperty("sslverify") ?? true);
                    _PrivacyIDEA.Logger = this;
                }
                else
                {
                    _PrivacyIDEA.Url = (string)e.NewValue;
                }
            }

            if (e.SettingName is "sslverify")
            {
                if (_PrivacyIDEA is not null)
                {
                    _PrivacyIDEA.SSLVerify = (bool)e.NewValue;
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
                    Log(e);
                    exception = e;
                }
                catch (UriFormatException)
                {
                    Log("PrivacyIDEA URL is empty or in wrong format");
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

        async Task<string?> IPrivacyIDEAService.SendCSR(string csr, string attestation, string? description)
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
                Log(e);
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

            return response is not null ? response.Certificate : null;
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
            Log(exception);
        }
        #endregion PI_LOG
    }
}
