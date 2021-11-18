using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window, ICloseableWindow
    {
        public LoadingWindow()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            DataContext = App.Current.Services.GetService<LoadingVM>();
        }

        public void CloseWindow()
        {
            Close();
        }
    }
}
