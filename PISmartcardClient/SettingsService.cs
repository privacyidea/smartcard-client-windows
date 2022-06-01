using Microsoft.Win32;
using System;
using static PIVBase.Utilities;

namespace PISmartcardClient
{
    internal class SettingsService : ISettingsService
    {
        private readonly string _RegistryPath = "SOFTWARE\\Netknights GmbH\\PrivacyIDEA Enrollment Tool";

        public bool? GetBoolProperty(string name)
        {
            var val = ReadRegistryEntry(name);
            Log($"Settings: Read {val} for {name}");
            if (val is not null)
            {
                return val == "1";
            }
            
            return null;
        }

        public string? GetStringProperty(string name)
        {
            var ret = ReadRegistryEntry(name);
            Log($"Settings: Read {ret} for {name}");
            return ret;
        }

        private string? ReadRegistryEntry(string name)
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(_RegistryPath);
                if (key is not null)
                {
                    Object? o = key.GetValue(name);
                    if (o is not null)
                    {
                        return o as string;
                    }
                    else
                    {
                        Log("object for key " + key + " is null.");
                    }

                }
            }
            catch (Exception ex)
            {
                Log("Exception while trying to read registry value: " + ex.Message);
            }

            return null;
        }
    }
}
