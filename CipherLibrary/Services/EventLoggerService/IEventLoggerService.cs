using System.Diagnostics;

namespace CipherLibrary.Services.EventLoggerService
{
    public interface IEventLoggerService
    {
        void CreateLog();
        void WriteDebug(string message);
        void WriteInfo(string message);
        void WriteError(string message);
        void WriteWarning(string message);
        void SetTraceLevel(TraceLevel level);
        TraceLevel GetTraceLevel();
        void ClearEntries();
    }
}