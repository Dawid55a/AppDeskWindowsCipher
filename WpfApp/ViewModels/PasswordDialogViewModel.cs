using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CipherLibrary.Wpf;
using System.Windows.Input;
using System;
using CipherLibrary.Helpers;

namespace WpfApp.ViewModels
{
    public class PasswordDialogViewModel : INotifyPropertyChanged
    {
        private string _passwordFirst;
        private string _passwordSecond;
        private string _errorMessage;

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage == value) return;
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public string PasswordFirst
        {
            get => _passwordFirst;
            set
            {
                if (_passwordFirst == value) return;
                _passwordFirst = value;
                OnPropertyChanged(nameof(PasswordFirst));
            }
        }
        public string PasswordSecond
        {
            get => _passwordSecond;
            set
            {
                if (_passwordSecond == value) return;
                _passwordSecond = value;
                OnPropertyChanged(nameof(PasswordSecond));
            }
        }

        public Action CloseAction { get; set; }
        private bool _isClosingCommandExecuted;
        // private RelayCommand _closingCommand;
        // public RelayCommand ClosingCommand
        // {
        //     get
        //     {
        //
        //         return _closingCommand ?? (_closingCommand = new RelayCommand(
        //             args =>
        //             {
        //                 if (_isClosingCommandExecuted)
        //                 {
        //                     return;
        //                 }
        //
        //                 if (args is CancelEventArgs e)
        //                 {
        //                     // If the window should not close, set e.Cancel = true;
        //                     e.Cancel = true;
        //                     _isClosingCommandExecuted = true;
        //                     ErrorMessage = "closing command executed";
        //
        //                     var result = MessageBox.Show("Czy na pewno chcesz wyjść z aplikacji? Wszyskie zmiany zostaną zapomniane",
        //                         "Potwierdzenie", MessageBoxButton.YesNo);
        //                     if (result == MessageBoxResult.Yes)
        //                     {
        //                         Application.Current.Shutdown();
        //                     }
        //
        //                 }
        //             },
        //             args => true // determine when the command can execute
        //         ));
        //     }
        // }
        public ICommand SubmitCommand { get; private set; }

        public PasswordDialogViewModel()
        {
            SubmitCommand = new RelayCommand(Submit);
        }

        private void Submit(object obj)
        {
            // Password needs to be at least 8 characters long and contain at least one digit, one uppercase letter and one lowercase letter
            if (!PasswordValidator.IsValid(PasswordFirst))
            {
                ErrorMessage = "Hasło musi mieć minimum 8 znaków, dużą literę, małą literę i cyfrę";
                return;
            }

            if (PasswordFirst != PasswordSecond)
            {
                ErrorMessage = "Hasła nie są takie same";
                return;
            }

            _isClosingCommandExecuted = false;
            CloseAction?.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}