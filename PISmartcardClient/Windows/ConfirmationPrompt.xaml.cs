using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PISmartcardClient.ViewModels;

namespace PISmartcardClient.Windows
{
    /// <summary>
    /// Interaction logic for ActionPrompt.xaml
    /// </summary>
    public partial class ConfirmationPrompt : Window, ICloseableWindow
    {
        public ConfirmationPrompt()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            DataContext = App.Current.Services.GetService<ConfirmationPromptVM>();
        }

        public void CloseWindow()
        {
            Close();
        }
    }
}
