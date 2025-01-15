using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Windows.Threading;

public interface IObservableList<T> : IList<T>, INotifyCollectionChanged
{
}

public class ObservableList<T> : IObservableList<T>
{
    private IList<T> collection = new List<T>();
    public event NotifyCollectionChangedEventHandler CollectionChanged;
    private ReaderWriterLockSlim sync = new ReaderWriterLockSlim(); //240301 RGJ ReaderWriterLock 고성능 슬림락으로 변경.

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        if (CollectionChanged == null)
            return;
        foreach (NotifyCollectionChangedEventHandler handler in CollectionChanged.GetInvocationList())
        {
            // If the subscriber is a DispatcherObject and different thread.
            var dispatcherObject = handler.Target as DispatcherObject;

            if (dispatcherObject != null && !dispatcherObject.CheckAccess())
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                    dispatcherObject.Dispatcher.Invoke
                          (DispatcherPriority.DataBind, handler, this, args);
                else
                    // Invoke handler in the target dispatcher's thread... 
                    // asynchronously for better responsiveness.
                    dispatcherObject.Dispatcher.BeginInvoke
                          (DispatcherPriority.DataBind, handler, this, args);
            }
            else
            {
                // Execute handler as is.
                handler(this, args);
            }
        }
    }

    public ObservableList()
    {
    }

    public void Add(T item)
    {
        sync.EnterWriteLock();
        try
        {
            collection.Add(item);
            OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                      NotifyCollectionChangedAction.Add, item));
        }
        finally
        {
            sync.ExitWriteLock();
        }
    }

    public void Clear()
    {
        sync.EnterWriteLock();
        try
        {
            collection.Clear();
        }
        finally
        {
            sync.ExitWriteLock();
            //플레이백 UI Thread 에서 리턴이 안되는 케이스가 생겨서 락을 풀수가 없는경우 발생.
            //락을 해체하고 이벤트 발생으로 변경
            OnCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
        }
    }

    public bool Contains(T item)
    {
        sync.EnterReadLock();
        try
        {
            var result = collection.Contains(item);
            return result;
        }
        finally
        {
            sync.ExitReadLock();
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        sync.EnterWriteLock();
        try
        {
            collection.CopyTo(array, arrayIndex);
        }
        finally
        {
            sync.ExitWriteLock();
        }
    }

    public int Count
    {
        get
        {
            sync.EnterReadLock();
            try
            {
                return collection.Count;
            }
            finally
            {
                sync.ExitReadLock();
            }
        }
    }

    public bool IsReadOnly
    {
        get { return collection.IsReadOnly; }
    }

    public bool Remove(T item)
    {
        sync.EnterWriteLock();
        bool result = false;
        int index = 0;
        try
        {
            index = collection.IndexOf(item);
            if (index == -1)
                return false;
            result = collection.Remove(item);
            if (result)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return result;
        }
        finally
        {
            sync.ExitWriteLock();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        //return collection.GetEnumerator();
        return CloneColletion().GetEnumerator();  //240301 RGJ  ObservableList<T> 개선 열거중에는 락이 안걸리므로 예외가 발생한다. 이에 복사본을 전달한다.
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        //return collection.GetEnumerator();
        return CloneColletion().GetEnumerator();//240301 RGJ ObservableList<T> 개선 열거중에는 락이 안걸리므로 예외가 발생한다. 이에 복사본을 전달한다.
    }

    /// <summary>
    /// Clone method to create an in memory copy of the Thread safe list
    /// </summary>
    /// <returns></returns>
    public List<T> CloneColletion()
    {
        List<T> clonedList = new List<T>();
        sync.EnterReadLock();
        try
        {
            (collection as List<T>).ForEach(x => clonedList.Add(x));
            return clonedList;
        }
        finally
        {
            sync.ExitReadLock();
        }
    }

    public int IndexOf(T item)
    {
        sync.EnterReadLock();
        try
        {
            var result = collection.IndexOf(item);
            return result;
        }
        finally
        {
            sync.ExitReadLock();
        }
    }

    public void Insert(int index, T item)
    {
        sync.EnterWriteLock();
        try
        {
            collection.Insert(index, item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }
        finally
        {
            sync.ExitWriteLock();
        }
    }

    public void Replace(int iindex, T item)
    {
        T olditem = default(T);
        sync.EnterWriteLock();
        try
        {
            if (collection.Count == 0 || collection.Count <= iindex)
                return;
            olditem = collection[iindex];
            collection[iindex] = item;
            OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                       NotifyCollectionChangedAction.Replace, item, olditem, iindex));

        }
        finally
        {
            sync.ExitWriteLock();

        }
    }

    public void RemoveAt(int index)
    {
        sync.EnterWriteLock();
        try
        {
            if (collection.Count == 0 || collection.Count <= index)
                return;
            var item = collection[index];
            collection.RemoveAt(index);
            OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                       NotifyCollectionChangedAction.Remove, item, index));
        }
        finally
        {
            sync.ExitWriteLock();
        }
    }

    public T this[int index]
    {
        get
        {
            sync.EnterReadLock();
            try
            {
                var result = collection[index];
                return result;
            }
            finally
            {
                sync.ExitReadLock();
            }
        }
        set
        {
            sync.EnterWriteLock();
            try
            {
                if (collection.Count == 0 || collection.Count <= index)
                    return;
                var item = collection[index];
                collection[index] = value;
                OnCollectionChanged(
                        new NotifyCollectionChangedEventArgs(
                           NotifyCollectionChangedAction.Replace, value, item, index));
            }
            finally
            {
                sync.ExitWriteLock();
            }
        }

    }
}
