using PISmartcardClient.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;

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

        public bool ShowUsernameInput { get; set; } = true;

        public bool Cancelled { get; set; }

        public RelayCommand<ICloseableWindow> OK { get; set; }
        public RelayCommand<ICloseableWindow> Cancel { get; set; }
        public Func<string?>? PasswordGetter { get; set; }

        public AuthVM()
        {
            OK = new(
                (window) =>
                {
                    if (PasswordGetter is not null)
                    {
                        PasswordInput = PasswordGetter();
                    }
                    window?.CloseWindow();
                });

            Cancel = new((window) =>
            {
                Cancelled = true;
                window?.CloseWindow();
            });
        }
    }
}
