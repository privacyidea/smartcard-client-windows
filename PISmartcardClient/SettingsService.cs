using System.Configuration;

namespace PISmartcardClient.Model
{
    public sealed class SettingsService : ApplicationSettingsBase, ISettingsService
    {
        [UserScopedSetting, DefaultSettingValue("")]
        public string URL
        {
            get => (string)this[nameof(URL)];
            set => this[nameof(URL)] = value;
        }

        [UserScopedSetting, DefaultSettingValue("true")]
        public bool SSLVerify
        {
            get => (bool)this[nameof(SSLVerify)];
            set => this[nameof(SSLVerify)] = value;
        }

        [UserScopedSetting, DefaultSettingValue("")]
        public string ServiceUser
        {
            get => (string)this[nameof(ServiceUser)];
            set => this[nameof(ServiceUser)] = value;
        }

        [UserScopedSetting, DefaultSettingValue("")]
        public string ServicePass
        {
            get => (string)this[nameof(ServicePass)];
            set => this[nameof(ServicePass)] = value;
        }

        bool? ISettingsService.GetBoolProperty(string name)
        {
            return (bool)this[name];
        }

        string? ISettingsService.GetStringProperty(string name)
        {
            return (string)this[name];
        }

        void ISettingsService.RegisterSettingsChangedHandler(SettingChangingEventHandler handler)
        {
            SettingChanging += handler;
        }
        void ISettingsService.SetProperty(string name, string value)
        {
            if ((string)this[name] != value)
            {
                this[name] = value;
                Save();
            }
        }

        void ISettingsService.SetProperty(string name, bool value)
        {
            if ((bool)this[name] != value)
            {
                this[name] = value;
                Save();
            }
        }
    }
}
