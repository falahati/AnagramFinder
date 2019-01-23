using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace RabbitChallenge
{
    internal class GenericHashTable<TValue> : ICollection<TValue>, ICollection
    {
        private int[] _buckets;
        private int _count;
        private Entry[] _entries;
        private int _freeCount;
        private int _freeList;
        private object _syncRoot;

        public GenericHashTable() : this(0, null)
        {
        }

        public GenericHashTable(int capacity, IEqualityComparer<TValue> comparer)
        {
            if (capacity > 0)
            {
                Initialize(capacity);
            }

            Comparer = comparer ?? EqualityComparer<TValue>.Default;
        }

        public GenericHashTable(IEnumerable<TValue> dictionary, IEqualityComparer<TValue> comparer) : this(0, comparer)
        {
            foreach (var pair in dictionary)
            {
                Add(pair);
            }
        }

        public IEqualityComparer<TValue> Comparer { get; }

        /// <inheritdoc />
        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        void ICollection.CopyTo(Array array, int index)
        {
            if (array is TValue[] pairs)
            {
                CopyTo(pairs, index);
            }
            else
            {
                if (!(array is object[] objects))
                {
                    return;
                }

                var count = _count;
                var entries = _entries;

                for (var i = 0; i < count; i++)
                {
                    if (entries[i].HashCode >= 0)
                    {
                        objects[index++] = entries[i].Value;
                    }
                }
            }
        }

        /// <inheritdoc />
        bool ICollection.IsSynchronized
        {
            get => false;
        }

        /// <inheritdoc />
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                }

                return _syncRoot;
            }
        }

        /// <inheritdoc />
        // ReSharper disable once MethodNameNotMeaningful
        public void Add(TValue value)
        {
            Insert(value, true);
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_count > 0)
            {
                for (var i = 0; i < _buckets.Length; i++)
                {
                    _buckets[i] = -1;
                }

                Array.Clear(_entries, 0, _count);
                _freeList = -1;
                _count = 0;
                _freeCount = 0;
            }
        }

        /// <inheritdoc />
        public bool Contains(TValue value)
        {
            var i = FindEntry(value);

            if (i >= 0 && EqualityComparer<TValue>.Default.Equals(_entries[i].Value, value))
            {
                return true;
            }

            return false;
        }

        /// <summary>Gets the number of elements contained in the <see cref="GenericHashTable{TValue}"></see>.</summary>
        /// <returns>The number of elements contained in the <see cref="GenericHashTable{TValue}"></see>.</returns>
        void ICollection<TValue>.CopyTo(TValue[] array, int index)
        {
            CopyTo(array, index);
        }

        public int Count
        {
            get => _count - _freeCount;
        }

        /// <inheritdoc />
        bool ICollection<TValue>.IsReadOnly
        {
            get => false;
        }

        /// <inheritdoc />
        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        public bool Remove(TValue value)
        {
            if (_buckets != null)
            {
                var hashCode = Comparer.GetHashCode(value) & 0x7FFFFFFF;
                var bucket = hashCode % _buckets.Length;
                var last = -1;

                for (var i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].Next)
                {
                    if (_entries[i].HashCode == hashCode && Comparer.Equals(_entries[i].Value, value))
                    {
                        if (last < 0)
                        {
                            _buckets[bucket] = _entries[i].Next;
                        }
                        else
                        {
                            _entries[last].Next = _entries[i].Next;
                        }

                        _entries[i].HashCode = -1;
                        _entries[i].Next = _freeList;
                        _entries[i].Value = default;
                        _freeList = i;
                        _freeCount++;

                        return true;
                    }
                }
            }

            return false;
        }


        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc />
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An instance of <see cref="Enumerator"></see> object that can be used to iterate through the collection.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private void CopyTo(IList<TValue> array, int index)
        {
            var c = _count;
            var e = _entries;

            for (var i = 0; i < c; i++)
            {
                if (e[i].HashCode >= 0)
                {
                    array[index++] = e[i].Value;
                }
            }
        }

        private int FindEntry(TValue value)
        {
            if (_buckets == null)
            {
                return -1;
            }

            var hashCode = Comparer.GetHashCode(value) & 0x7FFFFFFF;

            for (var i = _buckets[hashCode % _buckets.Length]; i >= 0; i = _entries[i].Next)
            {
                if (_entries[i].HashCode == hashCode)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Initialize(int capacity)
        {
            var size = HashHelpers.GetPrime(capacity);
            _buckets = new int[size];

            for (var i = 0; i < _buckets.Length; i++)
            {
                _buckets[i] = -1;
            }

            _entries = new Entry[size];
            _freeList = -1;
        }

        // ReSharper disable once FlagArgument
        // ReSharper disable once MethodTooLong
        private void Insert(TValue value, bool add)
        {
            if (_buckets == null)
            {
                Initialize(0);
            }

            var hashCode = Comparer.GetHashCode(value) & 0x7FFFFFFF;
            var targetBucket = hashCode % _buckets.Length;

            for (var i = _buckets[targetBucket]; i >= 0; i = _entries[i].Next)
            {
                if (_entries[i].HashCode == hashCode)
                {
                    if (add)
                    {
                        return;
                    }

                    _entries[i].Value = value;

                    return;
                }
            }

            int index;

            if (_freeCount > 0)
            {
                index = _freeList;
                _freeList = _entries[index].Next;
                _freeCount--;
            }
            else
            {
                if (_count == _entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % _buckets.Length;
                }

                index = _count;
                _count++;
            }

            _entries[index].HashCode = hashCode;
            _entries[index].Next = _buckets[targetBucket];
            _entries[index].Value = value;
            _buckets[targetBucket] = index;
        }

        private void Resize()
        {
            Resize(HashHelpers.ExpandPrime(_count), false);
        }

        // ReSharper disable once FlagArgument
        // ReSharper disable once MethodTooLong
        private void Resize(int newSize, bool forceNewHashCodes)
        {
            var newBuckets = new int[newSize];

            for (var i = 0; i < newBuckets.Length; i++)
            {
                newBuckets[i] = -1;
            }

            var newEntries = new Entry[newSize];
            Array.Copy(_entries, 0, newEntries, 0, _count);

            if (forceNewHashCodes)
            {
                for (var i = 0; i < _count; i++)
                {
                    if (newEntries[i].HashCode != -1)
                    {
                        newEntries[i].HashCode = Comparer.GetHashCode(newEntries[i].Value) & 0x7FFFFFFF;
                    }
                }
            }

            for (var i = 0; i < _count; i++)
            {
                if (newEntries[i].HashCode >= 0)
                {
                    var bucket = newEntries[i].HashCode % newSize;
                    newEntries[i].Next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }

            _buckets = newBuckets;
            _entries = newEntries;
        }

        private struct Entry
        {
            public int HashCode; // Lower 31 bits of hash code, -1 if unused
            public int Next; // Index of next entry, -1 if last
            public TValue Value; // Value of entry
        }

        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly GenericHashTable<TValue> _dictionary;
            private int _index;
            public TValue Current { get; private set; }

            internal Enumerator(GenericHashTable<TValue> dictionary)
            {
                _dictionary = dictionary;
                _index = 0;
                Current = default;
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                while ((uint) _index < (uint) _dictionary._count)
                {
                    if (_dictionary._entries[_index].HashCode >= 0)
                    {
                        Current = _dictionary._entries[_index].Value;
                        _index++;

                        return true;
                    }

                    _index++;
                }

                _index = _dictionary._count + 1;
                Current = default;

                return false;
            }

            /// <inheritdoc />
            TValue IEnumerator<TValue>.Current
            {
                get => Current;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                // nothing to do
            }

            /// <inheritdoc />
            object IEnumerator.Current
            {
                get => Current;
            }

            /// <inheritdoc />
            void IEnumerator.Reset()
            {
                _index = 0;
                Current = default;
            }
        }
    }
}