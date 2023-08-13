using System.Text.RegularExpressions;

namespace CipherLibrary.Helpers
{
    public class PasswordValidator
    {
        private const string PasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

        public static bool IsValid(string password)
        {
            return !string.IsNullOrEmpty(password) && Regex.IsMatch(password, PasswordPattern);
        }
    }
}