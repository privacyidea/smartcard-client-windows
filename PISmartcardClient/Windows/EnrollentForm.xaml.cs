using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for EnrollentForm.xaml
    /// </summary>
    public partial class EnrollentForm : Window, ICloseableWindow
    {
        public EnrollentForm()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            DataContext = App.Current.Services.GetService<EnrollmentFormVM>();
            InputSubjectName.Focus();
        }

        public void CloseWindow()
        {
            Close();
        }
    }
}
