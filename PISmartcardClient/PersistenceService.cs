using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using PISmartcardClient.Model;
using PISmartcardClient.Utilities;
using static PIVBase.Utilities;

namespace PISmartcardClient
{
    // TODO bundle file reads and writes
    // TODO file access rights
    public interface IPersistenceService
    {
        public bool SaveCSR(PIPendingCertificateRequest data);
        public List<PIPendingCertificateRequest> LoadData(string user);
        public bool ExportCertificate(X509Certificate2 certificate, string path);
        public bool Remove(PIPendingCertificateRequest data);
    }
    public class PersistenceService : IPersistenceService
    {
        private static readonly string PENDING_DIRECTORY = @"C:\Program Files\PrivacyIDEA Smartcard Client\Pending\";
        private bool _checkedDir;

        List<PIPendingCertificateRequest> IPersistenceService.LoadData(string user)
        {
            EnsureDirectoryExists();
            string[] fileNames = Directory.GetFiles(PENDING_DIRECTORY, user + "*");
            List<PIPendingCertificateRequest> ret = new();
            if (fileNames.Length > 0)
            {
                ret = new();
                foreach (string fileName in fileNames)
                {
                    string s = File.ReadAllText(fileName);
                    if (!string.IsNullOrEmpty(s))
                    {
                        try
                        {
                            if (JsonSerializer.Deserialize(s, typeof(PIPendingCertificateRequest)) is PIPendingCertificateRequest data)
                            {
                                ret.Add(data);
                            }
                        }
                        catch (Exception e)
                        {
                            Error("Unable to deserialize '" + s + "'.");
                            Error(e);
                        }

                    }
                    else
                    {
                        Error("The file " + fileName + " had no content in it.");
                    }
                }
            }
            return ret;
        }

        bool IPersistenceService.SaveCSR(PIPendingCertificateRequest data)
        {
            EnsureDirectoryExists();
            string name = data.User + "_" + data.DeviceSerial + "_" + data.Slot.ToString("G") + ".txt";
            string sData = JsonSerializer.Serialize(data);
            try
            {
                File.WriteAllText(PENDING_DIRECTORY + name, sData);
                Log("CSR written to file.");
            }
            catch (Exception e)
            {
                Error(e);
                return false;
            }
            return true;
        }

        private void EnsureDirectoryExists()
        {
            if (!_checkedDir)
            {
                Directory.CreateDirectory(PENDING_DIRECTORY);
                _checkedDir = true;
            }
        }

        bool IPersistenceService.ExportCertificate(X509Certificate2 certificate, string path)
        {
            string? strCert = CertUtil.FormatCertBytesForFile(certificate.RawData);
            try
            {
                File.WriteAllText(path, strCert);
            }
            catch (Exception e)
            {
                Error(e);
                return false;
            }
            return true;
        }

        bool IPersistenceService.Remove(PIPendingCertificateRequest data)
        {
            EnsureDirectoryExists();
            string name = data.User + "_" + data.DeviceSerial + "_" + data.Slot.ToString("G") + ".txt";
            try
            {
                File.Delete(PENDING_DIRECTORY + name);
                return true;
            }
            catch (Exception e)
            {
                Error(e);
                return false;
            }
        }
    }
}
