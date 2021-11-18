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
        public bool SaveCSR(PICertificateRequestData data);
        public List<PICertificateRequestData>? LoadData(string user);
        public bool ExportCertificate(X509Certificate2 certificate, string path);
        public bool Remove(PICertificateRequestData data);
    }
    public class PersistenceService : IPersistenceService
    {
        private static readonly string PENDING_DIRECTORY = @"C:\Program Files\PrivacyIDEA PIV Enrollment\Pending\";
        private bool _checkedDir;

        List<PICertificateRequestData>? IPersistenceService.LoadData(string user)
        {
            EnsureDirectoryExists();
            //Log("PersistenceService: Checking for data of user " + user);
            string[] fileNames = Directory.GetFiles(PENDING_DIRECTORY, user + "*");
            List<PICertificateRequestData>? ret = null;
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
                            if (JsonSerializer.Deserialize(s, typeof(PICertificateRequestData)) is PICertificateRequestData data)
                            {
                                ret.Add(data);
                            }
                        }
                        catch (Exception e)
                        {
                            Log("Unable to deserialize '" + s + "'.");
                            Log(e);
                        }

                    }
                    else
                    {
                        Log("The file " + fileName + " had no content in it.");
                    }
                }
            }
            return ret;
        }

        bool IPersistenceService.SaveCSR(PICertificateRequestData data)
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
                Log(e);
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
                //TODO check exception types
                Log(e);
                return false;
            }
            return true;
        }

        bool IPersistenceService.Remove(PICertificateRequestData data)
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
                Log(e);
                return false;
            }
        }
    }
}
