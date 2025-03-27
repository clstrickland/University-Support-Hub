using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SupportHubApp
{
    class Logging
    {
        private static readonly EventLog eventLog = new();
        private const string moduleName = "Support Hub App";
        public required string subModuleName;

        // Constructor
        public Logging()
        {
            if (!EventLog.SourceExists(moduleName))
            {
                EventLog.CreateEventSource(moduleName, moduleName);
            }
            eventLog.Source = moduleName;
            eventLog.Log = moduleName;
        }

        public void LogInfo(string message)
        {
            eventLog.WriteEntry($"{subModuleName}: {message}", EventLogEntryType.Information);
        }

        public void LogWarning(string message)
        {
            eventLog.WriteEntry($"{subModuleName}: {message}", EventLogEntryType.Warning);
        }

        public void LogError(string message)
        {
            eventLog.WriteEntry($"{subModuleName}: {message}", EventLogEntryType.Error);
        }

        public void LogMessage(string message, EventLogEntryType type)
        {
            eventLog.WriteEntry($"{subModuleName}: {message}", type);
        }

        public void LogException(Exception ex)
        {
            eventLog.WriteEntry($"{subModuleName}: {ex.Message}\n{ex.StackTrace}", EventLogEntryType.Error);
        }
    }

 }
