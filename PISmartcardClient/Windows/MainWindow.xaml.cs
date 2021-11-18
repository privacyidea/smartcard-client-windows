using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            MainVM? vm = App.Current.Services.GetService<MainVM>();
            if (vm is not null)
            {
                DataContext = vm;
                Closing += vm.OnWindowClosing;
            }
            InitializeComponent();
        }
    }
}
