using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PIVBase
{
    public class Utilities
    {
        private static readonly string LOG_FILE_PATH = @"C:\Program Files\PrivacyIDEA Smartcard Client\log.txt";

        public static bool LogDebug = false;

        public static byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "");
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string DashedHexString(string hex)
        {
            hex = hex.Replace("-", "");
            var list = Enumerable.Range(0, hex.Length / 2)
                             .Select(x => hex.Substring(x * 2, 2));
            return string.Join("-", list);
        }

        public static string InsertPeriodically(string input, int period, string insertion)
        {
            StringBuilder sb = new();
            string s = input;
            int count = 0;
            while (true)
            {
                try
                {
                    string tmp = s.Substring(count * period, period);
                    sb.Append(tmp).Append(insertion);
                    count++;
                }
                catch (Exception)
                {
                    sb.Append(s[(count * period)..]);
                    break;
                }
            }
            return sb.ToString();
        }

        public static string ByteArrayToHexString(byte[] ba, bool dashed = false)
        {
            if (ba == null)
            {
                return "";
            }

            string ret = BitConverter.ToString(ba);
            if (!dashed)
            {
                ret = ret.Replace("-", "");
            }
            return ret;
        }

        public static void Log(string message)
        {
            if (LogDebug)
            {
                string fullmsg = Prefix() + message;
                Trace.WriteLine(fullmsg);
                WriteToFile(fullmsg);
            }
        }

        public static void Error(string message)
        {
            string fullmsg = Prefix() + message;
            Trace.WriteLine(fullmsg);
            WriteToFile(fullmsg);
        }

        public static void Error(Exception exception)
        {
            Trace.WriteLine(exception);
            WriteToFile(Prefix() + exception.ToString());
        }

        private static string Prefix()
        {
            return $"[{Environment.CurrentManagedThreadId}][{DateTime.UtcNow:yyyy-MM-dd HH\\:mm\\:ss}] ";
        }

        public static void WriteToFile(string msg)
        {
            // TODO add a testwrite at startup to check??
            // Do not catch exception so it should be visible in eventviewer?
            using StreamWriter streamWriter = new(LOG_FILE_PATH, append: true);
            streamWriter.WriteLine(msg);
        }
    }
}
