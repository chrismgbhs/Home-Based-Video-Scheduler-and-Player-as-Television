using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // WPF calls this to check if the button should be enabled
        public bool CanExecute(object parameter) =>
            _canExecute?.Invoke() ?? true;

        // WPF calls this when the button is clicked
        public void Execute(object parameter) => _execute();
    }
}
