using System;
using System.Collections.Generic;
using System.Linq;
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

        async Task<bool> IPrivacyIDEAService.UserAuthentication()
        {
            Log("PrivacyIDEA Service: DoUserAuthentication");
            EnsurePISetup();

            // Default values to start with
            PIResponse? response = null;
            bool showUsernameInput = true;
            string? message = null;
            string? secondInputLabel = null;

            // Completed indicates success for both OTP and push threads. Bool get/set operations are atomic by default
            bool completed = false;
            string? user = null;
            CancellationTokenSource source = new();
            var pollToken = source.Token;
            // TransactionID has to be persisted through error responses
            string? transactionID = null;
            while (!completed)
            {
                // Overwrite defaults if there was a response
                if (response is not null)
                {
                    showUsernameInput = false;
                    message = response.Message;
                    secondInputLabel = "One-Time Password:";
                }

                (bool success, string? userInput, string? secondInput) = _WindowService.AuthenticationPrompt(message, showUsernameInput, secondInputLabel);
                if (!success)
                {
                    source.Cancel();
                    completed = false;
                    break;
                }
                // Persist the username for possible second step
                if (user is null && !string.IsNullOrEmpty(userInput))
                {
                    user = userInput;
                }

                // Try to authenticate with the inputs
                if (!string.IsNullOrEmpty(user) || response is not null)
                {
                    // Exit here if push authentication was completed
                    if (completed)
                    {
                        break;
                    }
                    CancellationToken? cToken = _WindowService.StartLoadingWindow("Authenticating...");
                    Exception? exception = null;

                    // Run the actual authentication in a separate thread so the loading window is animated
                    await Task.Run(async () =>
                    {
                        Task? t = null;
                        try
                        {
                            response = await _PrivacyIDEA!.Auth(user, secondInput ?? "", transactionID: transactionID, cancellationToken: cToken ?? default);

                            // Check if authentication is successful or if challenges have been triggered
                            if (!string.IsNullOrEmpty(response.AuthToken))
                            {
                                _CurrentAuthToken = response.AuthToken;
                                _CurrentUsername = user;
                                _PrivacyIDEA!.SetAuthorizationHeader(_CurrentAuthToken);
                                completed = true;
                                source.Cancel();
                                return;
                            }
                            else if (response.Challenges.Count > 0)
                            {
                                transactionID = response.TransactionID;
                                if (response.Challenges.Any(c => c.Type == "push"))
                                {
                                    // Run the polling for push in separate thread which has to be terminated if the authentication is completed otherwise
                                    string pollTransactionID = response.TransactionID;
                                    t = Task.Run(async () =>
                                    {
                                        while (!pollToken.IsCancellationRequested)
                                        {
                                            Thread.Sleep(250);
                                            bool pushCompleted = await _PrivacyIDEA.PollTransaction(pollTransactionID);
                                            if (pushCompleted) { break; }
                                        }
                                    });
                                    t.GetAwaiter().OnCompleted(async () =>
                                    {
                                        if (!pollToken.IsCancellationRequested)
                                        {
                                            var finalResponse = await _PrivacyIDEA.Auth(user, "", pollTransactionID);
                                            if (!string.IsNullOrEmpty(finalResponse.AuthToken))
                                            {
                                                _CurrentAuthToken = finalResponse.AuthToken;
                                                _CurrentUsername = user;
                                                _PrivacyIDEA!.SetAuthorizationHeader(_CurrentAuthToken);
                                                completed = true;
                                                _WindowService.CloseChildWindows();
                                            }
                                            else
                                            {
                                                Error($"Authentication with push token could not be completed. Check the server response. Error:{finalResponse.ErrorMessage}");
                                            }
                                        }
                                        Log("Poll thread terminating");
                                    });
                                }
                            }
                            // The /auth endpoint response is not well defined. It returns an error message even when challenges are triggered.
                            // So it is only truly an error if the error is present and there were no challenges triggered
                            else if (!string.IsNullOrEmpty(response.ErrorMessage) && response.Challenges.Count == 0)
                            {
                                // Append the error message to the general message which will be displayed in the prompt anyway
                                response.Message += $"\n{response.ErrorMessage} ({response.ErrorCode})";
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
                        catch (Exception e)
                        {
                            Error($"Unknown error:{e.Message}\n{e.StackTrace}");
                            UpdateStatus($"An unknown error occured: {e.Message}");
                            exception = e;
                        }
                    });
                    // Guarantee that the loading window is closed by the owner thread then rethrow so the callee can handle the exception
                    _WindowService.StopLoadingWindow();

                    if (exception is not null)
                    {
                        throw exception;
                    }
                }
                else
                {
                    Log("Authentication: Username input was empty.");
                    UpdateStatus("Cannot authenticate with emtpy username!");
                    break;
                }
            }

            return completed;
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
            string? ca = _SettingsService.GetStringProperty("ca");
            if (ca is null)
            {
                Log("No ca configured, abort enrollment");
                return null;
            }
            try
            {
                response = await Task.Run(() => _PrivacyIDEA!.CertInit(_CurrentUsername, csr, attestation, ca, description, cToken));
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
