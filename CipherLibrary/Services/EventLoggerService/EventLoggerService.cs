﻿using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;

namespace CipherLibrary.Services.EventLoggerService
{
    public class EventLoggerService : IEventLoggerService
    {
        private EventLog _log;
        private TraceSwitch _traceSwitch;

        private static readonly NameValueCollection AllAppSettings = ConfigurationManager.AppSettings;
        private readonly string _sourceName = AllAppSettings["SourceName"];
        private readonly string _logName = AllAppSettings["LogName"];


        public EventLoggerService()
        {
            CreateLog();
            _traceSwitch = new TraceSwitch("MySwitch", "Description");
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

        public void WriteDebug(string message)
        {
            if (_traceSwitch.TraceVerbose)
            {
                _log.WriteEntry(message, EventLogEntryType.Information);
            }
        }

        public void WriteInfo(string message)
        {
            if (_traceSwitch.TraceInfo)
            {
                _log.WriteEntry(message, EventLogEntryType.Information);
            }
        }

        public void WriteError(string message)
        {
            if (_traceSwitch.TraceError)
            {
                _log.WriteEntry(message, EventLogEntryType.Error);
            }
        }

        public void WriteWarning(string message)
        {
            if (_traceSwitch.TraceWarning)
            {
                _log.WriteEntry(message, EventLogEntryType.Warning);
            }
        }

        public TraceLevel GetTraceLevel()
        {
            return _traceSwitch.Level;
        }

        public void ClearEntries()
        {
            _log.Clear();
        }

        public void SetTraceLevel(TraceLevel level)
        {
            // Reconstructing the TraceSwitch with the desired level
            _traceSwitch = new TraceSwitch("MySwitch", "Description", level.ToString());
        }
    }
}