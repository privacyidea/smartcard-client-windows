using System;
using System.Threading.Tasks;

namespace PISmartcardClient
{
    public interface IPrivacyIDEAService : IDisposable
    {
        public Task<bool> DoUserAuthentication();
        public Task<string?> SendCSR(string csr, string attestation, string? description = default);
        public string? CurrentUser();
        public void Logout();
        public bool IsConfigured();
        public void RegisterUpdateStatus(Action<string?> updateStatus);
    }
}
