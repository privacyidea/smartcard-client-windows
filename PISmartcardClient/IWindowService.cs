using System;
using System.Threading;

namespace PISmartcardClient
{
    public interface IWindowService
    {
        public (bool success, string? input) SimplePrompt(string title, string message, string buttonText);
        public (bool success, string? managementKey) YubikeyMgmtKeyPrompt();
        public (bool success, string? algorithm) EnrollmentForm();
        public (bool success, string? user, string? secondInput) AuthenticationPrompt(string? message, string? otpLabel, bool showUserInput);
        
        // Returns true if the user confirmed, false otherwise
        public bool ConfirmationPrompt(string message, bool showCancel);
        public bool ConfirmationPrompt(string message) => ConfirmationPrompt(message, true);
        public string? SaveFileDialog(string? filter, string defaultFileName);
        public string? SaveFileDialog(string? filter) => SaveFileDialog(filter, "");
        public CancellationToken StartLoadingWindow(string message);
        public void StopLoadingWindow();
        public void UpdateLoadingMessage(string newMessage);

        // Show a PIN prompt with optionally a second input. If the second label is not set, the second input will not be shown.
        public (bool success, string? pin1, string? pin2) PinPrompt(string message, string label1, string? label2);
        public (bool success, string? pin1, string? pin2) PinPrompt(string message, string label) => PinPrompt(message, label, null);
        public void Settings();
    }
}
