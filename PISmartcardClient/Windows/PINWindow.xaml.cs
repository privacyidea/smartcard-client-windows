using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for PinInputWindow.xaml
    /// </summary>
    public partial class PinWindow : Window, ICloseableWindow
    {
        public PinWindow()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            PinVM? vm = App.Current.Services.GetService<PinVM>();
            PWB1.Focus();
            if (vm is not null)
            {
                DataContext = vm;
                vm.PinGetter = () => (PWB1.Password, PWB2.Password);
            }
        }

        public void CloseWindow()
        {
            Close();
        }
    }
}
