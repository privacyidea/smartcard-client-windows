using System.Collections.Generic;

namespace PISmartcardClient.ViewModels
{
    internal class SettingsVM
    {
        public Dictionary<string, string> Settings { get; set; }

        private ISettingsService _SettingsService;
        public SettingsVM(ISettingsService settingsService)
        {
            _SettingsService = settingsService;
            Settings = _SettingsService.GetAll();
        }
    }
}
