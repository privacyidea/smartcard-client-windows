using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for PinInputWindow.xaml
    /// </summary>
    public partial class PinWindow : Window
    {
        public PinWindow()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            PWB1.Focus();
            Title = "PIN required";
            if (App.Current.Services.GetService<PinVM>() is PinVM vm)
            {
                DataContext = vm;
                vm.PinGetter = () => (PWB1.Password, PWB2.Password);
            }
            Topmost = true;
        }
    }
}
