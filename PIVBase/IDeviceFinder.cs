using System.Collections.Generic;

namespace PIVBase
{
    public interface IDeviceFinder
    {
        public List<IPIVDevice> GetConnectedDevices();

    }
}
