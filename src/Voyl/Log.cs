using System;
using System.IO;
using System.Text;

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
                // Use File.AppendAllText with UTF-8 encoding to avoid BOM issues
                File.AppendAllText(LogPath, message + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Fallback: try with ASCII encoding
                try
                {
                    File.AppendAllText(LogPath, message + Environment.NewLine, Encoding.ASCII);
                }
                catch { }
            }
        }
    }
}
