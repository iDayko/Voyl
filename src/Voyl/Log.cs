using System;
using System.IO;

namespace Voyl
{
    internal static class Log
    {
        private static readonly string LogDirectory;
        private static readonly string LogPath;

        static Log()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            LogDirectory = Path.Combine(appData, "Voyl");
            LogPath = Path.Combine(LogDirectory, "voyl.log");
        }

        internal static void Start()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
            }
            catch { }
        }

        internal static void Stop()
        {
            try
            {
                Write("STOP: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            catch { }
        }

        internal static void Write(string message)
        {
            try
            {
                using var stream = File.Open(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(message);
                writer.Flush();
            }
            catch { }
        }
    }
}
