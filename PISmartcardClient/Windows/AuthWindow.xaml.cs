using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for PIAuthWindow.xaml
    /// </summary>
    public partial class AuthInputWindow : Window
    {
        public AuthInputWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            AuthVM? vm = App.Current.Services.GetService<AuthVM>();
            if (vm is not null)
            {
                DataContext = vm;
                vm.PasswordGetter = () => PWB1.Password;
            }
            
            UserInput.Focus();
        }
    }
}
