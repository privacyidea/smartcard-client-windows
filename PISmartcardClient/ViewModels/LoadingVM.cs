using System.Threading;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows;

namespace PISmartcardClient.ViewModels
{
    public class LoadingVM : ObservableObject

    {
        private string? _Message;
        public string? Message
        {
            get => _Message;
            set => SetProperty(ref _Message, value);
        }

        public RelayCommand<Window> Cancel { get; set; }
        public CancellationToken CancellationToken { get; }
        private readonly CancellationTokenSource _Source;

        public LoadingVM()
        {
            _Source = new();
            CancellationToken = _Source.Token;

            Cancel = new((window) =>
            {
                _Source.Cancel();
                window?.Close();
            });
        }
    }
}
