using System.Collections.Generic;
using PIVBase;

namespace PISmartcardClient
{
    public interface IDeviceService
    {
        List<IPIVDevice> GetAllDevices();
    }
}
