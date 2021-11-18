using System.Collections.ObjectModel;
using PISmartcardClient.Windows;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

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

        private string? _SubjectName;
        public string? SubjectName
        {
            get => _SubjectName;
            set => SetProperty(ref _SubjectName, value);
        }

        public bool Cancelled { get; set; }
        public RelayCommand<ICloseableWindow> Cancel { get; set; }
        public RelayCommand<ICloseableWindow> OK { get; set; }
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
                    window?.CloseWindow();
                });

            OK = new((window) => window?.CloseWindow());
        }
        public void Reset()
        {
            SubjectName = "";
            SelectedAlgorithm = Algorithms[0];
            Cancelled = false;
        }
    }
}
