using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace BoxPrint.DataList
{
    /// <summary>
    /// https://gist.github.com/danielmarbach/977029
    /// Thread Safe ObservableCollection 구현버전
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [ComVisible(false)]
    public class SafeObservableCollection<T> : ObservableCollection<T>
    {
        private readonly Dispatcher dispatcher;

        private readonly ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

        public SafeObservableCollection()
            : this(Enumerable.Empty<T>())
        {
        }

        public SafeObservableCollection(Dispatcher dispatcher)
            : this(Enumerable.Empty<T>(), dispatcher)
        {
        }


        public SafeObservableCollection(IEnumerable<T> collection)
            : this(collection, Dispatcher.CurrentDispatcher)
        {
        }

        public SafeObservableCollection(IEnumerable<T> collection, Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;

            foreach (T item in collection)
            {
                this.Add(item);
            }
        }

        protected override void SetItem(int index, T item)
        {
            using (var locker = new WriterLock(this.readerWriterLock))
            {
                this.ExecuteOrBeginInvoke(() => this.SetItemBase(index, item));
            }
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            using (var locker = new WriterLock(this.readerWriterLock))
            {
                this.ExecuteOrBeginInvoke(() => this.MoveItemBase(oldIndex, newIndex));
            }
        }

        protected override void ClearItems()
        {
            using (var locker = new WriterLock(this.readerWriterLock))
            {
                this.ExecuteOrBeginInvoke(this.ClearItemsBase);
            }
        }

        protected override void InsertItem(int index, T item)
        {
            using (var locker = new WriterLock(this.readerWriterLock))
            {
                this.ExecuteOrBeginInvoke(() => this.InsertItemBase(index, item));
            }
        }

        protected override void RemoveItem(int index)
        {
            using (var locker = new WriterLock(this.readerWriterLock))
            {
                this.ExecuteOrBeginInvoke(() => this.RemoveItemBase(index));
            }
        }

        private void RemoveItemBase(int index)
        {
            base.RemoveItem(index);
        }

        private void InsertItemBase(int index, T item)
        {
            base.InsertItem(index, item);
        }

        private void ClearItemsBase()
        {
            base.ClearItems();
        }

        private void MoveItemBase(int oldIndex, int newIndex)
        {
            base.MoveItem(oldIndex, newIndex);
        }

        private void SetItemBase(int index, T item)
        {
            try
            {
                base.SetItem(index, item);
            }
            catch (Exception)
            {

            }
        }

        private void ExecuteOrBeginInvoke(Action action)
        {
            if (this.dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                this.dispatcher.Invoke(action);
            }
        }

        private class WriterLock : IDisposable
        {
            private readonly ReaderWriterLockSlim readerWriterLockSlim;

            public WriterLock(ReaderWriterLockSlim readerWriterLockSlim)
            {
                this.readerWriterLockSlim = readerWriterLockSlim;
                this.readerWriterLockSlim.EnterWriteLock();
            }

            public void Dispose()
            {
                this.readerWriterLockSlim.ExitWriteLock();
            }
        }
    }
}
