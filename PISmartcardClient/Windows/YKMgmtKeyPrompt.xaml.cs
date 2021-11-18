using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for MgmtKeyPrompt.xaml
    /// </summary>
    public partial class YKMgmtKeyPrompt : Window, ICloseableWindow
    {
        public YKMgmtKeyPrompt()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            DataContext = App.Current.Services.GetService<YKMgmtKeyVM>();
        }

        public void CloseWindow()
        {
            Close();
        }
    }
}
