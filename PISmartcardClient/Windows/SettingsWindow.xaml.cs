using Microsoft.Extensions.DependencyInjection;
using PISmartcardClient.ViewModels;
using System.Windows;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = App.Current.Services.GetService<SettingsVM>();
        }
    }
}
