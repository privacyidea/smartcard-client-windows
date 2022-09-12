using Microsoft.Toolkit.Mvvm.Input;
using PISmartcardClient.Windows;
using System.Collections.Generic;
using System.Windows;

namespace PISmartcardClient.ViewModels
{
    internal class SettingsVM
    {
        public Dictionary<string, string> Settings { get; set; }

        private ISettingsService _SettingsService;
        public RelayCommand<Window> Close { get; set; }

        public SettingsVM(ISettingsService settingsService)
        {
            _SettingsService = settingsService;
            Settings = _SettingsService.GetAll();
            Close = new((wdw) => wdw?.Close());
        }
    }
}
