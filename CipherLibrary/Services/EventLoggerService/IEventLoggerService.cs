namespace CipherLibrary.Services.EventLoggerService
{
    public interface IEventLoggerService
    {
        void CreateLog();
        void WriteInfo(string message);
        void WriteError(string message);
        void WriteWarning(string message);
        void ClearEntries();
    }
}