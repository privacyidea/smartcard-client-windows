using System.Collections.Generic;

namespace PISmartcardClient
{
    public interface ISettingsService
    {
        public string? GetStringProperty(string name);
        public bool? GetBoolProperty(string name);
        public Dictionary<string, string> GetAll();
    }
}
