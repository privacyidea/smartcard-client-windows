using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PIVBase
{
    public class Utilities
    {
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
            string tmp = "";
            int count = 0;
            while (true)
            {
                try
                {
                    tmp = s.Substring(count * period, period);
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
            string ret = BitConverter.ToString(ba);
            if (!dashed)
                ret.Replace("-", "");
            return ret;
        }

        public static void Log(string message)
        {
            //Console.WriteLine(message);
            Trace.WriteLine("[" + Thread.CurrentThread.ManagedThreadId + "]  " + message);
        }

        public static void Log(Exception exception)
        {
            //Console.WriteLine(exception);
            Trace.WriteLine(exception);
        }
    }
}
