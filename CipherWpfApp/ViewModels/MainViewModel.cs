using CipherWpfApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Forms;
using CipherLibrary.Wpf;
using System.Windows;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Autofac;
using Autofac.Integration.Wcf;
using CipherLibrary.Wcf.Contracts;

namespace CipherWpfApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly NameValueCollection _allAppSettings = ConfigurationManager.AppSettings;
        private Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        private readonly ICipherService _cipherService;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<FileEntry> DecryptedFiles { get; set; }
        public ObservableCollection<FileEntry> ToEncryptFiles { get; set; }
        public ObservableCollection<LogEntry> LogEntries { get; set; }

        public ICommand SelectFolderCommand { get; set; }
        public ICommand MoveToEncryptCommand { get; set; }
        public ICommand MoveToDecryptCommand { get; set; }
        public ICommand StartEncryptionCommand { get; set; }

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
        public MainViewModel(ICipherService cipherService)
        {
            _cipherService = cipherService;

            DecryptedFiles = new ObservableCollection<FileEntry>();
            ToEncryptFiles = new ObservableCollection<FileEntry>();
            LogEntries = new ObservableCollection<LogEntry>();
            SelectedDecryptedFiles = new ObservableCollection<FileEntry>();
            SelectedEncryptedFiles = new ObservableCollection<FileEntry>();

            // Initialize commands
            SelectFolderCommand = new RelayCommand(SelectFolder);
            MoveToEncryptCommand = new RelayCommand(MoveToEncrypt);
            MoveToDecryptCommand = new RelayCommand(MoveToDecrypt);
            StartEncryptionCommand = new RelayCommand(StartEncryption);

            // Initialize working directory
            _allAppSettings.Set("WorkFolder", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            _config.Save(ConfigurationSaveMode.Modified);

            FolderPath = _allAppSettings.Get("WorkFolder");



            ToEncryptFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File1.txt", Name = "File1.txt", IsEncrypted = true, IsDecrypted = false});
            ToEncryptFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File2.txt", Name = "File2.txt", IsEncrypted = true, IsDecrypted = false});
            ToEncryptFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File3.txt", Name = "File3.txt", IsEncrypted = true, IsDecrypted = false });

            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true});
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true});
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true });
            //DecryptedFiles.Add(new FileEntry { Path = "C:\\Path\\To\\File4.txt", Name = "File4.txt", IsEncrypted = false, IsDecrypted = true });

            
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

        private void StartEncryption(object obj)
        {
            //var filesToEncrypt = ToEncryptFiles.Where(x => x.ToBeEncrypted).ToList();
            //var filesToDecrypt = DecryptedFiles.Where(x => x.ToBeDecrypted).ToList();

            //foreach (var file in filesToEncrypt)
            //{
            //    //_cipherService.EncryptFile(file.Path, EncryptionPassword);
            //}

            //foreach (var file in filesToDecrypt)
            //{
            //    //_cipherService.DecryptFile(file.Path, EncryptionPassword);
            //}
            foreach (var file in _cipherService.GetEncryptedFiles())
            {
                DecryptedFiles.Add(file);
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

}