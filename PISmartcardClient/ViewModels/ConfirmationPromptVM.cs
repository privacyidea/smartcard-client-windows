using System;
using PISmartcardClient.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace PISmartcardClient.ViewModels
{
    public class ConfirmationPromptVM : ObservableObject
    {
        private string _Message = "";
        public string Message
        {
            get => _Message;
            set => SetProperty(ref _Message, value);
        }

        public RelayCommand<ICloseableWindow> BtnOK { get; set; }
        public RelayCommand<ICloseableWindow> BtnCancel { get; set; }
        public bool Cancelled { get; set; }

        public bool ShowCancel { get; set; }

        public ConfirmationPromptVM()
        {
            BtnOK = new(
                (window) =>
                {
                    window?.CloseWindow();
                });

            BtnCancel = new(
                (window) =>
                {
                    Cancelled = true;
                    window?.CloseWindow();
                });
        }
    }
}
