using PISmartcardClient.Utilities;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows;

namespace PISmartcardClient.ViewModels
{
    public class YKMgmtKeyVM : ObservableObject
    {
        private static readonly string DEFAULT_MANAGEMENT_KEY = "010203040506070801020304050607080102030405060708";
        private string? _Input;
        public string? Input
        {
            get => _Input;
            set => SetProperty(ref _Input, value);
        }

        private bool _InputEnabled = true;
        public bool InputEnabled
        {
            get => _InputEnabled;
            set => SetProperty(ref _InputEnabled, value);
        }

        public bool Cancelled { get; set; }

        public DelegateCommand CheckBoxCommand { get; set; }

        public RelayCommand<Window> OK { get; set; }

        public RelayCommand<Window> Cancel { get; set; }

        public YKMgmtKeyVM()
        {
            CheckBoxCommand = new(
                (cmdparam) =>
                {
                    bool checkd = (bool)cmdparam;
                    if (checkd)
                    {
                        Input = DEFAULT_MANAGEMENT_KEY;
                        InputEnabled = false;
                    }
                    else
                    {
                        Input = "";
                        InputEnabled = true;
                    }
                });

            OK = new(
                (cmdparam) =>
                {
                    cmdparam?.Close();
                });

            Cancel = new(
                (cmdparam) =>
                {
                    Cancelled = true;
                    cmdparam?.Close();
                });
        }
    }
}
