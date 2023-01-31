using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for PIAuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            AuthVM? vm = App.Current.Services.GetService<AuthVM>();
            Topmost = true;

            if (vm is not null)
            {
                DataContext = vm;
                vm.PasswordGetter = () => PWB1.Password;
                vm.UsernameInputVisibilityChanged = (val) =>
                {
                    if (val)
                    {
                        UserInput.Focus();
                    }
                    else
                    {
                        PWB1.Focus();
                    }
                };
            }
        }
    }
}
