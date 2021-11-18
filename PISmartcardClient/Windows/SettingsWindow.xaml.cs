using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window, ICloseableWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = App.Current.Services.GetService<SettingsVM>();
        }

        public void CloseWindow()
        {
            Close();
        }
    }
}
