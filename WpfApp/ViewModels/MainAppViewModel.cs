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
using System.Threading.Tasks;
using System.Windows;
using WpfApp.Views;
using System.Windows.Threading;

namespace WpfApp.ViewModels
{
    public class MainAppViewModel : INotifyPropertyChanged
    {
        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        private readonly ICipherService _cipherService;
        private readonly IEventLoggerService _eventLoggerService;
        private DispatcherTimer _eventLogTimer;
        private readonly EventLog _eventLog;
        private bool _cryptButtonEnabled;
        private DateTime _lastEventLogEntryTime;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FileEntry> DecryptedFiles { get; set; }
        public ObservableCollection<FileEntry> EncrypedFiles { get; set; }
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


        public MainAppViewModel(ICipherService cipherService, IEventLoggerService eventLoggerService)
        {
            _cipherService = cipherService;
            _eventLoggerService = eventLoggerService;
            _eventLog = new EventLog(_allAppSettings["LogName"], Environment.MachineName, _allAppSettings["SourceName"]);

            DecryptedFiles = new ObservableCollection<FileEntry>();
            EncrypedFiles = new ObservableCollection<FileEntry>();
            LogEntries = new ObservableCollection<EventLogEntry>();
            SelectedDecryptedFiles = new ObservableCollection<FileEntry>();
            SelectedEncryptedFiles = new ObservableCollection<FileEntry>();

            // Initialize commands
            SelectFolderCommand = new RelayCommand(SelectFolder);
            MoveToEncryptCommand = new RelayCommand(MoveToEncrypt);
            MoveToDecryptCommand = new RelayCommand(MoveToDecrypt);
            StartEncryptionCommand = new RelayCommand(async obj => await StartEncryptionAndDecryptionAsync(obj).ConfigureAwait(true));
            SetLogLevelCommand = new RelayCommand(SetLogLevel);
            TogglePopupCommand = new RelayCommand(TogglePopup);
            OnLoadedCommand = new RelayCommand(OnLoaded);
            RefreshDataCommand = new RelayCommand(RefreshData);
            ClearLogCommand = new RelayCommand(ClearLog);

            // Initialize working directory
            _allAppSettings.Set("WorkFolder", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            _config.Save(ConfigurationSaveMode.Modified);

            FolderPath = _allAppSettings.Get("WorkFolder");


            // EncrypedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File1.txt", Name = "File1.txt", IsEncrypted = true, IsDecrypted = false });
            // EncrypedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File2.txt", Name = "File2.txt", IsEncrypted = true, IsDecrypted = false });
            // EncrypedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File3.txt", Name = "File3.txt", IsEncrypted = true, IsDecrypted = false });

            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true});
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true});
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true });
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true });

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
            _eventLogTimer.Tick += FetchEventLogEntries;
            _eventLogTimer.Start();
        }
        private void FetchEventLogEntries(object sender, EventArgs e)
        {
            _eventLog.Entries.Cast<EventLogEntry>().ToList().ForEach(x =>
            {
                if (x.TimeGenerated <= _lastEventLogEntryTime) return;
                LogEntries.Add(x);
                _lastEventLogEntryTime = x.TimeGenerated;
            });
        }


        private void RefreshData(object obj)
        {
            DecryptedFiles.Clear();
            EncrypedFiles.Clear();

            foreach (var file in _cipherService.GetDecryptedFiles())
            {
                DecryptedFiles.Add(file);
            }

            foreach (var file in _cipherService.GetEncryptedFiles())
            {
                EncrypedFiles.Add(file);
            }

            CryptButtonEnabled = true;
        }

        private void OnLoaded(object obj)
        {
            // on first startup
            if (_allAppSettings.Get("FirstStartup") == "YES")
            {
                // TODO: Otrzymać publickey z serwisu
                // _publicKey = _cipherService.GetPublicKey();
            
                var passwordDialog = new PasswordDialog();
            
                // This will freeze the app until the dialog is closed
                var res = passwordDialog.ShowDialog();
                if (res == false)
                {
                    EncryptionPassword = passwordDialog.PasswordFirst.Password;
                    // TODO: Use the password
                }
                
                _allAppSettings.Set("FirstStartup", "NO");
                _config.Save(ConfigurationSaveMode.Modified);
            }

            _publicKey = _cipherService.GetPublicKey();

            foreach (var file in _cipherService.GetDecryptedFiles())
            {
                DecryptedFiles.Add(file);
            }

            foreach (var file in _cipherService.GetEncryptedFiles())
            {
                EncrypedFiles.Add(file);
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
                //_allAppSettings.Set("WorkFolder", dialog.SelectedPath);
                //workDirPath.Text = _allAppSettings.Get("WorkFolder");
            }
            _cipherService.ChangeOperationDirectory(FolderPath);
            _allAppSettings.Set("WorkFolder", FolderPath);
            //_client.SetWorkingDirectory(_allAppSettings.Get("WorkFolder"));
            //_config.Save(ConfigurationSaveMode.Modified);
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
                    EncrypedFiles.Add(file);
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
                    EncrypedFiles.Remove(file);
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
                    encryptedPassword = rsaPublicOnly.Encrypt(Encoding.UTF8.GetBytes(EncryptionPassword), true);
                }

                // Encrypt and decrypt files
                await Task.Run(() => _cipherService.EncryptFiles(EncrypedFiles.ToList(), encryptedPassword)).ConfigureAwait(true);
                await Task.Run(() => _cipherService.DecryptFiles(DecryptedFiles.ToList(), encryptedPassword)).ConfigureAwait(true);
            }
            finally
            {

                DecryptedFiles.Clear();
                EncrypedFiles.Clear();

                foreach (var file in _cipherService.GetDecryptedFiles())
                {
                    DecryptedFiles.Add(file);
                }

                foreach (var file in _cipherService.GetEncryptedFiles())
                {
                    EncrypedFiles.Add(file);
                }

                CryptButtonEnabled = true;
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