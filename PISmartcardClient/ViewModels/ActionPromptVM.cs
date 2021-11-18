using System;
using PISmartcardClient.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace PISmartcardClient.ViewModels
{
    public class ActionPromptVM : ObservableObject
    {
        private string _Message = "";
        public string Message
        {
            get => _Message;
            set => SetProperty(ref _Message, value);
        }

        public Action? Action { get; set; }
        public RelayCommand<ICloseableWindow> BtnOK { get; set; }

        public ActionPromptVM()
        {
            BtnOK = new(
                (window) =>
                {
                    window?.CloseWindow();
                    if (Action != null)
                    {
                        Action.Invoke();
                    }
                });
        }


    }
}
