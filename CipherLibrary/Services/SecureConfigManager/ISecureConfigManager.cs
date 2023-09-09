using System.Collections.Generic;
using CipherLibrary.DTOs;

namespace CipherLibrary.Services.SecureConfigManager
{
    public interface ISecureConfigManager
    {
        void SaveSetting(string key, string value);
        string GetSetting(string key);
    }
}