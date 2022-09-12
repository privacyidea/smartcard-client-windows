using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PISmartcardClient.Utilities;
using PIVBase;
using PrivacyIDEAClient;
using PISmartcardClient.ExtensionMethods;
using static PIVBase.Utilities;
using PISmartcardClient.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Management;
using System.Diagnostics;

namespace PISmartcardClient.ViewModels
{
    public class MainVM : ObservableObject, PILog
    {
        public PIVSlot CurrentSlot = PIVSlot.None;

        private bool _ShowCenterControls;
        public bool ShowCenterGrid
        {
            get => _ShowCenterControls;
            set => SetProperty(ref _ShowCenterControls, value);
        }
        private bool _ShowCreateBtn;
        public bool ShowCreateBtn
        {
            get => _ShowCreateBtn;
            set => SetProperty(ref _ShowCreateBtn, value);
        }

        private string? _PendingRolloutText;
        public string? PendingRolloutText
        {
            get => _PendingRolloutText;
            set => SetProperty(ref _PendingRolloutText, value);
        }

        private bool _ShowCheckPendingBtn;
        public bool ShowCheckPendingBtn
        {
            get => _ShowCheckPendingBtn;
            set => SetProperty(ref _ShowCheckPendingBtn, value);
        }

        private string _LoginSwitchBtnText = "Login";
        public string LoginSwitchBtnText
        {
            get => _LoginSwitchBtnText;
            set => SetProperty(ref _LoginSwitchBtnText, value);
        }

        private string _NoSlotOrCertText = "Please select a slot.";
        public string NoSlotOrCertText
        {
            get => _NoSlotOrCertText;
            set => SetProperty(ref _NoSlotOrCertText, value);
        }

        private string? _Status;
        public string? Status
        {
            get => _Status;
            set
            {
                SetProperty(ref _Status, value);
                if (value != "")
                {
                    Task.Delay(10000).ContinueWith((t) => { Status = ""; });
                }
            }
        }

        private string? _CurrentUser;
        public string? CurrentUserLabel
        {
            get => _CurrentUser ?? "No User";
            set => SetProperty(ref _CurrentUser, value);
        }

        private SlotData? _CurrentSlotData;
        public SlotData? CurrentSlotData
        {
            get => _CurrentSlotData;
            set => SetProperty(ref _CurrentSlotData, value);
        }
        public ObservableCollection<PIVDevice> DeviceList { get; set; } = new();
        private PIVDevice? _SelectedDevice;
        public PIVDevice? SelectedDevice
        {
            get => _SelectedDevice;
            set
            {
                SetProperty(ref _SelectedDevice, value);
                if (value is not null)
                {
                    CurrentDevice = value.Device;
                }
            }
        }

        public DelegateCommand BtnNew { get; set; }
        public DelegateCommand BtnExport { get; set; }
        public DelegateCommand BtnChangeSlot { get; set; }
        public DelegateCommand BtnChangeUser { get; set; }
        public DelegateCommand BtnCheckPending { get; set; }
        public DelegateCommand BtnReloadDevices { get; set; }
        public DelegateCommand BtnChangePIN { get; set; }
        public DelegateCommand BtnChangePUK { get; set; }
        public DelegateCommand BtnSettings { get; set; }

        // Pending CSRs are attempted to be loaded upon user login or device insertion
        private List<PIPendingCertificateRequest>? _PendingCSRsForUserDevice;
        private PIPendingCertificateRequest? _PendingCSRForSlot;

        private IPIVDevice? _CurrentDevice;
        public IPIVDevice? CurrentDevice
        {
            get => _CurrentDevice;
            set
            {
                try
                {
                    _CurrentDevice?.Disconnect();
                }
                // TODO catch exceptions of other device types here
                catch (Yubico.PlatformInterop.SCardException)
                {
                    Status = "Device removed before connection could be closed!";
                }

                _CurrentDevice = value;

                // Reset controls
                ShowCenterGrid = false;
                ShowCreateBtn = false;
                CurrentSlot = PIVSlot.None;
                _CurrentSlotData = null;
                BtnChangeSlot.RaiseCanExecuteChanged();
                BtnChangePIN.RaiseCanExecuteChanged();
                BtnChangePUK.RaiseCanExecuteChanged();
                NoSlotOrCertText = _CurrentDevice is not null
                                   ? "Please select a slot."
                                   : "Please select a device.";
            }
        }

        private readonly IWindowService _WindowService;
        private readonly IPrivacyIDEAService _PrivacyIDEAService;
        private readonly IPersistenceService _PersistenceService;
        private readonly IDeviceService _DeviceService;
        private readonly IUIDispatcher _UIDispatcher;
        private readonly ISettingsService _SettingsService;

        private bool _DeviceInserted = true;
        private bool _DeviceRemoved = true;

        public MainVM(IWindowService windowService, IPrivacyIDEAService privacyIDEAService, ISettingsService settingsService,
            IPersistenceService persistenceService, IDeviceService deviceService, IUIDispatcher uiDispatcher)
        {
            _WindowService = windowService;
            _PrivacyIDEAService = privacyIDEAService;
            privacyIDEAService.RegisterUpdateStatus(new((message) => Status = message));

            _SettingsService = settingsService;
            _PersistenceService = persistenceService;
            _DeviceService = deviceService;
            _UIDispatcher = uiDispatcher;

            // Button Actions
            BtnChangeSlot = new(
                async (commandParameter) => await LoadSlot(commandParameter),
                (commandParameter) => _CurrentDevice is not null
                                   && CurrentSlot.ToString("G") != (string)commandParameter);

            BtnNew = new(
                async (_) => await GenerateNewButtonClick(),
                (_) => CurrentDevice is not null && _PendingCSRForSlot is null);

            BtnExport = new(
                async (_) => await ExportCertFromCurrentSlot(),
                (_) => CurrentDevice is not null
                    && _CurrentSlotData is not null
                    && CurrentSlot is not PIVSlot.None
                    && !string.IsNullOrEmpty(_CurrentSlotData.ExpirationDate));

            BtnChangeUser = new(async (_) => await LoginLogout());

            BtnCheckPending = new((_) =>
            {
                if (_PendingCSRForSlot is not null)
                {
                    CompleteCurrentPendingCSR();
                }
                else
                {
                    Error("Invalid state: Trying to complete rollout but no pending rollout is set.");
                }
            });

            BtnReloadDevices = new((commandParameter) => ReloadDevices());

            BtnChangePIN = new((_) =>
            {
                bool? success = CurrentDevice?.ChangePIN();
                Status = success.HasValue && success.Value ? "PIN change successful" : "PIN change cancelled or failed!";
            },
            (_) => CurrentDevice is not null);

            BtnChangePUK = new((_) =>
            {
                bool? success = CurrentDevice?.ChangePUK();
                Status = success.HasValue && success.Value ? "PUK change successful" : "PUK change cancelled or failed!";
            },
            (_) => CurrentDevice is not null);

            BtnSettings = new((_) => _WindowService.Settings());

            RegisterUSBWatchers();
            ReloadDevices();
        }

        private void RegisterUSBWatchers()
        {
            var insertionWatcher = new ManagementEventWatcher();
            insertionWatcher.EventArrived += new EventArrivedEventHandler(USBDeviceInserted);
            insertionWatcher.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
            insertionWatcher.Start();

            var removalWatcher = new ManagementEventWatcher();
            removalWatcher.EventArrived += new EventArrivedEventHandler(USBDeviceRemoved);
            removalWatcher.Query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
            removalWatcher.Start();
        }

        private void USBDeviceInserted(object sender, EventArrivedEventArgs args)
        {
            Log("Device inserted");
            if (_DeviceInserted)
            {
                Log("MOF=" + args.NewEvent.GetText(TextFormat.Mof));
                _DeviceInserted = false;
                CheckForPendingCSR();

                Task.Delay(300).ContinueWith((t) => _UIDispatcher.Invoke(() => ReloadDevices()));
                Task.Delay(300).ContinueWith((t) => _DeviceInserted = true);
            }
        }

        private void USBDeviceRemoved(object sender, EventArrivedEventArgs args)
        {
            Log("Device removed");
            if (_DeviceRemoved)
            {
                Log("MOF=" + args.NewEvent.GetText(TextFormat.Mof));
                _DeviceRemoved = false;
                ResetPendingCSRs();

                Task.Delay(300).ContinueWith((t) => _UIDispatcher.Invoke(() => ReloadDevices(true)));
                Task.Delay(300).ContinueWith((t) => _DeviceRemoved = true);
            }
        }

        private void ReloadDevices(bool deviceRemoved = false)
        {
            Log("Reloading devices...");
            // If a device is inserted, do not close the session with the device that is currently in use.
            // If a device is removed, it could be the one that is currently used so in that case it has to be checked.
            if (!deviceRemoved && SelectedDevice is not null)
            {
                var listcopy = DeviceList;
                for (int i = 0; i < listcopy.Count; i++)
                {
                    if (listcopy[i] != SelectedDevice)
                    {
                        DeviceList.Remove(DeviceList[i]);
                    }
                }
            }
            else
            {
                DeviceList.Clear();
            }

            var newDevices = _DeviceService.GetAllDevices();

            if (newDevices.Count > 0)
            {
                foreach (var d in newDevices)
                {
                    PIVDevice tmp = new(d);
                    if (SelectedDevice is not null && tmp.Description == SelectedDevice.Description)
                    {
                        continue;
                    }
                    DeviceList.Add(tmp);
                }

                if (SelectedDevice is null)
                {
                    if (DeviceList.Count > 1)
                    {
                        NoSlotOrCertText = "Please select a device!";
                    }
                    else if (DeviceList.Count == 1)
                    {
                        SelectedDevice = DeviceList[0];
                    }
                }
            }
            else
            {
                // Last device was removed, reset everything
                SelectedDevice = null;
                _CurrentDevice = null;
                ShowCenterGrid = false;
                ShowCreateBtn = false;
                CurrentSlot = PIVSlot.None;
                NoSlotOrCertText = "Please insert a device!";
            }

            BtnChangePIN.RaiseCanExecuteChanged();
            BtnChangePUK.RaiseCanExecuteChanged();
            BtnChangeSlot.RaiseCanExecuteChanged();
            BtnNew.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Loads all pending for the current user. If user is null, clears the UI.
        /// </summary>
        /// <param name="user"></param>
        private async void CompleteCurrentPendingCSR()
        {
            if (_PendingCSRForSlot is null)
            {
                Error("CompleteCurrentPendingCSR but there is none pending!");
                return;
            }
            Log("CompleteCurrentPendingCSR");

            string user = _PendingCSRForSlot.User;
            string serial = _PendingCSRForSlot.TokenSerial;

            Dictionary<string, string> parameters = new()
            {
                { "username", user },
                { "serial", serial }
            };

            var tokenList = await _PrivacyIDEAService.GetTokenForCurrentUser(parameters);
            bool completed = false;
            X509Certificate2? cert = null;
            if (tokenList.Count > 0)
            {
                PIToken token = tokenList[0];
                if (token.data.TryGetValue("rollout_state", out var rolloutState))
                {
                    // TODO what is the rollout state for enrolled token
                    if (rolloutState is not null && (string)rolloutState == "")
                    {
                        if (token.info.TryGetValue("certificate", out object? objCert) && objCert is string certStr)
                        {
                            cert = CertUtil.ExtractCertificateFromResponse(certStr);
                        }
                        else
                        {
                            Error("Response did not contain certificate!");
                        }
                    }
                }
            }

            if (cert is not null)
            {
                Log($"Certificate received from server.");
                bool proceed = _WindowService.ConfirmationPrompt($"Your certificate token {serial} is complete!\nDo you want to import it into slot {_PendingCSRForSlot.Slot:G} now?");
                if (proceed)
                {
                    try
                    {
                        _CurrentDevice!.ImportCertificate(_PendingCSRForSlot.Slot, cert);
                        Log("Import completed");
                        completed = true;
                    }
                    catch (Exception e)
                    {
                        Error(e);
                    }
                }
                else
                {
                    Log("Importing certificate cancelled by user.");
                }
            }

            if (completed)
            {
                string message = $"Completed rollout for the following token:\n\n{_PendingCSRForSlot.TokenSerial} in slot {_PendingCSRForSlot.Slot:G}";
                _PersistenceService.Remove(_PendingCSRForSlot);
                _WindowService.ConfirmationPrompt(message, false);
                await ReloadSlot();
            }
        }

        /// <summary>
        /// Performs Login or Logout depending on the current state.
        /// </summary>
        /// <returns>true if a new user if authenticated</returns>
        private async Task<bool> LoginLogout()
        {
            if (_PrivacyIDEAService.IsConfigured())
            {
                string? currUser = _PrivacyIDEAService.CurrentUser();
                if (currUser is null)
                {
                    bool authenticated = false;
                    try
                    {
                        authenticated = await _PrivacyIDEAService.DoUserAuthentication();
                    }
                    catch (HttpRequestException e)
                    {
                        Status = e.Message;
                    }

                    if (authenticated)
                    {
                        string newUser = _PrivacyIDEAService.CurrentUser()!;
                        CurrentUserLabel = newUser;
                        LoginSwitchBtnText = "Logout";
                        CheckForPendingCSR();
                    }
                    return authenticated;
                }
                else
                {
                    _PrivacyIDEAService.Logout();
                    CurrentUserLabel = null;
                    LoginSwitchBtnText = "Login";
                    ResetPendingCSRs();
                }
            }
            else
            {
                Error("Cannot authenticate without PrivacyIDEA configured.");
            }
            return false;
        }

        private void CheckForPendingCSR()
        {
            Log($"Checking for pending CSR for slot {CurrentSlot:G}");
            if (_PrivacyIDEAService.CurrentUser() is string username && CurrentSlot is not PIVSlot.None && _CurrentDevice is IPIVDevice curDevice)
            {
                var pendingForUser = _PersistenceService.LoadData(username);
                _PendingCSRsForUserDevice = pendingForUser.FindAll(item => item.DeviceSerial == curDevice.Serial());

                if (_PendingCSRsForUserDevice.Find(item => item.Slot == CurrentSlot) is PIPendingCertificateRequest pending)
                {
                    _PendingCSRForSlot = pending;
                    ShowCheckPendingBtn = true;
                    PendingRolloutText = $"There is a pending certificate request for this slot that was started {_PendingCSRForSlot.CreationTime}";
                    BtnNew.RaiseCanExecuteChanged();
                }
                else
                {
                    ShowCheckPendingBtn = false;
                    _PendingCSRForSlot = null;
                }
            }
        }

        private void ResetPendingCSRs()
        {
            Log("Resetting pending CSRs");
            _PendingCSRsForUserDevice = null;
            _PendingCSRForSlot = null;
            ShowCheckPendingBtn = false;
        }

        private async Task ExportCertFromCurrentSlot()
        {
            Log("Exporting certificate from slot " + CurrentSlot.ToString("G"));

            string? path = _WindowService.SaveFileDialog("Certificate File (PEM, CRT)|*.pem;*.crt", CurrentSlotData?.SerialNumber ?? "");

            if (string.IsNullOrEmpty(path))
            {
                Log("No path for export selected, aborting.");
                return;
            }

            _WindowService.StartLoadingWindow("Exporting...");
            bool success = false;
            await Task.Run(() =>
            {
                X509Certificate2? cert = CurrentDevice?.GetCertificate(CurrentSlot);
                if (cert is null)
                {
                    Error("No cert to export in selected slot");
                    return;
                }

                success = _PersistenceService.ExportCertificate(cert, path);
            });
            _WindowService.StopLoadingWindow();
            Status = success ? "Export successful!" : "Export failed.";
        }

        private async Task LoadSlot(object commandParameter)
        {
            PIVSlot slot = ((string)commandParameter).ToEnum<PIVSlot>();
            await LoadSlot(slot);
        }

        private async Task LoadSlot(PIVSlot slot)
        {
            Log($"Loading Slot {slot:G}");
            if (slot is not PIVSlot.None)
            {
                await Task.Run(() =>
                {
                    CurrentSlot = slot;
                    X509Certificate2? cert = CurrentDevice!.GetCertificate(slot);
                    CurrentSlotData = cert is not null
                        ? new SlotData
                        {
                            ExpirationDate = cert.NotAfter.ToShortDateString(),
                            DateOfIssue = cert.NotBefore.ToShortDateString(),
                            Thumbprint = cert.Thumbprint,
                            SerialNumber = cert.SerialNumber,
                            SubjectName = cert.SubjectName.Decode(X500DistinguishedNameFlags.None).Replace("CN=", ""),
                            Issuer = cert.Issuer.Replace("CN=", ""),
                            KeyType = new Oid(cert.GetKeyAlgorithm()).FriendlyName ?? "Unknown",
                            Certificate = cert
                        }
                        : null;

                    CheckForPendingCSR();

                    _UIDispatcher.Invoke(() =>
                    {
                        BtnChangeSlot.RaiseCanExecuteChanged();
                        BtnNew.RaiseCanExecuteChanged();
                        BtnExport.RaiseCanExecuteChanged();
                        ShowCreateBtn = true;
                        ShowCenterGrid = CurrentSlotData is not null;
                        if (!ShowCenterGrid)
                        {
                            NoSlotOrCertText = "There is currently no certificate in this slot.";
                        }
                    });
                });
            }
        }

        private async Task ReloadSlot()
        {
            if (CurrentSlot != PIVSlot.None)
            {
                await LoadSlot(CurrentSlot);
            }
        }

        private async Task GenerateNewButtonClick()
        {
            PILog("Generating new certificate for slot " + CurrentSlot.ToString("G"));
            if (CurrentSlot is PIVSlot.None)
            {
                Error("No PIV Slot selected!");
                return;
            }
            if (CurrentDevice is null)
            {
                Error("No device selected to generate new key on");
                return;
            }

            if (string.IsNullOrEmpty(_PrivacyIDEAService.CurrentUser()))
            {
                bool b = await LoginLogout();
                if (!b)
                {
                    Error("No user authenticated.");
                    return;
                }
            }
            string subjectName = _PrivacyIDEAService.CurrentUser()!;

            // TODO REMOVE
            subjectName += new Random().Next(10000);

            PIVSlot slot = CurrentSlot;
            string algo = "";
            bool success = false;
            if (_SettingsService.GetStringProperty("algorithm") is string settingsAlg)
            {
                Log($"Using fixed algorithm from settings: {settingsAlg}");
                algo = settingsAlg;
            }
            else
            {
                (success, string? formAlg) = _WindowService.EnrollmentForm();
                if (!success)
                {
                    Log("EnrollmentForm cancelled.");
                    return;
                }
                algo = formAlg!;
            }

            PIVAlgorithm pivAlgorithm = algo.ToEnum<PIVAlgorithm>();

            PICertificateRequestData? data = CertUtil.CreateCSRData(CurrentDevice, subjectName, pivAlgorithm, slot);
            if (data != null)
            {
                if (_PrivacyIDEAService.IsConfigured() && !string.IsNullOrEmpty(_PrivacyIDEAService.CurrentUser()))
                {
                    string description = $"{_CurrentDevice!.DeviceType()} {_CurrentDevice!.DeviceVersion()} (Serial {_CurrentDevice!.Serial()})";

                    PIResponse? response;
                    Log("Sending CSR to privacyIDEA...");
                    try
                    {
                        response = await _PrivacyIDEAService.SendCSR(data.CSR, data.Attestation, description);
                    }
                    catch (HttpRequestException e)
                    {
                        Status = e.Message;
                        Error(e);
                        return;
                    }
                    catch (TaskCanceledException)
                    {
                        Log("Sending of CSR cancelled.");
                        return;
                    }

                    if (response is null)
                    {
                        Error("No response received from the privacyIDEA server.");
                        Status = "No response received from the privacyIDEA server, please check your connection.";
                        return;
                    }

                    if (response!.RolloutState == "pending")
                    {
                        // Save the data to re-try later
                        PIPendingCertificateRequest pendingRequest = new(slot, CurrentDevice.Serial(), CurrentDevice.ManufacturerName(), subjectName, response.Serial, data.CSR);
                        _PersistenceService.SaveCSR(pendingRequest);
                        Log("Saved pending request.");
                        Status = "Certificate submission successful. The status is pending, check back later.";
                        CheckForPendingCSR();
                    }
                    else
                    {
                        // TODO check rollout state?
                        string? certStr = response!.Certificate;
                        // TODO check error
                        if (string.IsNullOrEmpty(certStr))
                        {
                            Log("No Cert received!");
                            Status = "Response from PI did not contain a certificate!";
                            return;
                        }

                        X509Certificate2? cert = CertUtil.ExtractCertificateFromResponse(certStr);
                        if (cert is not null)
                        {
                            success = CurrentDevice.ImportCertificate(slot, cert);
                            if (success)
                            {
                                Log("Import successful!");
                                Status = "Certificate import successful!";
                                await LoadSlot(slot);
                                _PendingCSRForSlot = null;
                                ShowCheckPendingBtn = false;
                                PendingRolloutText = null;
                            }
                            else
                            {
                                Status = "Importing the Certificate failed!";
                                Log("Import failed!");
                            }
                        }
                    }
                }
                else
                {
                    Error("PrivacyIDEA setup is not complete. Unable to enroll certificate.");
                }
            }
            else
            {
                Error("Failed to generate CSR.");
            }
        }

        public void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            //Log("OnWindowClosing");
            CurrentDevice?.Disconnect();
        }

        #region PI_LOG
        public void PILog(string message)
        {
            Log(message);
        }

        public void PIError(string message)
        {
            Error(message);
        }

        public void PIError(Exception exception)
        {
            Error(exception);
        }
        #endregion
    }
}
