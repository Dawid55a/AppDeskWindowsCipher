using CipherLibrary.Wcf.Contracts;
using CipherLibrary.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using System;
using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using CipherLibrary.Services.EventLoggerService;
using CipherWpfApp.Models;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfApp.Views;
using System.Windows.Threading;
using CipherLibrary.DTOs;
using Application = System.Windows.Application;
using System.IO.IsolatedStorage;
using System.IO;
using CipherLibrary.Services.SecureConfigManager;
using MessageBox = System.Windows.MessageBox;

namespace WpfApp.ViewModels
{
    public class MainAppViewModel : INotifyPropertyChanged
    {
        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;

        private readonly ICipherService _cipherService;
        private readonly IEventLoggerService _eventLoggerService;
        private readonly ISecureConfigManager _secureConfig;
        private DispatcherTimer _eventLogTimer;
        private readonly EventLog _eventLog;
        private bool _cryptButtonEnabled;
        private DateTime _lastEventLogEntryTime;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FileEntry> DecryptedFiles { get; set; }
        public ObservableCollection<FileEntry> EncryptedFiles { get; set; }
        public ObservableCollection<EventLogEntry> LogEntries { get; set; }

        public ICommand SelectFolderCommand { get; set; }
        public ICommand MoveToEncryptCommand { get; set; }
        public ICommand MoveToDecryptCommand { get; set; }
        public ICommand StartEncryptionCommand { get; set; }
        public ICommand SetLogLevelCommand { get; set; }
        public ICommand TogglePopupCommand { get; set; }
        public ICommand OnLoadedCommand { get; set; }
        public ICommand RefreshDataCommand { get; set; }
        public ICommand ClearLogCommand { get; set; }

        private string _publicKey;

        private string _folderPath;

        public string FolderPath
        {
            get => _folderPath;
            set
            {
                _folderPath = value;
                OnPropertyChanged(nameof(FolderPath));
            }
        }

        private string _encryptionPassword;

        public string EncryptionPassword
        {
            get => _encryptionPassword;
            set
            {
                _encryptionPassword = value;
                OnPropertyChanged(nameof(EncryptionPassword));
            }
        }

        private IList _selectedDecryptedFiles;

        public IList SelectedDecryptedFiles
        {
            get => _selectedDecryptedFiles;
            set
            {
                _selectedDecryptedFiles = value;
                OnPropertyChanged(nameof(SelectedDecryptedFiles));
            }
        }

        private IList _selectedEncryptedFiles;

        public IList SelectedEncryptedFiles
        {
            get => _selectedEncryptedFiles;
            set
            {
                _selectedEncryptedFiles = value;
                OnPropertyChanged(nameof(SelectedEncryptedFiles));
            }
        }

        private string _logLevel;

        public string LogLevel
        {
            get => _logLevel;
            set
            {
                _logLevel = value;
                OnPropertyChanged(nameof(LogLevel));
            }
        }

        private bool _isPopupOpen;

        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                _isPopupOpen = value;
                OnPropertyChanged(nameof(IsPopupOpen));
            }
        }

        private bool _isButtonChecked;

        public bool IsButtonChecked
        {
            get => _isButtonChecked;
            set
            {
                _isButtonChecked = value;
                if (!_isButtonChecked)
                {
                    IsPopupOpen = false;
                }

                OnPropertyChanged(nameof(IsButtonChecked));
            }
        }


        public bool CryptButtonEnabled
        {
            get => _cryptButtonEnabled;
            set
            {
                _cryptButtonEnabled = value;
                OnPropertyChanged(nameof(CryptButtonEnabled));
            }
        }


        public MainAppViewModel(ICipherService cipherService, IEventLoggerService eventLoggerService, ISecureConfigManager secureConfig)
        {
            _cipherService = cipherService;
            _eventLoggerService = eventLoggerService;
            _secureConfig = secureConfig;

            _eventLog = new EventLog(
                _allAppSettings[AppConfigKeys.LogName], 
                Environment.MachineName,
                _allAppSettings[AppConfigKeys.SourceName]);

            DecryptedFiles = new ObservableCollection<FileEntry>();
            EncryptedFiles = new ObservableCollection<FileEntry>();
            LogEntries = new ObservableCollection<EventLogEntry>();
            SelectedDecryptedFiles = new ObservableCollection<FileEntry>();
            SelectedEncryptedFiles = new ObservableCollection<FileEntry>();

            // Initialize commands
            SelectFolderCommand = new RelayCommand(SelectFolder);
            MoveToEncryptCommand = new RelayCommand(MoveToEncrypt);
            MoveToDecryptCommand = new RelayCommand(MoveToDecrypt);
            StartEncryptionCommand =
                new RelayCommand(async obj => await StartEncryptionAndDecryptionAsync(obj).ConfigureAwait(true));
            SetLogLevelCommand = new RelayCommand(SetLogLevel);
            TogglePopupCommand = new RelayCommand(TogglePopup);
            OnLoadedCommand = new RelayCommand(OnLoaded);
            RefreshDataCommand = new RelayCommand(RefreshData);
            ClearLogCommand = new RelayCommand(ClearLog);

            // Initialize working directory
            if (string.IsNullOrEmpty(_secureConfig.GetSetting(AppConfigKeys.WorkFolder)))
            {
                _secureConfig.SaveSetting(AppConfigKeys.WorkFolder, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            }

            FolderPath = _secureConfig.GetSetting(AppConfigKeys.WorkFolder);

            LogLevel = _eventLoggerService.GetTraceLevel().ToString();

            CryptButtonEnabled = true;

            StartEventLogTimer();
        }

        private void ClearLog(object obj)
        {
            _eventLog.Clear();
            LogEntries.Clear();
        }

        private void StartEventLogTimer()
        {
            _eventLogTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _eventLogTimer.Tick += (sender, e) => FetchEventLogEntriesAsync();
            _eventLogTimer.Start();
        }

        private async Task FetchEventLogEntriesAsync()
        {
            var newEntries = new List<EventLogEntry>();

            await Task.Run(() =>
            {
                newEntries.AddRange(_eventLog.Entries.Cast<EventLogEntry>()
                    .Where(x => x.TimeGenerated > _lastEventLogEntryTime));

                _lastEventLogEntryTime =
                    newEntries.Any() ? newEntries.Max(x => x.TimeGenerated) : _lastEventLogEntryTime;
            }).ConfigureAwait(true);

            // Aktualizacja UI w głównym wątku
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var entry in newEntries)
                {
                    LogEntries.Add(entry);
                }
            });
        }


        private void RefreshData(object obj)
        {
            DecryptedFiles.Clear();
            EncryptedFiles.Clear();

            foreach (var file in _cipherService.GetDecryptedFiles())
            {
                DecryptedFiles.Add(file);
            }

            foreach (var file in _cipherService.GetEncryptedFiles())
            {
                EncryptedFiles.Add(file);
            }

            CryptButtonEnabled = true;
        }

        private void OnLoaded(object obj)
        {
            _eventLoggerService.WriteDebug($"First startup key {_secureConfig.GetSetting(AppConfigKeys.FirstStartup)}");
            // On first startup
            if (string.IsNullOrEmpty(_secureConfig.GetSetting(AppConfigKeys.FirstStartup)))
            {

                int retryCount = 0;
                while (retryCount < 3)
                {
                    _publicKey = _cipherService.GetPublicKey();

                    if (!string.IsNullOrEmpty(_publicKey))
                    {
                        break;  // Klucz został pomyślnie wczytany, wyjdź z pętli
                    }

                    // Zwiększ liczbę prób i poczekaj przed kolejnym podejściem
                    retryCount++;
                    Thread.Sleep(TimeSpan.FromSeconds(retryCount * 2));  // 2, 4, 6 sekund
                }

                if (string.IsNullOrEmpty(_publicKey))
                {
                    // Tutaj obsłuż błąd, na przykład wyświetl komunikat i zamknij aplikację
                    _eventLoggerService.WriteError("Nie można wczytać klucza publicznego. Aplikacja zostanie zamknięta.");
                    MessageBox.Show("Nie można wczytać klucza publicznego. Aplikacja zostanie zamknięta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);  // Zakończenie działania aplikacji z kodem błędu 1
                    return;
                }

                var passwordDialog = new PasswordDialog();

                // This will freeze the app until the dialog is closed
                var res = passwordDialog.ShowDialog();
                if (res == false)
                {
                    EncryptionPassword = passwordDialog.PasswordFirst.Password;
                    byte[] encryptedPassword;
                    using (var rsaPublicOnly = new RSACryptoServiceProvider())
                    {
                        _eventLoggerService.WriteDebug($"Public key in password encrypting: {_publicKey}");
                        rsaPublicOnly.FromXmlString(_publicKey);
                        encryptedPassword = rsaPublicOnly.Encrypt(Encoding.Default.GetBytes(EncryptionPassword), true);
                    }

                    _secureConfig.SaveSetting(AppConfigKeys.EncryptedPassword,
                        Encoding.Default.GetString(encryptedPassword, 0, encryptedPassword.Length));
                    _cipherService.SetPassword(encryptedPassword);
                }

                _secureConfig.SaveSetting(AppConfigKeys.FirstStartup, "NO");
                _eventLoggerService.WriteDebug($"First startup key {_secureConfig.GetSetting(AppConfigKeys.FirstStartup)}");

            }



            foreach (var file in _cipherService.GetDecryptedFiles())
            {
                DecryptedFiles.Add(file);
            }

            foreach (var file in _cipherService.GetEncryptedFiles())
            {
                EncryptedFiles.Add(file);
            }

            if (string.IsNullOrEmpty(_secureConfig.GetSetting(AppConfigKeys.PublicKey)))
            {
                _publicKey = _cipherService.GetPublicKey();
            }

        }

        private void SelectFolder(object obj)
        {
            var dialog = new FolderBrowserDialog
            {
                Description = "Wybierz katalog w którym przechowywane będzie pracować aplikacja"
            };

            var res = dialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                FolderPath = dialog.SelectedPath;
            }

            _cipherService.ChangeOperationDirectory(FolderPath);
            _secureConfig.SaveSetting(AppConfigKeys.WorkFolder, FolderPath);
            RefreshData(null);
        }

        private void MoveToEncrypt(object obj)
        {
            // Get selected files from decrypted files list
            if (SelectedDecryptedFiles != null)
            {
                // Make a copy of the list because we're modifying the collections during the loop
                var selectedFiles = new List<FileEntry>(SelectedDecryptedFiles.Cast<FileEntry>());
                foreach (FileEntry file in selectedFiles)
                {
                    DecryptedFiles.Remove(file);
                    if (file.IsEncrypted)
                    {
                        file.ToBeDecrypted = false;
                    }

                    if (file.IsDecrypted)
                    {
                        file.ToBeEncrypted = true;
                    }

                    EncryptedFiles.Add(file);
                }

                // Clear the selected items
                SelectedDecryptedFiles.Clear();
            }
        }

        private void MoveToDecrypt(object obj)
        {
            if (SelectedEncryptedFiles != null)
            {
                // Make a copy of the list because we're modifying the collections during the loop
                var selectedFiles = new List<FileEntry>(SelectedEncryptedFiles.Cast<FileEntry>());
                foreach (FileEntry file in selectedFiles)
                {
                    EncryptedFiles.Remove(file);
                    if (file.IsEncrypted)
                    {
                        file.ToBeDecrypted = true;
                    }

                    if (file.IsDecrypted)
                    {
                        file.ToBeEncrypted = false;
                    }


                    DecryptedFiles.Add(file);
                }

                // Clear the selected items
                SelectedEncryptedFiles.Clear();
            }
        }

        private async Task StartEncryptionAndDecryptionAsync(object obj)
        {
            CryptButtonEnabled = false;
            try
            {
                if (string.IsNullOrEmpty(_publicKey))
                {
                    throw new InvalidOperationException("Public key is not initialized.");
                }

                // Encrypt password with public key

                byte[] encryptedPassword;
                using (var rsaPublicOnly = new RSACryptoServiceProvider())
                {
                    rsaPublicOnly.FromXmlString(_publicKey);
                    encryptedPassword = rsaPublicOnly.Encrypt(Encoding.Default.GetBytes(EncryptionPassword), true);
                    _eventLoggerService.WriteWarning($"Korzystasz z hasła {EncryptionPassword}");
                }

                _cipherService.CheckPassword(encryptedPassword);


                if (_cipherService.CheckPassword(encryptedPassword))
                {
                    // Encrypt and decrypt files
                    await Task.Run(() => _cipherService.DecryptFiles(DecryptedFiles.ToList(), encryptedPassword))
                        .ConfigureAwait(true);
                    await Task.Run(() => _cipherService.EncryptFiles(EncryptedFiles.ToList(), encryptedPassword))
                        .ConfigureAwait(true);
                }
                else
                {
                    _eventLoggerService.WriteError("Wpisano nieprawidłowe hasło!");
                    MessageBox.Show("Hasło jest nieprawidłowe", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch(Exception e)
            {
                _eventLoggerService.WriteError(e.Message);
                return;
            }
            finally
            {
                // Giving some time for file watcher tu update changes
                Thread.Sleep(50);
                RefreshData(null);
            }
        }

        private void SetLogLevel(object parameter)
        {
            LogLevel = parameter as string;
            IsPopupOpen = false;
            IsButtonChecked = false;
        }

        private void TogglePopup(object obj)
        {
            IsPopupOpen = IsButtonChecked;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}