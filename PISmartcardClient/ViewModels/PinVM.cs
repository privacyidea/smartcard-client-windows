using System;
using PISmartcardClient.Windows;
using Microsoft.Toolkit.Mvvm.Input;

namespace PISmartcardClient.ViewModels
{
    public class PinVM
    {
        public string Message { get; set; } = "";
        public string Input1Label { get; set; } = "";
        public string? Input2Label { get; set; }
        public bool Cancelled { get; set; }
        public bool Show2ndInput { get; set; }
        public RelayCommand<ICloseableWindow> BtnOK { get; set; }
        public RelayCommand<ICloseableWindow> BtnCancel { get; set; }
        public Func<(string?, string?)>? PinGetter { get; set; }
        public string? Pin1 { get; private set; }
        public string? Pin2 { get; private set; }
        public PinVM()
        {
            BtnOK = new((wdw) =>
            {
                if (PinGetter is not null)
                {
                    var tuple = PinGetter();
                    Pin1 = tuple.Item1;
                    Pin2 = tuple.Item2;
                }
                wdw?.CloseWindow();
            });

            BtnCancel = new((wdw) =>
            {
                Cancelled = true;
                wdw?.CloseWindow();
            });
        }
    }
}
