using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Windows;

namespace PISmartcardClient.ViewModels
{
    public class AuthVM : ObservableObject
    {
        private string? _Message;
        public string Message
        {
            get => _Message ?? "Please enter your credentials:";
            set => SetProperty(ref _Message, value);
        }

        private string? _UsernameInput;
        public string? UsernameInput
        {
            get => _UsernameInput;
            set
            {
                SetProperty(ref _UsernameInput, value);
                OK.NotifyCanExecuteChanged();
            }
        }

        private string? _PasswordInput;
        public string? PasswordInput
        {
            get => _PasswordInput;
            set => SetProperty(ref _PasswordInput, value);
        }

        private string? _PasswordLabel;
        public string PasswordLabel
        {
            get => _PasswordLabel ?? "Password or OTP";
            set => SetProperty(ref _PasswordLabel, value);
        }

        private bool _ShowUsernameInput = true;
        public bool ShowUsernameInput
        {
            get => _ShowUsernameInput;
            set
            {
                UsernameInputVisibilityChanged?.Invoke(value);
                _ShowUsernameInput = value;
            }
        }

        public bool Cancelled { get; set; }

        public RelayCommand<Window> OK { get; set; }
        public RelayCommand<Window> Cancel { get; set; }
        public Func<string?>? PasswordGetter { get; set; }

        public Action<bool>? UsernameInputVisibilityChanged { get; set; }

        public AuthVM()
        {
            OK = new(
                (window) =>
                {
                    if (PasswordGetter is not null)
                    {
                        PasswordInput = PasswordGetter();
                    }
                    window?.Close();
                });

            Cancel = new((window) =>
            {
                Cancelled = true;
                window?.Close();
            });
        }
    }
}
