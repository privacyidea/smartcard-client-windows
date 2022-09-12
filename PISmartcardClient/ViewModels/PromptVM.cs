using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows;

namespace PISmartcardClient.ViewModels
{
    public partial class PromptVM : ObservableObject
    {
        private string _Input = "";
        public string Input
        {
            get => _Input;
            set => SetProperty(ref _Input, value);
        }

        private string _Message = "";
        public string Message
        {
            get => _Message;
            set => SetProperty(ref _Message, value);
        }

        private string _Title = "";
        public string Title
        {
            get => _Title;
            set => SetProperty(ref _Title, value);
        }
        public bool Cancelled { get; set; }

        private string _ButtonText = "OK";
        public string ButtonText
        {
            get => _ButtonText;
            set => SetProperty(ref _ButtonText, value);
        }
        public RelayCommand<Window> OnClick { get; set; }

        public PromptVM()
        {
            OnClick = new((wdw) => wdw?.Close());
        }

        public void Reset()
        {
            Cancelled = false;
            Title = "Prompt";
            ButtonText = "OK";
            Message = "";
            Input = "";
        }
    }
}
