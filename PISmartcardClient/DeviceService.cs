using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using PIVBase;
using Yubico.YubiKey;
using YubiKeyPIV;
using static PIVBase.Utilities;

namespace PISmartcardClient
{
    public class DeviceService : IDeviceService
    {
        private readonly List<IDeviceFinder> _DeviceFinders = new();
        private readonly IWindowService _WindowService;

        public DeviceService(IWindowService windowService)
        {
            _WindowService = windowService;

            // TODO add new DeviceFinders for other Manufacturers here
            _DeviceFinders.Add(new YKDeviceFinder(KeyCollector));
        }

        List<IPIVDevice> IDeviceService.GetAllDevices()
        {
            List<IPIVDevice> foundDevices = new();
            foreach (var deviceFinder in _DeviceFinders)
            {
                var list = deviceFinder.GetConnectedDevices();
                foundDevices.AddRange(list);
            }
            return foundDevices;
        }


        /// <summary>
        /// Show a prompt for input until the input matches the contraints or the operation is cancelled.
        /// The input has to be non-empty and 6-8 characters long. The label for the input is configurable so it can be used for the PUK aswell.
        /// The PUK contraints are the same as for the PIN (for yubikeys)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inputLabel"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        public (bool success, string? pin) GetYubikeyPIN(string message, string inputLabel, int? retries = null)
        {
            while (true)
            {
                if (retries is int r)
                {
                    message += $"\n({r} retries remaining)";
                }

                (bool success, string? pin, _) = _WindowService.PinPrompt(message, inputLabel);

                if (!success)
                {
                    Log("PIN prompt cancelled by user");
                    return (false, null);
                }
                else if (pin is null)
                {
                    message = $"The PIN can not be empty!\n{message}";
                }
                else if (pin!.Length is < 6 or > 8)
                {
                    message = $"The PIN has to be 6, 7 or 8 characters long!\n{message}";
                }
                else
                {
                    return (true, pin);
                }
            }
        }

        public (bool success, string? oldValue, string? newValue) ChangePINorPUK(bool isPUK = false)
        {
            string insert = isPUK ? "PUK" : "PIN";

            (bool success, string? oldPIN) = GetYubikeyPIN($"Please enter your old {insert} first!", $"{insert}:");
            if (!success)
            {
                Log("Operation cancelled!");
                return (false, null, null);
            }
            string? newPIN1;
            string message = $"Please enter the new {insert}!";
            while (true)
            {
                (success, newPIN1) = GetYubikeyPIN(message, $"New {insert}:");
                if (!success)
                {
                    Log("Operation cancelled!");
                    return (false, null, null);
                }

                (success, string? newPIN2) = GetYubikeyPIN($"Please repeat the new {insert}!", $"New {insert}:");
                if (!success)
                {
                    Log("Operation cancelled!");
                    return (false, null, null);
                }

                if (newPIN1 == newPIN2)
                {
                    break;
                }
                else
                {
                    message = $"The new {insert}s did not match!\n{message}";
                }
            }
            return (true, oldPIN, newPIN1);
        }

        public bool KeyCollector(KeyEntryData keyEntryData)
        {
            Log("KeyCollector called...");

            if (keyEntryData.IsRetry)
            {
                var retriesRemaining = keyEntryData.RetriesRemaining;
                Log($"is retry with {retriesRemaining} tries remaining");
            }

            // Returning false cancels the operation on the yubikey
            switch (keyEntryData.Request)
            {
                case KeyEntryRequest.Release:
                {
                    Log("...Release");
                    break;
                }
                case KeyEntryRequest.VerifyPivPin:
                {
                    Log("...Verify PIV PIN");
                    string message = "Please enter your PIN!";
                    if (keyEntryData.IsRetry)
                    {
                        message = $"Wrong PIN! Retries remaining: {keyEntryData.RetriesRemaining}\n{message}";
                    }

                    (bool success, string? pin) = GetYubikeyPIN(message, "PIN:");
                    if (!success)
                    {
                        Log("Operation cancelled!");
                        return false;
                    }

                    ReadOnlySpan<byte> pinSpan = Encoding.ASCII.GetBytes(pin!);
                    keyEntryData.SubmitValue(pinSpan);
                    break;
                }
                case KeyEntryRequest.ChangePivPin:
                {
                    Log("...Change PIV PIN");

                    (bool success, string? oldPIN, string? newPIN) = ChangePINorPUK();
                    if (success)
                    {
                        ReadOnlySpan<byte> spanOldPIN = Encoding.ASCII.GetBytes(oldPIN!);
                        ReadOnlySpan<byte> spanNewPIN = Encoding.ASCII.GetBytes(newPIN!);

                        keyEntryData.SubmitValues(spanOldPIN, spanNewPIN);
                        break;
                    }
                    return false;
                }
                case KeyEntryRequest.ResetPivPinWithPuk:
                {
                    Log("... ResetPivPinWithPuk");
                    break;
                }
                case KeyEntryRequest.ChangePivPuk:
                {
                    Log("... ChangePivPuk");
                    (bool success, string? oldPUK, string? newPUK) = ChangePINorPUK(true);
                    if (success)
                    {
                        ReadOnlySpan<byte> spanOldPUK = Encoding.ASCII.GetBytes(oldPUK!);
                        ReadOnlySpan<byte> spanNewPUK = Encoding.ASCII.GetBytes(newPUK!);

                        keyEntryData.SubmitValues(spanOldPUK, spanNewPUK);
                        break;
                    }
                    return false;
                }
                case KeyEntryRequest.AuthenticatePivManagementKey:
                {
                    Log("...Authenticate PIV Management key");

                    (_, string? managementKey) = _WindowService.YubikeyMgmtKeyPrompt();
                    if (managementKey is not null)
                    {
                        ReadOnlySpan<byte> mgmtKeySpan = HexStringToByteArray(managementKey);
                        keyEntryData.SubmitValue(mgmtKeySpan);
                        break;
                    }
                    return false;
                }
                case KeyEntryRequest.ChangePivManagementKey:
                {
                    Log("... ChangePivManagementKey");
                    break;
                }
                case KeyEntryRequest.VerifyOathPassword:
                {
                    Log("... VerifyOathPassword");
                    break;
                }
                case KeyEntryRequest.SetOathPassword:
                {
                    Log("... SetOathPassword");
                    break;
                }
                default:
                {
                    Log("Unknown case: " + keyEntryData.Request);
                    return false;
                }
            }
            return true;
        }
    }
}
