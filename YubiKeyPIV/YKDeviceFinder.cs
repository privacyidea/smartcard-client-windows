using System;
using System.Collections.Generic;
using PIVBase;
using Yubico.YubiKey;
using static PIVBase.Utilities;

namespace YubiKeyPIV
{
    public class YKDeviceFinder : IDeviceFinder
    {
        private Func<KeyEntryData, bool> _keyCollector;
        public YKDeviceFinder(Func<KeyEntryData, bool> keyCollectorDelegate)
        {
            _keyCollector = keyCollectorDelegate;
        }
        List<IPIVDevice> IDeviceFinder.GetConnectedDevices()
        {
            List<IPIVDevice> retList = new();
            IEnumerable<IYubiKeyDevice> yubiKeyList = YubiKeyDevice.FindAll();

            foreach (IYubiKeyDevice current in yubiKeyList)
            {
                retList.Add(new YKPIVDevice(current, _keyCollector));
                Log("Found YubiKey " + current.SerialNumber);
            }

            return retList;
        }
    }
}
