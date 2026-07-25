using System;
using System.Globalization;
using System.IO;

namespace BGMMRPlugin.Util
{
    internal static class PluginLogger
    {
        private static readonly object Sync = new object();

        public static string CacheDirectory { get; } =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData
                ),
                "HearthstoneDeckTracker",
                "BGMMRPlugin"
            );

        private static string LogPath =>
            Path.Combine(CacheDirectory, "plugin.log");

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Debug(string message)
        {
            Write("DEBUG", message);
        }

        public static void Error(
            string message,
            Exception exception)
        {
            Write(
                "ERROR",
                message
                + (exception == null
                    ? string.Empty
                    : Environment.NewLine + exception)
            );
        }

        private static void Write(
            string level,
            string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(CacheDirectory);

                    RotateIfRequired();

                    File.AppendAllText(
                        LogPath,
                        DateTime.Now.ToString(
                            "yyyy-MM-dd HH:mm:ss.fff",
                            CultureInfo.InvariantCulture
                        )
                        + " | "
                        + level
                        + " | "
                        + message
                        + Environment.NewLine
                    );
                }
            }
            catch
            {
                // Logging must never interrupt HDT.
            }
        }

        private static void RotateIfRequired()
        {
            if (!File.Exists(LogPath))
                return;

            FileInfo information = new FileInfo(LogPath);

            if (information.Length < 1024 * 1024)
                return;

            string oldPath = Path.Combine(
                CacheDirectory,
                "plugin.old.log"
            );

            if (File.Exists(oldPath))
                File.Delete(oldPath);

            File.Move(LogPath, oldPath);
        }
    }
}
