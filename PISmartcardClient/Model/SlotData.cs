using System.Security.Cryptography.X509Certificates;

namespace PISmartcardClient.Model
{
    public class SlotData
    {
        public string? Issuer { get; set; }
        public string? SubjectName { get; set; }
        public string? ExpirationDate { get; set; }
        public string? KeyType { get; set; }
        public X509Certificate2? Certificate { get; set; }
    }
}
