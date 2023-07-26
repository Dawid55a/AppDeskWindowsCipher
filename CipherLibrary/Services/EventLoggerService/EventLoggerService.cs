using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;

namespace CipherLibrary.Services.EventLoggerService
{
    public class EventLoggerService : IEventLoggerService
    {
        private EventLog _log;

        private static readonly NameValueCollection AllAppSettings = ConfigurationManager.AppSettings;
        private readonly string _sourceName = AllAppSettings["SourceName"];
        private readonly string _logName = AllAppSettings["LogName"];

        public EventLoggerService()
        {
            CreateLog();
        }
        
        public void CreateLog()
        {
            if (!EventLog.SourceExists(_sourceName))
            {
                EventLog.CreateEventSource(_sourceName, _logName);
            }
            _log = new EventLog
            {
                Source = _sourceName,
                Log = _logName
            };
        }

        public void WriteInfo(string message)
        {
            _log.WriteEntry(message, EventLogEntryType.Information);
        }

        public void WriteError(string message)
        {
            _log.WriteEntry(message, EventLogEntryType.Error);
        }

        public void WriteWarning(string message)
        {
            _log.WriteEntry(message, EventLogEntryType.Warning);
        }

        public void ClearEntries()
        {
            _log.Clear();
        }
    }
}