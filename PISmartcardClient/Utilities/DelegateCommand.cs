using System;
using System.Windows.Input;

namespace PISmartcardClient.Utilities
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object>? execute;
        private readonly Predicate<object>? canExecute;

        public DelegateCommand(Action<object> execute, Predicate<object> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        public DelegateCommand(Action<object> execute)
        {
            this.execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke(parameter!) ?? true;
        }

        public void Execute(object? parameter)
        {
            execute?.Invoke(parameter!);
        }
    }
}
