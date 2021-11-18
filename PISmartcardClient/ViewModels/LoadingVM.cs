using System.Threading;
using PISmartcardClient.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

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

        public RelayCommand<ICloseableWindow> Cancel { get; set; }
        public CancellationToken CancellationToken { get; }
        private CancellationTokenSource _Source;

        public LoadingVM()
        {
            _Source = new();
            this.CancellationToken = _Source.Token;

            Cancel = new((window) =>
            {
                _Source.Cancel();
                window?.CloseWindow();
            });
        }
    }
}
