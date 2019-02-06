using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDR.Console2File
{
    public static class ConsoleLogger
    {
        private static StringBuilder sbWriteLine = new StringBuilder();
        public static string Fileformat = "{0}-{1:yyyy-MM}.{2}"; // {0} = filename, {1} date, {2} = extension (log)

        private static void LogInfo(string s, bool logTime = true)
        {
            try
            {
                string path = Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location);
                string appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                string filename = Path.Combine(path, string.Format(Fileformat, appName, DateTime.Now, "log"));
                using (StreamWriter w = new StreamWriter(new FileStream(filename, FileMode.Append, FileAccess.Write)))
                {
                    if (logTime)
                    {
                        w.WriteLine(String.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [Info]: ", DateTime.Now) + s);
                    }
                    else
                    {
                        w.WriteLine(s);
                    }
                }
            }
            catch { }
        }

        public static void WriteEmptyLineToLog()
        {
            ConsoleLogger.LogInfo("", false);
        }

        public static void Write(string s)
        {
            if (Environment.UserInteractive)
            {
                System.Console.Write(s);
            }
            if (sbWriteLine == null)
            {
                sbWriteLine = new StringBuilder(s);
            }
            else
            {
                sbWriteLine.Append(s);
            }
        }

        public static void WriteLine(string s)
        {
            if (Environment.UserInteractive)
            {
                System.Console.WriteLine(s);
            }
            if (sbWriteLine != null)
            {
                sbWriteLine.Append(s);

                ConsoleLogger.LogInfo(sbWriteLine.ToString());
                sbWriteLine = null;
            }
            else
            {
                ConsoleLogger.LogInfo(s);
            }
        }

        public static void WriteLine()
        {
            if (Environment.UserInteractive)
            {
                System.Console.WriteLine();
            }
            if (sbWriteLine != null)
            {
                ConsoleLogger.LogInfo(sbWriteLine.ToString());
                sbWriteLine = null;
            }
            else
            {
                ConsoleLogger.LogInfo("");
            }
        }

    }
}