using System;
using System.Reflection;
using System.Windows.Input;

namespace BoxPrint.GUI.ViewModels.BindingCommand
{
    //MVVM Light GitHub Source 가져와서 우리 프로그램에서 사용할 수 있도록 변경함.
    public class BindingDelegateCommand : ICommand
    {
        private readonly Action _execute;

        private readonly Func<bool> _canExecute;

        public BindingDelegateCommand(Action execute) : this(execute, null)
        {
        }

        public BindingDelegateCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = new Action(execute);

            if (canExecute != null)
            {
                _canExecute = new Func<bool>(canExecute);
            }
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }
            return _canExecute();
        }

        public virtual void Execute(object parameter)
        {
            this._execute();
        }
    }

    public class BindingDelegateCommand<T> : ICommand
    {
        private readonly Action<T> _execute;

        private readonly Func<T, bool> _canExecute;

        public BindingDelegateCommand(Action<T> execute) : this(execute, null)
        {
        }

        public BindingDelegateCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = new Action<T>(execute);

            if (canExecute != null)
            {
                _canExecute = new Func<T, bool>(canExecute);
            }
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            if (_canExecute == null)
            {
                return true;
            }

            if (parameter == null && typeof(T).GetTypeInfo().IsValueType)
            {
                return _canExecute(default(T));
            }

            if (parameter == null || parameter is T)
            {
                return _canExecute((T)parameter);
            }

            return false;
        }

        public virtual void Execute(object parameter)
        {
            var val = parameter;

            if (CanExecute(val) && _execute != null)
            {
                if (val == null)
                {
                    if (typeof(T).GetTypeInfo().IsValueType)
                    {
                        _execute(default(T));
                    }
                    else
                    {
                        _execute((T)val);
                    }
                }
                else
                {
                    _execute((T)val);
                }
            }
        }
    }
}
