using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace CipherLibrary.Wpf.Behaviors
{
    public class ClosingBehavior : Behavior<Window>
    {
        public ICommand ClosingCommand
        {
            get => (ICommand)GetValue(ClosingCommandProperty);
            set => SetValue(ClosingCommandProperty, value);
        }

        public static readonly DependencyProperty ClosingCommandProperty =
            DependencyProperty.Register("ClosingCommand", typeof(ICommand), typeof(ClosingBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            if (this.AssociatedObject is Window window)
            {
                window.Closing += OnWindowClosing;
            }
        }

        protected override void OnDetaching()
        {
            if (this.AssociatedObject is Window window)
            {
                window.Closing -= OnWindowClosing;
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (ClosingCommand != null && ClosingCommand.CanExecute(null))
            {
                ClosingCommand.Execute(e);
            }
        }
    }
}