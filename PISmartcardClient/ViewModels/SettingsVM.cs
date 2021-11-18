using PISmartcardClient.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using static PIVBase.Utilities;

namespace PISmartcardClient.ViewModels
{
    public class SettingsVM : ObservableObject
    {
        private ISettingsService _Settings;

        private string? _Url;
        public string? Url
        {
            get => _Url;
            set => SetProperty(ref _Url, value);
        }

        private bool? _SSLverify;
        public bool SSLverify
        {
            get => _SSLverify ?? true;
            set => SetProperty(ref _SSLverify, value);
        }

        private string? _ServiceUser;
        public string? ServiceUser
        {
            get => _ServiceUser;
            set => SetProperty(ref _ServiceUser, value);
        }

        private string? _ServicePass;
        public string? ServicePass
        {
            get => _ServicePass;
            set => SetProperty(ref _ServicePass, value);
        }

        public RelayCommand<ICloseableWindow> BtnOK { get; set; }

        public SettingsVM(ISettingsService settingsService)
        {
            _Settings = settingsService;

            _Url = _Settings.GetStringProperty("url");
            _SSLverify = _Settings.GetBoolProperty("sslverify");
            _ServiceUser = _Settings.GetStringProperty("serviceuser");
            _ServicePass = _Settings.GetStringProperty("servicepass");

            BtnOK = new(
                (window) =>
                {
                    // TODO verify url format?
                    SaveNewSettings();
                    window?.CloseWindow();
                });
        }

        private void SaveNewSettings()
        {
            _Settings.SetProperty("url", _Url ?? "");
            _Settings.SetProperty("sslverify", SSLverify);
            _Settings.SetProperty("serviceuser", _ServiceUser ?? "");
            _Settings.SetProperty("servicepass", _ServicePass ?? "");
            Log("Saved Settings.");
        }
    }
}
