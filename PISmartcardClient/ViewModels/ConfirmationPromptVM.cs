using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows;

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

        public RelayCommand<Window> BtnOK { get; set; }
        public RelayCommand<Window> BtnCancel { get; set; }
        public bool Cancelled { get; set; }

        public bool ShowCancel { get; set; }

        public ConfirmationPromptVM()
        {
            BtnOK = new(
                (window) =>
                {
                    window?.Close();
                });

            BtnCancel = new(
                (window) =>
                {
                    Cancelled = true;
                    window?.Close();
                });
        }
    }
}
