using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Windows;

namespace PISmartcardClient.ViewModels
{
    public class EnrollmentFormVM : ObservableObject
    {
        public ObservableCollection<string> Algorithms { get; set; }

        private string? _SelectedAlgorithm;
        public string SelectedAlgorithm
        {
            get => _SelectedAlgorithm ?? "EccP256";
            set => SetProperty(ref _SelectedAlgorithm, value);
        }

        public bool Cancelled { get; set; }
        public RelayCommand<Window> Cancel { get; set; }
        public RelayCommand<Window> OK { get; set; }
        public EnrollmentFormVM()
        {
            Algorithms = new()
            {
                "EccP256",
                "EccP384",
                "RSA2048"
            };

            Cancel = new(
                (window) =>
                {
                    Cancelled = true;
                    window?.Close();
                });

            OK = new((window) => window?.Close());
        }
        public void Reset()
        {
            SelectedAlgorithm = Algorithms[0];
            Cancelled = false;
        }
    }
}
