using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CipherLibrary.Wpf;
using System.Windows.Input;

namespace WpfApp.ViewModels
{
    public class PasswordDialogViewModel : INotifyPropertyChanged
    {
        private string _password;

        public string Password
        {
            get => _password;
            set
            {
                if (_password == value) return;
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public ICommand SubmitCommand { get; private set; }

        public PasswordDialogViewModel()
        {
            SubmitCommand = new RelayCommand(Submit);
        }

        private void Submit(object obj)
        {
            // You can perform password validation here before assigning the result.
            Password = _password;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}