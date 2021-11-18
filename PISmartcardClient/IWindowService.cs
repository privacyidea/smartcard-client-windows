using System;
using System.Threading;

namespace PISmartcardClient
{
    public interface IWindowService
    {
        public (bool success, string? input) SimplePrompt(string title, string message, string buttonText = "");
        public (bool success, string? managementKey) YubikeyMgmtKeyPrompt();
        public (bool success, string? subjectName, string? algorithm) EnrollmentForm();
        public void Settings();
        public (bool success, string? user, string? secondInput) AuthenticationPrompt();
        public void ActionPrompt(string message, Action? action);
        public string? SaveFileDialog(string? filter);
        public CancellationToken StartLoadingWindow(string message);
        public void StopLoadingWindow();
        public void UpdateLoadingMessage(string newMessage);
        public (bool success, string? pin1, string? pin2) PinPrompt(string message, string label1, string? label2 = null);
    }
}
