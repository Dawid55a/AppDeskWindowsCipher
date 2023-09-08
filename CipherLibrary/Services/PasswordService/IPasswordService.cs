namespace CipherLibrary.Services.PasswordService
{
    public interface IPasswordService
    {
        void SetPassword(string password);
        void SetPassword(byte[] password);
        string GetPassword();
        string DecryptPassword(byte[] password);
        bool IsPasswordSet();
        bool IsPasswordCorrect(string password);
        bool IsPasswordCorrect(byte[] password);

    }
}