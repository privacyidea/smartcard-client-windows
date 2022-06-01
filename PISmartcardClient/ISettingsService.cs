using System.Configuration;

namespace PISmartcardClient
{
    public interface ISettingsService
    {
        public string? GetStringProperty(string name);
        public bool? GetBoolProperty(string name);
    }
}
