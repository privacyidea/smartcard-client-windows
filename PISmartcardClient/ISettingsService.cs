using System.Configuration;

namespace PISmartcardClient
{
    public interface ISettingsService
    {
        public string? GetStringProperty(string name);
        public bool? GetBoolProperty(string name);
        public void SetProperty(string name, string value);
        public void SetProperty(string name, bool value);
        public void RegisterSettingsChangedHandler(SettingChangingEventHandler handler);
    }
}
