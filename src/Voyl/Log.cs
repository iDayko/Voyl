using System;
using System.IO;
using System.Runtime.InteropServices;

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
                Write("START: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            catch
            {
                // Fail silently - no logging dependency allowed
            }
        }

        internal static void Stop()
        {
            try
            {
                Write("STOP: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            catch
            {
                // Fail silently
            }
        }

        internal static void Write(string message)
        {
            try
            {
                File.AppendAllText(LogPath, message + Environment.NewLine);
            }
            catch
            {
                // Fail silently
            }
        }
    }
}
