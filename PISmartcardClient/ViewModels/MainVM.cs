using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PISmartcardClient.Utilities;
using PIVBase;
using PrivacyIDEASDK;
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

namespace PISmartcardClient.ViewModels
{
    public class MainVM : ObservableObject, PILog
    {
        public PIVSlot CurrentSlot = PIVSlot.None;

        private bool _ShowCenterControls;
        public bool ShowCenterControls
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

        private bool _ShowCompleteBtn;
        public bool ShowCompleteBtn
        {
            get => _ShowCompleteBtn;
            set => SetProperty(ref _ShowCompleteBtn, value);
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
            get => "User: " + (_CurrentUser ?? "None");
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

        public DelegateCommand BtnCreate { get; set; }
        public DelegateCommand BtnExport { get; set; }
        public DelegateCommand BtnSettings { get; set; }
        public DelegateCommand BtnChangeSlot { get; set; }
        public DelegateCommand BtnChangeUser { get; set; }
        public DelegateCommand BtnComplete { get; set; }
        public DelegateCommand BtnReloadDevices { get; set; }
        public DelegateCommand BtnChangePIN { get; set; }
        public DelegateCommand BtnChangePUK { get; set; }
        public DelegateCommand BtnChangeMgmtKey { get; set; }

        private List<PICertificateRequestData>? _PendingRolloutsForCurrentUser;
        private PICertificateRequestData? _PendingRollout;

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
                ShowCenterControls = false;
                ShowCreateBtn = false;
                CurrentSlot = PIVSlot.None;
                _CurrentSlotData = null;
                BtnChangeSlot.RaiseCanExecuteChanged();
                BtnChangePIN.RaiseCanExecuteChanged();
                BtnChangePUK.RaiseCanExecuteChanged();
                BtnChangeMgmtKey.RaiseCanExecuteChanged();
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

        private bool _DeviceInserted = true;
        private bool _DeviceRemoved = true;

        public MainVM(IWindowService windowService, IPrivacyIDEAService privacyIDEAService,
            IPersistenceService persistenceService, IDeviceService deviceService, IUIDispatcher uiDispatcher)
        {
            _WindowService = windowService;
            _PrivacyIDEAService = privacyIDEAService;
            privacyIDEAService.RegisterUpdateStatus(new((message) => Status = message));
            _PersistenceService = persistenceService;
            _DeviceService = deviceService;
            _UIDispatcher = uiDispatcher;

            // Button Actions
            BtnChangeSlot = new(
                async (commandParameter) => await LoadSlot(commandParameter),
                (commandParameter) => _CurrentDevice is not null
                                   && CurrentSlot.ToString("G") != (string)commandParameter);

            BtnCreate = new(
                async (_) => await GenerateNewButtonClick(),
                (_) => CurrentDevice is not null);

            BtnExport = new(
                async (_) => await ExportCertFromCurrentSlot(),
                (_) => CurrentDevice is not null
                    && _CurrentSlotData is not null
                    && CurrentSlot is not PIVSlot.None
                    && !string.IsNullOrEmpty(_CurrentSlotData.ExpirationDate));

            BtnSettings = new((_) => _WindowService.Settings());

            BtnChangeUser = new(async (_) => await LoginLogout());

            BtnComplete = new(async (_) =>
            {
                if (_PendingRollout is not null)
                {
                    await CompleteCertificateRollout(_PendingRollout!);
                }
                else
                {
                    Log("Invalid state: Trying to complete rollout but no pending rollout is set.");
                }
            });

            BtnReloadDevices = new((commandParameter) => ReloadDevices());

            BtnChangePIN = new((_) =>
            {
                bool? success = CurrentDevice?.ChangePIN();
                Status = success.HasValue && success.Value ? "PIN change successful" : "PIN change failed!";
            },
            (_) => CurrentDevice is not null);

            BtnChangePUK = new((_) =>
            {
                bool? success = CurrentDevice?.ChangePUK();
                Status = success.HasValue && success.Value ? "PUK change successful" : "PUK change failed!";
            },
            (_) => CurrentDevice is not null);

            BtnChangeMgmtKey = new((_) => { });

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
                ShowCenterControls = false;
                ShowCreateBtn = false;
                CurrentSlot = PIVSlot.None;
                NoSlotOrCertText = "Please insert a device!";
            }

            BtnChangePIN.RaiseCanExecuteChanged();
            BtnChangePUK.RaiseCanExecuteChanged();
            BtnChangeSlot.RaiseCanExecuteChanged();
            BtnCreate.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Loads all pending for the current user. If user is null, clears the UI.
        /// </summary>
        /// <param name="user"></param>
        private void LoadPendingEnrollmentsForUser(string? user)
        {
            if (user is not null)
            {
                _PendingRolloutsForCurrentUser = _PersistenceService.LoadData(user!);
                CheckPendingRolloutsForCurrentSlot();
            }
            else
            {
                _PendingRolloutsForCurrentUser = null;
                PendingRolloutText = null;
                ShowCompleteBtn = false;
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
                        LoadPendingEnrollmentsForUser(newUser);
                    }
                    return authenticated;
                }
                else
                {
                    _PrivacyIDEAService.Logout();
                    CurrentUserLabel = null;
                    LoginSwitchBtnText = "Login";
                    LoadPendingEnrollmentsForUser(null);
                }
            }
            else
            {
                Log("Cannot authenticate without PrivacyIDEA configured.");
            }
            return false;
        }

        private void CheckPendingRolloutsForCurrentSlot()
        {
            Log("CheckPendingRolloutsForCurrentSlot");
            if (CurrentDevice is not null && _PendingRolloutsForCurrentUser is not null)
            {
                string serial = CurrentDevice!.Serial();
                foreach (PICertificateRequestData data in _PendingRolloutsForCurrentUser)
                {
                    if (data.DeviceSerial == serial && data.Slot == CurrentSlot)
                    {
                        _PendingRollout = data;
                        ShowCompleteBtn = true;
                        PendingRolloutText = "Pending rollout found for this slot.\nStarted: " + data.CreationTime.ToLongDateString();
                    }
                }
            }
        }

        private async Task ExportCertFromCurrentSlot()
        {
            Log("Exporting certificate from slot " + CurrentSlot.ToString("G"));

            string? path = _WindowService.SaveFileDialog("Certificate File (PEM, CRT)|*.pem;*.crt");

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
                    Log("No cert to export in selected slot");
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
            if (slot is not PIVSlot.None)
            {
                _WindowService.StartLoadingWindow("Retrieving slot data...");

                await Task.Run(() =>
                {
                    CurrentSlot = slot;
                    X509Certificate2 cert = CurrentDevice!.GetCertificate(slot);

                    CurrentSlotData = cert is not null
                        ? new SlotData
                        {
                            ExpirationDate = cert.NotAfter.ToShortDateString(),
                            SubjectName = cert.SubjectName.Decode(X500DistinguishedNameFlags.None).Replace("CN=", ""),
                            Issuer = cert.Issuer.Replace("CN=", ""),
                            KeyType = new Oid(cert.GetKeyAlgorithm()).FriendlyName ?? "Unknown",
                            Certificate = cert
                        }
                        : null;

                    _UIDispatcher.Invoke(() =>
                    {
                        // TODO enable when this has a use
                        //CheckPendingRolloutsForCurrentSlot();

                        BtnChangeSlot.RaiseCanExecuteChanged();
                        BtnCreate.RaiseCanExecuteChanged();
                        BtnExport.RaiseCanExecuteChanged();
                        ShowCreateBtn = true;
                        ShowCenterControls = CurrentSlotData is not null;
                        if (!ShowCenterControls)
                        {
                            NoSlotOrCertText = "There is currently no certificate in this slot.";
                        }
                    });
                });

                _WindowService.StopLoadingWindow();
            }
        }

        private async Task GenerateNewButtonClick()
        {
            PILog("Generating new certificate for slot " + CurrentSlot.ToString("G"));
            if (CurrentSlot is PIVSlot.None)
            {
                Log("No PIV Slot selected!");
                return;
            }
            if (CurrentDevice is null)
            {
                Log("No device selected to generate new key on");
                return;
            }
            if (string.IsNullOrEmpty(_PrivacyIDEAService.CurrentUser()))
            {

                bool b = await LoginLogout();
                if (!b)
                {
                    Log("No user authenticated.");
                    return;
                }
            }

            PIVSlot slot = CurrentSlot;
            (bool success, string? subjectName, string? algorithm) = _WindowService.EnrollmentForm();

            if (!success)
            {
                Log("EnrollmentForm failure.");
                return;
            }
            if (string.IsNullOrEmpty(subjectName))
            {
                Log("Empty SubjectName!");
                return;
            }
            PIVAlgorithm pivAlgorithm = algorithm!.ToEnum<PIVAlgorithm>();

            PICertificateRequestData? data = CertUtil.CreateCSRData(CurrentDevice, subjectName, pivAlgorithm, slot, _CurrentUser!);
            if (data != null)
            {
                _PersistenceService.SaveCSR(data);
                await CompleteCertificateRollout(data);
            }
            else
            {
                Log("Failed to generate CSR.");
            }
        }

        private async Task CompleteCertificateRollout(PICertificateRequestData data)
        {
            Log("CompleteCertificateRollout");
            if (CurrentDevice is null)
            {
                Log("No device selected.");
                return;
            }

            if (_PrivacyIDEAService.IsConfigured() && !string.IsNullOrEmpty(_PrivacyIDEAService.CurrentUser()))
            {
                string desc = _CurrentDevice!.ManufacturerName() + " " + _CurrentDevice!.DeviceType()
                            + _CurrentDevice!.DeviceVersion() + " (Serial " + _CurrentDevice!.Serial() + ")";

                string? certStr = null;
                try
                {
                    certStr = await _PrivacyIDEAService.SendCSR(data.CSR, data.Attestation, desc);
                }
                catch (HttpRequestException e)
                {
                    Status = e.Message;
                    return;
                }
                catch (TaskCanceledException)
                {
                    return;
                }

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
                    bool success = CurrentDevice.ImportCertificate(data.Slot, cert);
                    if (success)
                    {
                        Log("Import successful!");
                        Status = "Imported the certificate successfully!";
                        await LoadSlot(data.Slot);

                        // Cleanup after success
                        _PersistenceService.Remove(data);
                        _PendingRollout = null;
                        ShowCompleteBtn = false;
                        PendingRolloutText = null;
                    }
                    else
                    {
                        Status = "Importing the Certificate failed!";
                        Log("Import failed!");
                    }
                }
            }
            else
            {
                Log("PrivacyIDEA setup is not complete. Cannot enroll certificate.");
            }
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
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
            Log(message);
        }

        public void PIError(Exception exception)
        {
            Log(exception);
        }
        #endregion
    }
}
