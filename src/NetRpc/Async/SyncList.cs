using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace NetRpc
{
    public class SyncList<T> : List<T>
    {
        #region Fields

        private object? _syncRoot;

        #endregion Fields

        #region Indexers

        public new T this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return base[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    base[index] = value;
                }
            }
        }

        #endregion Indexers

        #region Constructors

        public SyncList()
        {
        }

        public SyncList(int capacity)
            : base(capacity)
        {
        }

        public SyncList(IEnumerable<T> collection)
            : base(collection)
        {
        }

        #endregion Constructors

        #region Properties Public

        public object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                return _syncRoot;
            }
        }

        public new int Capacity
        {
            get
            {
                lock (SyncRoot)
                    return base.Capacity;
            }
            set
            {
                lock (SyncRoot)
                    base.Capacity = value;
            }
        }

        public new int Count
        {
            get
            {
                lock (SyncRoot)
                    return base.Count;
            }
        }

        #endregion Properties Public

        #region Methods Public

        public new void Add(T item)
        {
            lock (SyncRoot)
            {
                base.Add(item);
            }
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            lock (SyncRoot)
            {
                base.AddRange(collection);
            }
        }

        public new ReadOnlyCollection<T> AsReadOnly()
        {
            lock (SyncRoot)
            {
                return base.AsReadOnly();
            }
        }

        public new int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            lock (SyncRoot)
            {
                return base.BinarySearch(index, count, item, comparer);
            }
        }

        public new int BinarySearch(T item)
        {
            lock (SyncRoot)
            {
                return base.BinarySearch(item);
            }
        }

        public new int BinarySearch(T item, IComparer<T> comparer)
        {
            lock (SyncRoot)
            {
                return base.BinarySearch(item, comparer);
            }
        }

        public new void Clear()
        {
            lock (SyncRoot)
            {
                base.Clear();
            }
        }

        public new bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return base.Contains(item);
            }
        }

        public new void CopyTo(T[] array)
        {
            lock (SyncRoot)
            {
                base.CopyTo(array);
            }
        }

        public new void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            lock (SyncRoot)
            {
                base.CopyTo(index, array, arrayIndex, count);
            }
        }

        public new void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                base.CopyTo(array, arrayIndex);
            }
        }

        public new void ForEach(Action<T> action)
        {
            lock (SyncRoot)
            {
                base.ForEach(action);
            }
        }

        public new List<T> GetRange(int index, int count)
        {
            lock (SyncRoot)
            {
                return base.GetRange(index, count);
            }
        }

        public new int IndexOf(T item)
        {
            lock (SyncRoot)
            {
                return base.IndexOf(item);
            }
        }

        public new int IndexOf(T item, int index)
        {
            lock (SyncRoot)
            {
                return base.IndexOf(item, index);
            }
        }

        public new int IndexOf(T item, int index, int count)
        {
            lock (SyncRoot)
            {
                return base.IndexOf(item, index, count);
            }
        }

        public new void Insert(int index, T item)
        {
            lock (SyncRoot)
            {
                base.Insert(index, item);
            }
        }

        public new void InsertRange(int index, IEnumerable<T> collection)
        {
            lock (SyncRoot)
            {
                base.InsertRange(index, collection);
            }
        }

        public new int LastIndexOf(T item)
        {
            lock (SyncRoot)
            {
                return base.LastIndexOf(item);
            }
        }

        public new int LastIndexOf(T item, int index)
        {
            lock (SyncRoot)
            {
                return base.LastIndexOf(item, index);
            }
        }

        public new int LastIndexOf(T item, int index, int count)
        {
            lock (SyncRoot)
            {
                return base.LastIndexOf(item, index, count);
            }
        }

        public new bool Remove(T item)
        {
            lock (SyncRoot)
            {
                return base.Remove(item);
            }
        }

        public new void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                base.RemoveAt(index);
            }
        }

        public new void RemoveRange(int index, int count)
        {
            lock (SyncRoot)
            {
                base.RemoveRange(index, count);
            }
        }

        public new void RemoveAll(Predicate<T> match)
        {
            lock (SyncRoot)
            {
                base.RemoveAll(match);
            }
        }

        public new void Reverse()
        {
            lock (SyncRoot)
            {
                base.Reverse();
            }
        }

        public new void Reverse(int index, int count)
        {
            lock (SyncRoot)
            {
                base.Reverse(index, count);
            }
        }

        public new void Sort()
        {
            lock (SyncRoot)
            {
                base.Sort();
            }
        }

        public new void Sort(IComparer<T> comparer)
        {
            lock (SyncRoot)
            {
                base.Sort(comparer);
            }
        }

        public new void Sort(int index, int count, IComparer<T> comparer)
        {
            lock (SyncRoot)
            {
                base.Sort(index, count, comparer);
            }
        }

        public new void Sort(Comparison<T> comparison)
        {
            lock (SyncRoot)
            {
                base.Sort(comparison);
            }
        }

        public new T[] ToArray()
        {
            lock (SyncRoot)
            {
                return base.ToArray();
            }
        }

        public new void TrimExcess()
        {
            lock (SyncRoot)
            {
                base.TrimExcess();
            }
        }

        #endregion Methods Public
    }
}