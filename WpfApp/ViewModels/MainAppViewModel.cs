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
using System.Linq;
using System.Windows.Forms;
using CipherLibrary.Services.EventLoggerService;
using CipherWpfApp.Models;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using WpfApp.Views;

namespace WpfApp.ViewModels
{
    public class MainAppViewModel : INotifyPropertyChanged
    {
        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        private readonly ICipherService _cipherService;
        private readonly IEventLoggerService _eventLoggerService;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FileEntry> DecryptedFiles { get; set; }
        public ObservableCollection<FileEntry> ToEncryptFiles { get; set; }
        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public ICommand SelectFolderCommand { get; set; }
        public ICommand MoveToEncryptCommand { get; set; }
        public ICommand MoveToDecryptCommand { get; set; }
        public ICommand StartEncryptionCommand { get; set; }
        public ICommand SetLogLevelCommand { get; set; }
        public ICommand TogglePopupCommand { get; set; }
        public ICommand FirstStartupCommand { get; set; }


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


        public MainAppViewModel(ICipherService cipherService, IEventLoggerService eventLoggerService)
        {
            _cipherService = cipherService;
            _eventLoggerService = eventLoggerService;

            DecryptedFiles = new ObservableCollection<FileEntry>();
            ToEncryptFiles = new ObservableCollection<FileEntry>();
            LogEntries = new ObservableCollection<LogEntry>();
            SelectedDecryptedFiles = new ObservableCollection<FileEntry>();
            SelectedEncryptedFiles = new ObservableCollection<FileEntry>();

            // Initialize commands
            SelectFolderCommand = new RelayCommand(SelectFolder);
            MoveToEncryptCommand = new RelayCommand(MoveToEncrypt);
            MoveToDecryptCommand = new RelayCommand(MoveToDecrypt);
            StartEncryptionCommand = new RelayCommand(StartEncryptionAndDecryption);
            SetLogLevelCommand = new RelayCommand(SetLogLevel);
            TogglePopupCommand = new RelayCommand(TogglePopup);
            FirstStartupCommand = new RelayCommand(FirstStartup); 

            // Initialize working directory
            _allAppSettings.Set("WorkFolder", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            _config.Save(ConfigurationSaveMode.Modified);

            FolderPath = _allAppSettings.Get("WorkFolder");


            ToEncryptFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File1.txt", Name = "File1.txt", IsEncrypted = true, IsDecrypted = false });
            ToEncryptFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File2.txt", Name = "File2.txt", IsEncrypted = true, IsDecrypted = false });
            ToEncryptFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File3.txt", Name = "File3.txt", IsEncrypted = true, IsDecrypted = false });

            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true});
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true});
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true });
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true });

            LogLevel = _eventLoggerService.GetTraceLevel().ToString();
        }

        private void FirstStartup(object obj)
        {
            if (_allAppSettings.Get("FirstStartup") == "YES")
            {
                // TODO: Otrzymać publickey z serwisu
                // _publicKey = _cipherService.GetPublicKey();
            
                var passwordDialog = new PasswordDialog();
            
                // This will freeze the app until the dialog is closed
                if (passwordDialog.ShowDialog() == true)
                {
                    var password = passwordDialog.Password;
                    // TODO: Use the password
            
            
                }
                _allAppSettings.Set("FirstStartup", "NO");
                _config.Save(ConfigurationSaveMode.Modified);
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

            //_client.SetWorkingDirectory(_allAppSettings.Get("WorkFolder"));
            //_config.Save(ConfigurationSaveMode.Modified);
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
                    ToEncryptFiles.Add(file);
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
                    ToEncryptFiles.Remove(file);
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

        private void StartEncryptionAndDecryption(object obj)
        {
            // Encrypt password with public key
            byte[] encryptedPassword;
            using (var rsaPublicOnly = new RSACryptoServiceProvider())
            {
                rsaPublicOnly.FromXmlString(_allAppSettings["PublicKey"]);
                encryptedPassword = rsaPublicOnly.Encrypt(Encoding.UTF8.GetBytes(EncryptionPassword), true);

            }
            // Encrypt and decrypt files
            _cipherService.EncryptFiles(ToEncryptFiles.ToList(), encryptedPassword);
            _cipherService.DecryptFiles(DecryptedFiles.ToList(), encryptedPassword);
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