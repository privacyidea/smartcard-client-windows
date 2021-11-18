using Microsoft.Toolkit.Mvvm.ComponentModel;
using PIVBase;

namespace PISmartcardClient.Model
{
    public class PIVDevice : ObservableObject
    {
        private string? _Manufacturer;
        public string Manufacturer
        {
            get => _Manufacturer ?? "";
            set => SetProperty(ref _Manufacturer, value);
        }
        private string? _Type;
        public string Type
        {
            get => _Type ?? "";
            set => SetProperty(ref _Type, value);
        }
        private string? _Serial;
        public string Serial
        {
            get => _Serial ?? "";
            set => SetProperty(ref _Serial, value);
        }
        private string? _Version;
        public string Version
        {
            get => _Version ?? "";
            set => SetProperty(ref _Version, value);
        }
        public IPIVDevice Device { get; private set; }

        private string? _Description;
        public string Description
        {
            get => _Description ?? "";
            set => SetProperty(ref _Description, value);
        }
        public PIVDevice(IPIVDevice device)
        {
            Device = device;
            Manufacturer = device.ManufacturerName();
            Type = device.DeviceType();
            Serial = device.Serial();
            Version = device.DeviceVersion();
            Description = Type + " " + Version + " (" + Serial + ")";
        }
    }
}
