using System;
using System.Collections.Generic;
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

        private (bool success, string? oldPIN, string? newPIN) GetPINorPUKForChangeYubiKey(KeyEntryData keyEntryData, bool isPUK = false)
        {
            // Get the current PIN or PUK first
            string pinOrPuk = isPUK ? "PUK" : "PIN";
            string message = string.Format("Please enter your current {0}!", pinOrPuk);
            if (keyEntryData.IsRetry)
            {
                message = string.Format("Wrong {0}! Retries remaining: ", pinOrPuk) + keyEntryData.RetriesRemaining + "\n" + message;
            }

            (bool success, string? pin1, string? pin2) = _WindowService.PinPrompt(message, string.Format("{0}:", pinOrPuk), null);

            if (!success)
            {
                return (false, null, null);
            }
            string currentPIN = pin1!;

            // Get the new PIN or PUK and enforce the contstraints for it
            string defaultMessage = string.Format("Please enter a new {0} (6-8 Characters).", pinOrPuk);
            message = defaultMessage;
            string newPIN;
            while (true)
            {
                (success, pin1, pin2) = _WindowService.PinPrompt(message,
                                                                 string.Format("New {0}:", pinOrPuk),
                                                                 string.Format("Repeat new {0}:", pinOrPuk));
                if (!success)
                {
                    // Cancel the whole operation on the YubiKey
                    return (false, null, null);
                }
                else if (pin1 != pin2 || (string.IsNullOrEmpty(pin1) && string.IsNullOrEmpty(pin2)))
                {
                    message = string.Format("The {0}s must match and cannot be emtpy!\n", pinOrPuk) + defaultMessage;
                }
                else if (pin1!.Length is < 6 or > 8 || pin2!.Length is < 6 or > 8)
                {
                    message = string.Format("The {0} must match the length constraint!\n", pinOrPuk) + defaultMessage;
                }
                else
                {
                    newPIN = pin1!;
                    break;
                }
            }
            return (true, currentPIN, newPIN);
        }

        public bool KeyCollector(KeyEntryData keyEntryData)
        {
            Log("KeyCollector called...");

            if (keyEntryData.IsRetry)
            {
                var retriesRemaining = keyEntryData.RetriesRemaining;
                Log("is retry with" + retriesRemaining + " tries remaining");
            }
            (bool, string?) tup;

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
                            message = "Wrong PIN! Retries remaining: " + keyEntryData.RetriesRemaining + "\n" + message;
                        }
                        (bool success, string? pin1, string? _) = _WindowService.PinPrompt(message, "PIN:");
                        if (!success)
                        {
                            Log("Operation cancelled!");
                            return false;
                        }
                        if (pin1 is null)
                        {
                            throw new InvalidOperationException("PIN should not be empty!");
                        }

                        ReadOnlySpan<byte> pinSpan = Encoding.ASCII.GetBytes(pin1!);
                        keyEntryData.SubmitValue(pinSpan);
                        break;
                    }
                case KeyEntryRequest.ChangePivPin:
                    {
                        Log("...Change PIV PIN");
                        (bool success, string? oldPIN, string? newPIN) = GetPINorPUKForChangeYubiKey(keyEntryData);
                        if (!success)
                        {
                            Log("Operation cancelled!");
                            return false;
                        }
                        if (newPIN is null || oldPIN is null)
                        {
                            throw new InvalidOperationException("Cannot change PIN to emtpy value!");
                        }

                        ReadOnlySpan<byte> spanOldPIN = Encoding.ASCII.GetBytes(oldPIN);
                        ReadOnlySpan<byte> spanNewPIN = Encoding.ASCII.GetBytes(newPIN);

                        keyEntryData.SubmitValues(spanOldPIN, spanNewPIN);
                        break;
                    }
                case KeyEntryRequest.ResetPivPinWithPuk:
                    {
                        Log("... ResetPivPinWithPuk");
                        break;
                    }
                case KeyEntryRequest.ChangePivPuk:
                    {
                        Log("... ChangePivPuk");
                        (bool success, string? oldPUK, string? newPUK) = GetPINorPUKForChangeYubiKey(keyEntryData, true);
                        if (!success)
                        {
                            // Cancel operation on YubiKey
                            return false;
                        }
                        if (newPUK is null || oldPUK is null)
                        {
                            throw new InvalidOperationException("Cannot change PUK to emtpy value!");
                        }

                        ReadOnlySpan<byte> spanOldPUK = Encoding.ASCII.GetBytes(oldPUK);
                        ReadOnlySpan<byte> spanNewPUK = Encoding.ASCII.GetBytes(newPUK);

                        keyEntryData.SubmitValues(spanOldPUK, spanNewPUK);
                        break;
                    }
                case KeyEntryRequest.AuthenticatePivManagementKey:
                    {
                        Log("...Authenticate PIV Management key");
                        tup = _WindowService.YubikeyMgmtKeyPrompt();
                        if (!tup.Item1)
                        {
                            return false;
                        }
                        ReadOnlySpan<byte> mgmtKeySpan = HexStringToByteArray(tup.Item2);
                        keyEntryData.SubmitValue(mgmtKeySpan);
                        break;
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
