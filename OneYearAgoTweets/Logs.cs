using System;
using System.IO;

namespace OneYearAgoTweets
{
    internal class Logs
    {
        private static Logs _instance;
        private static readonly object SyncLock = new object();

        private Logs()
        {
            if (!(File.Exists("log.txt")))
            {
                File.Create("log.txt");
            }
        }

        public static Logs GetLogsClass()
        {
            if (_instance == null)
            {
                lock (SyncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new Logs();
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        ///     Writes the log.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="logstring">The logstring.</param>
        public static void WriteLog(string file, string logstring)
        {
            var fi = new FileInfo(file);
            if (fi.Length > 10485760)
            {
                //если больше 10 мб то бекапим лог и создаем новый
                File.Replace(file, file + DateTime.Now, file + "old");
                File.AppendAllText(file, "Old log was renamed to " + file + DateTime.Now);
            }

            File.AppendAllText(file, string.Format("{0} => {1}\n", DateTime.Now, logstring));
            Console.WriteLine(logstring);
        }
    }
}