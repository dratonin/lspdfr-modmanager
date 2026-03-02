using System.IO;

namespace LSPDFRModManager.Helpers;

/// <summary>
/// Simple file-based logger that appends timestamped entries to log.txt
/// in the application's working directory.
/// Thread-safe thanks to a lock around all writes.
/// </summary>
public static class Logger
{
    private static readonly string LogFilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

    private static readonly object Lock = new();

    /// <summary>
    /// Writes a single log line with a UTC timestamp.
    /// </summary>
    public static void Log(string message)
    {
        string entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";

        lock (Lock)
        {
            try
            {
                File.AppendAllText(LogFilePath, entry + Environment.NewLine);
            }
            catch
            {
                // Logging should never crash the app.
            }
        }
    }
}
