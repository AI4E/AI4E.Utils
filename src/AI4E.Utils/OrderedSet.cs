using System;
using System.Collections;
using System.Collections.Generic;

namespace AI4E.Utils
{
    // TODO: Implement ISet<T>
    // Adapted from: https://stackoverflow.com/questions/1552225/hashset-that-preserves-ordering#answer-17853085
    public class OrderedSet<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        private readonly IDictionary<T, LinkedListNode<T>> _dictionary;
        private readonly LinkedList<T> _linkedList;

        public OrderedSet()
            : this(EqualityComparer<T>.Default)
        { }

        public OrderedSet(IEqualityComparer<T> comparer)
        {
            _dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            _linkedList = new LinkedList<T>();
        }

        public int Count => _dictionary.Count;

        public virtual bool IsReadOnly => _dictionary.IsReadOnly;

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public bool Add(T item)
        {
            if (_dictionary.ContainsKey(item))
            {
                return false;
            }

            var node = _linkedList.AddLast(item);
            _dictionary.Add(item, node);
            return true;
        }

        public void Clear()
        {
            _linkedList.Clear();
            _dictionary.Clear();
        }

        public bool Remove(T item)
        {
            if (!_dictionary.Remove(item, out var node))
            {
                return false;
            }

            _linkedList.Remove(node);
            return true;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_linkedList.GetEnumerator());
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _linkedList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _linkedList.GetEnumerator();
        }

        public bool Contains(T item)
        {
            return _dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _linkedList.CopyTo(array, arrayIndex);
        }

        public readonly struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly LinkedList<T>.Enumerator _linkedListEnumerator;

            internal Enumerator(LinkedList<T>.Enumerator linkedListEnumerator)
            {
                _linkedListEnumerator = linkedListEnumerator;
            }

            public bool MoveNext()
            {
                return _linkedListEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                ((IEnumerator<T>)_linkedListEnumerator).Reset();
            }

            public T Current => _linkedListEnumerator.Current;

            object IEnumerator.Current => ((IEnumerator)_linkedListEnumerator).Current;

            public void Dispose()
            {
                _linkedListEnumerator.Dispose();
            }
        }
    }
}
