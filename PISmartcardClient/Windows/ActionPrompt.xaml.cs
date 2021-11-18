using System.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for ActionPrompt.xaml
    /// </summary>
    public partial class ActionPrompt : Window, ICloseableWindow
    {
        public ActionPrompt()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            DataContext = App.Current.Services.GetService<ActionPromptVM>();
        }

        public void CloseWindow()
        {
            Close();
        }
    }
}
