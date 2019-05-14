using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

namespace NetRpc
{
    [Serializable]
    public class SyncDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializable
    {
        private object _syncRoot;

        public SyncDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer)
        {
        }

        public SyncDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
        {
        }

        public SyncDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
        {
        }

        public SyncDictionary(int capacity) : base(capacity)
        {
        }

        public SyncDictionary()
        {
        }

        protected SyncDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        { 

        }

        public SyncDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
        {
        }

        public object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                return _syncRoot;
            }
        }

        public new IEqualityComparer<TKey> Comparer
        {
            get
            {
                lock (SyncRoot)
                {
                    return base.Comparer;
                }
            }
        }

        public new int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return base.Count;
                }
            }
        }

        public new KeyCollection Keys
        {
            get
            {
                lock (SyncRoot)
                {
                    return base.Keys;
                }
            }
        }

        public new ValueCollection Values
        {
            get
            {
                lock (SyncRoot)
                {
                    return base.Values;
                }
            }
        }

        public new TValue this[TKey key]
        {
            get
            {
                lock (SyncRoot)
                {
                    return base[key];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    base[key] = value;
                }
            }
        }

        public new void Add(TKey key, TValue value)
        {
            lock (SyncRoot)
            {
                base.Add(key, value);
            }
        }

        public new void Clear()
        {
            lock (SyncRoot)
            {
                base.Clear();
            }
        }

        public new bool ContainsKey(TKey key)
        {
            lock (SyncRoot)
            {
                return base.ContainsKey(key);
            }
        }

        public new bool ContainsValue(TValue value)
        {
            lock (SyncRoot)
            {
                return base.ContainsValue(value);
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            lock (SyncRoot)
            {
                base.GetObjectData(info, context);
            }
        }

        public override void OnDeserialization(object sender)
        {
            lock (SyncRoot)
            {
                base.OnDeserialization(sender);
            }
        }

        public new bool Remove(TKey key)
        {
            lock (SyncRoot)
            {
                return base.Remove(key);
            }
        }

        public new bool TryGetValue(TKey key, out TValue value)
        {
            lock (SyncRoot)
            {
                return base.TryGetValue(key, out value);
            }
        }

        public new Enumerator GetEnumerator()
        {
            lock (SyncRoot)
                return base.GetEnumerator();
        }

        public TKey FindOrDefaultByValue(TValue value)
        {
            lock (SyncRoot)
            {
                foreach (var key in this.Keys)
                {
                    if (Equals(this[key], value))
                    {
                        return key;
                    }
                }
            }

            return default;
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            lock (SyncRoot)
                this[key] = value;
        }

        public bool TryUpdate(TKey key, TValue value)
        {
            TValue oldValue;
            return TryUpdate(key, value, out oldValue);
        }

        public bool TryUpdate(TKey key, TValue value, out TValue oldValue)
        {
            lock (SyncRoot)
            {
                if (base.ContainsKey(key))
                {
                    oldValue = base[key];
                    base.Remove(key);
                    base.Add(key, value);
                    return true;
                }

                oldValue = default(TValue);
                return false;
            }
        }
    }
}