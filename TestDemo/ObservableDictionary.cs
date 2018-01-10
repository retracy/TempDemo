using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly IList<TValue> values;
    private readonly IDictionary<TKey, int> indexMap;

    private ObservableDictionary<TKey, TValue>.SimpleMonitor _monitor = new ObservableDictionary<TKey, TValue>.SimpleMonitor();

    private const string CountString = "Count";
    private const string IndexerName = "Item[]";
    private const string KeysString = "Keys";
    private const string ValuesString = "Values";

    #region Constructor

    public ObservableDictionary()
    {
        this.values = new List<TValue>();
        this.indexMap = new Dictionary<TKey, int>();
    }

    public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
    {
        this.values = new List<TValue>();
        this.indexMap = new Dictionary<TKey, int>();

        int idx = 0;
        foreach (var kvp in dictionary)
        {
            this.indexMap.Add(kvp.Key, idx);
            this.values.Add(kvp.Value);

            idx++;
        }
    }

    public ObservableDictionary(int capacity)
    {
        this.values = new List<TValue>(capacity);
        this.indexMap = new Dictionary<TKey, int>(capacity);
    }

    #endregion

    #region Virtual Add/Remove/Change Control Methods

    protected virtual void AddItem(TKey key, TValue value)
    {
        this.CheckReentrancy();

        var index = this.values.Count;
        this.indexMap.Add(key, index);
        this.values.Add(value);

        this.OnPropertyChanged(CountString);
        this.OnPropertyChanged(KeysString);
        this.OnPropertyChanged(ValuesString);
        this.OnPropertyChanged(IndexerName);
        this.OnCollectionChanged(NotifyCollectionChangedAction.Add, key, value, index);
    }

    protected virtual bool RemoveItem(TKey key)
    {
        this.CheckReentrancy();

        var index = this.indexMap[key];
        var value = this.values[index];

        if (this.indexMap.Remove(key))
        {
            this.values.RemoveAt(index);

            var keys = this.indexMap.Keys.ToList();

            foreach (var existingKey in keys)
            {
                if (this.indexMap[existingKey] > index)
                    this.indexMap[existingKey]--;
            }

            this.OnPropertyChanged(CountString);
            this.OnPropertyChanged(KeysString);
            this.OnPropertyChanged(ValuesString);
            this.OnPropertyChanged(IndexerName);
            this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, key, value, index);

            return true;
        }

        return false;
    }
    protected virtual bool RemoveItem(KeyValuePair<TKey, TValue> item)
    {
        this.CheckReentrancy();

        if (this.indexMap.ContainsKey(item.Key) && this.values[this.indexMap[item.Key]].Equals(item.Value))
        {
            var index = this.indexMap[item.Key];
            var value = this.values[index];

            this.indexMap.Remove(item.Key);
            this.values.RemoveAt(index);

            var keys = this.indexMap.Keys.ToList();

            foreach (var existingKey in keys)
            {
                if (this.indexMap[existingKey] > index)
                    this.indexMap[existingKey]--;
            }

            this.OnPropertyChanged(CountString);
            this.OnPropertyChanged(KeysString);
            this.OnPropertyChanged(ValuesString);
            this.OnPropertyChanged(IndexerName);
            this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, item.Key, item.Value, index);

            return true;
        }

        return false;
    }
    protected virtual void RemoveAllItems()
    {

        this.CheckReentrancy();
        this.values.Clear();
        this.indexMap.Clear();

        this.OnPropertyChanged(CountString);
        this.OnPropertyChanged(KeysString);
        this.OnPropertyChanged(ValuesString);
        this.OnPropertyChanged(IndexerName);
        this.OnCollectionChanged(NotifyCollectionChangedAction.Reset);
    }

    protected virtual void ChangeItem(TKey key, TValue newValue)
    {

        this.CheckReentrancy();

        if (!this.indexMap.ContainsKey(key))
            this.AddItem(key, newValue);
        else
        {
            var index = this.indexMap[key];
            var oldValue = this.values[index];
            this.values[index] = newValue;

            this.OnPropertyChanged(ValuesString);
            this.OnPropertyChanged(IndexerName);
            this.OnCollectionChanged(NotifyCollectionChangedAction.Replace, key, oldValue, newValue, index);
        }
    }

    protected IDisposable BlockReentrancy()
    {
        this._monitor.Enter();
        return (IDisposable)this._monitor;
    }

    protected void CheckReentrancy()
    {
        // ISSUE: reference to a compiler-generated field
        // ISSUE: reference to a compiler-generated field
        if (this._monitor.Busy && this.CollectionChanged != null && this.CollectionChanged.GetInvocationList().Length > 1)
            throw new InvalidOperationException("ObservableCollectionReentrancyNotAllowed");
    }


    #endregion

    #region IDictionary<TKey,TValue> Members

    public void Add(TKey key, TValue value)
    {
        this.AddItem(key, value);
    }

    public bool ContainsKey(TKey key)
    {
        return this.indexMap.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        return this.RemoveItem(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        int index;
        if (this.indexMap.TryGetValue(key, out index))
        {
            value = this.values[index];
            return true;
        }
        else
        {
            value = default(TValue);
            return false;
        }
    }


    public ICollection<TKey> Keys
    {
        get { return this.indexMap.Keys; }
    }

    public ICollection<TValue> Values
    {
        get { return this.values; }
    }

    public TValue this[TKey key]
    {
        get
        {
            var index = this.indexMap[key];
            return this.values[index];
        }
        set
        {
            this.ChangeItem(key, value);
        }
    }

    #endregion

    #region ICollection<KeyValuePair<TKey,TValue>> Members

    public void Clear()
    {
        this.RemoveAllItems();
    }

    public int Count
    {
        get { return this.indexMap.Count; }
    }

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
    {
        this.Add(item.Key, item.Value);
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        return this.indexMap.ContainsKey(item.Key) && this.values[this.indexMap[item.Key]].Equals(item.Value);
    }

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach (var kvp in this.indexMap)
        {
            array[arrayIndex] = new KeyValuePair<TKey, TValue>(kvp.Key, this.values[kvp.Value]);
            arrayIndex++;
        }
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
    {
        get { return false; }
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        return this.RemoveItem(item);
    }

    #endregion

    #region IEnumerable<KeyValuePair<TKey,TValue>> Members

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var kvp in this.indexMap)
        {
            var pair = new KeyValuePair<TKey, TValue>(kvp.Key, this.values[kvp.Value]);
            yield return pair;
        }
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    #endregion


    #region INotifyCollectionChanged Members

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        var handler = CollectionChanged;
        if (handler == null)
            return;

        using (this.BlockReentrancy())
        {
            handler(this, e);
        }
    }

    protected void OnCollectionChanged(NotifyCollectionChangedAction action)
    {
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action));
    }

    protected void OnCollectionChanged(NotifyCollectionChangedAction action, TKey key, TValue value, int index)
    {
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, new KeyValuePair<TKey, TValue>(key, value), index));
    }
    protected void OnCollectionChanged(NotifyCollectionChangedAction action, TKey key, TValue oldValue, TValue newValue, int index)
    {
        var newPair = new KeyValuePair<TKey, TValue>(key, newValue);
        var oldPair = new KeyValuePair<TKey, TValue>(key, oldValue);

        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newPair, oldPair, index));
    }



    #endregion


    #region INotifyPropertyChanged Members

    //Copied from Stack Overflow answer: http://stackoverflow.com/questions/1315621/implementing-inotifypropertychanged-does-a-better-way-exist/1316417#1316417
    //Author: Marc Gravell (http://stackoverflow.com/users/23354/marc-gravell)

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    private class SimpleMonitor : IDisposable
    {
        private int _busyCount;

        public bool Busy
        {
            get
            {
                return this._busyCount > 0;
            }
        }

        public void Enter()
        {
            this._busyCount = this._busyCount + 1;
        }

        public void Dispose()
        {
            this._busyCount = this._busyCount - 1;
        }
    }

}