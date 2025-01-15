using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool Set<T>(string propertyName, ref T field, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected Thread viewModelthread;

        public ViewModelBase()
        {
            viewModelthread = new Thread(new ThreadStart(ViewModelTimer));
            viewModelthread.IsBackground = true;
            viewModelthread.Start();
        }

        public bool CloseThread = false;
        protected virtual void ViewModelTimer() { return; }
    }
}
