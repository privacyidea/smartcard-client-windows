using PrivacyIDEAClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PISmartcardClient
{
    public interface IPrivacyIDEAService : IDisposable
    {
        public Task<bool> UserAuthentication();
        public Task<PIResponse?> SendCSR(string csr, string attestation, string? description = default);
        public string? CurrentUser();
        public void Logout();
        public bool IsConfigured();
        public void RegisterUpdateStatus(Action<string?> updateStatus);
        public Task<List<PIToken>> GetTokenForCurrentUser(Dictionary<string,string> parameters);
    }
}
