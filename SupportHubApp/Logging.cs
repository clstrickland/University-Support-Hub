using System;
using System.IO; // Required for file operations
using System.Text; // Used for Exception formatting
using System.Threading; // Can be useful if adding delays or advanced threading

namespace SupportHubApp
{
    public class Logging
    {
        // --- Configuration ---
        private const string AppFolderName = "SupportHubApp"; // Folder name within AppData\Local
        private const string LogFileName = "SupportHubApp.log"; // Log file name
        private static readonly string LogFilePath; // Full path to the log file
        private static readonly object _logLock = new(); // Object for thread safety lock
        // --- Instance specific ---
        public required string SubModuleName { get; init; } // Keep 'required', C# 9+ feature

        // --- Static Constructor: Determine log path once ---
        static Logging()
        {
            try
            {

                // by the way, Windows uses virtualization for appdata so (at least on my machine) %localappdata% is actually in
                // %localappdata%\Packages\UniversitySupportHub_ptw92rvs1bhse\LocalCache\Local
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDirectory = Path.Combine(localAppData, AppFolderName);

                // Ensure the directory exists
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                LogFilePath = Path.Combine(logDirectory, LogFileName);

            }
            catch (Exception ex)
            {
                // Fallback if path setup fails (e.g., permissions)
                Console.Error.WriteLine($"FATAL ERROR initializing logging: {ex.Message}");
                // Consider setting LogFilePath to null or a known invalid state
                // and checking it before attempting to write later.
                LogFilePath = string.Empty; // Indicate failure
            }
        }


        // --- Private Helper Method for Writing ---
        private void WriteToFile(string level, string message)
        {
            // Don't try to write if path initialization failed
            if (string.IsNullOrEmpty(LogFilePath)) return;

            // Format: YYYY-MM-DD HH:MM:SS.fff [LEVEL] [SubModule] Message
            // Use DateTimeOffset for better timezone handling if needed, otherwise DateTime.Now is fine.
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMessage = $"{timestamp} [{level,-7}] [{SubModuleName}] {message}{Environment.NewLine}"; // Pad level for alignment

            // Lock to prevent multiple threads writing simultaneously and corrupting the file
            lock (_logLock)
            {
                try
                {
                    File.AppendAllText(LogFilePath, formattedMessage, Encoding.UTF8);
                }
                catch (IOException ioEx)
                {
                    // Handle file access errors (e.g., file locked, disk full)
                    Console.Error.WriteLine($"Error writing to log file '{LogFilePath}': {ioEx.Message}");
                    // Optionally, try writing to Debug output as a fallback
                    // System.Diagnostics.Debug.WriteLine(formattedMessage);
                }
                catch (Exception ex)
                {
                    // Handle other unexpected errors during logging
                    Console.Error.WriteLine($"Unexpected error writing log: {ex.Message}");
                }
            }
        }

        // --- Public Logging Methods ---
        public void LogInfo(string message)
        {
            WriteToFile("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteToFile("WARNING", message);
        }

        public void LogError(string message)
        {
            WriteToFile("ERROR", message);
        }

        // Keep this if you want arbitrary log levels (less common for file logging)
        // public void LogMessage(string message, string level = "DEBUG")
        // {
        //     WriteToFile(level.ToUpper(), message);
        // }

        public void LogException(Exception ex, string contextMessage = "")
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(contextMessage))
            {
                sb.Append(contextMessage).Append(" -> ");
            }
            sb.Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            sb.Append(Environment.NewLine).Append("StackTrace:").Append(Environment.NewLine).Append(ex.StackTrace);

            // Optionally log inner exceptions recursively
            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.Append(Environment.NewLine).Append("--> InnerException: ").Append(inner.GetType().Name).Append(": ").Append(inner.Message);
                sb.Append(Environment.NewLine).Append(inner.StackTrace);
                inner = inner.InnerException;
            }

            WriteToFile("ERROR", sb.ToString());
        }
    }
}