using System;
using System.Windows;
using PISmartcardClient.Windows;
using PISmartcardClient.ViewModels;
using Microsoft.Win32;
using static PIVBase.Utilities;
using System.Threading;

namespace PISmartcardClient
{
    public class WindowService : IWindowService
    {
        private LoadingWindow? _LoadingWindow;
        (bool, string?) IWindowService.SimplePrompt(string? title, string message, string buttonText)
        {
            Prompt prompt = new();
            PromptVM promptVM = (PromptVM)prompt.DataContext;
            promptVM.Title = title ?? "Action Required!";
            promptVM.Message = message;
            if (!string.IsNullOrEmpty(buttonText))
            {
                promptVM.ButtonText = buttonText;
            }

            ShowBlockingDialog(prompt);
            if (promptVM.Cancelled)
            {
                Log("PIN input cancelled!");
                return (false, "");
            }
            return (true, promptVM.Input);
        }

        (bool, string?) IWindowService.YubikeyMgmtKeyPrompt()
        {
            YKMgmtKeyPrompt keyPrompt = new();
            YKMgmtKeyVM vm = (YKMgmtKeyVM)keyPrompt.DataContext;

            ShowBlockingDialog(keyPrompt);

            if (vm.Cancelled)
            {
                Log("Yubikey ManagementKeyPrompt cancelled.");
                return (false, null);
            }

            return (true, vm.Input);
        }

        (bool, string?, string?) IWindowService.EnrollmentForm()
        {
            EnrollentForm form = new();
            EnrollmentFormVM formVM = (EnrollmentFormVM)form.DataContext;
            ShowBlockingDialog(form);
            if (formVM.Cancelled)
            {
                Log("EnrollmentForm cancelled.");
                return (false, null, null);
            }
            return (true, formVM.SubjectName, formVM.SelectedAlgorithm);
        }

        (bool success, string? user, string? secondInput) IWindowService.AuthenticationPrompt()
        {
            AuthInputWindow authWindow = new();
            AuthVM authVM = (AuthVM)authWindow.DataContext;

            // TODO check for empty inputs?
            ShowBlockingDialog(authWindow);

            if (authVM.Cancelled)
            {
                Log("Authentication window cancelled.");
                return (false, null, null);
            }

            return (true, authVM.UsernameInput, authVM.PasswordInput);
        }

        void IWindowService.ActionPrompt(string message, Action? action)
        {
            ActionPrompt ap = new();
            ActionPromptVM apVM = (ActionPromptVM)ap.DataContext;
            apVM.Action = action;
            apVM.Message = message;
            ShowBlockingDialog(ap);
        }

        private void ShowBlockingDialog<T>(T t) where T : Window
        {
            Application.Current.MainWindow.IsEnabled = false;
            t.ShowDialog();
            Application.Current.MainWindow.IsEnabled = true;
        }

        string? IWindowService.SaveFileDialog(string? filter)
        {
            SaveFileDialog saveFileDialog = new();
            if (!string.IsNullOrEmpty(filter))
            {
                saveFileDialog.Filter = filter;
            }

            bool? diagSuccess = saveFileDialog.ShowDialog();
            if (diagSuccess.HasValue && !diagSuccess.Value)
            {
                Log("SaveFileDialog failed!");
                return null;
            }

            return saveFileDialog.FileName;
        }

        CancellationToken IWindowService.StartLoadingWindow(string message)
        {
            _LoadingWindow = new();
            LoadingVM vm = (LoadingVM)_LoadingWindow.DataContext;
            vm.Message = message;
            Application.Current.MainWindow.IsEnabled = false;
            _LoadingWindow.Show();
            return vm.CancellationToken;
        }

        void IWindowService.StopLoadingWindow()
        {
            if (_LoadingWindow is not null)
            {
                _LoadingWindow.Close();
                _LoadingWindow = null;
                Application.Current.MainWindow.IsEnabled = true;
            }
        }

        void IWindowService.UpdateLoadingMessage(string newMessage)
        {
            if (_LoadingWindow is not null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LoadingVM vm = (LoadingVM)_LoadingWindow.DataContext;
                    vm.Message = newMessage;
                });
            }
        }

        (bool success, string? pin1, string? pin2) IWindowService.PinPrompt(
            string message, string label1, string? label2)
        {
            PinWindow pinWindow = new();
            PinVM vm = (PinVM)pinWindow.DataContext;

            vm.Message = message;
            vm.Input1Label = label1;
            if (label2 is null)
            {
                vm.Show2ndInput = false;
            }
            else
            {
                vm.Show2ndInput = true;
                vm.Input2Label = label2;
            }

            ShowBlockingDialog(pinWindow);

            return (!vm.Cancelled, vm.Pin1, vm.Pin2);
        }
    }
}
