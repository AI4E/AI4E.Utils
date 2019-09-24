/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * Copyright (c) 2018-2019 Andreas Truetschel and contributors.
 * 
 * MIT License
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace AI4E.Utils.ApplicationParts
{
    internal sealed class ApplicationPartsList : IList<ApplicationPart>
    {
        private volatile ImmutableList<ApplicationPart> _inner = ImmutableList<ApplicationPart>.Empty;

        public int IndexOf(ApplicationPart item)
        {
            return _inner.IndexOf(item);
        }

        public void Insert(int index, ApplicationPart item)
        {
            ImmutableList<ApplicationPart> current = _inner, start, desired;

            do
            {
                start = current;
                desired = start.Insert(index, item);
                current = Interlocked.CompareExchange(ref _inner, desired, start);
            }
            while (start != current);

            OnCollectionChanged();
        }

        public void RemoveAt(int index)
        {
            ImmutableList<ApplicationPart> current = _inner, start, desired;

            do
            {
                start = current;
                desired = start.RemoveAt(index);
                current = Interlocked.CompareExchange(ref _inner, desired, start);
            }
            while (start != current);

            OnCollectionChanged();
        }

        public ApplicationPart this[int index]
        {
            get => _inner[index];
            set
            {
                ImmutableList<ApplicationPart> current = _inner, start, desired;

                do
                {
                    start = current;
                    desired = start.SetItem(index, value);
                    current = Interlocked.CompareExchange(ref _inner, desired, start);
                }
                while (start != current);

                OnCollectionChanged();
            }
        }

        public void Add(ApplicationPart item)
        {
            ImmutableList<ApplicationPart> current = _inner, start, desired;

            do
            {
                start = current;
                desired = start.Add(item);
                current = Interlocked.CompareExchange(ref _inner, desired, start);
            }
            while (start != current);

            OnCollectionChanged();
        }

        public void Clear()
        {
            var previous = Interlocked.Exchange(ref _inner, ImmutableList<ApplicationPart>.Empty);

            if (previous.Any())
            {
                OnCollectionChanged();
            }
        }

        public bool Contains(ApplicationPart item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(ApplicationPart[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public bool Remove(ApplicationPart item)
        {
            ImmutableList<ApplicationPart> current = _inner, start, desired;

            do
            {
                start = current;

                desired = start.Remove(item);

                if (desired == start)
                    return false;

                current = Interlocked.CompareExchange(ref _inner, desired, start);
            }
            while (start != current);

            OnCollectionChanged();

            return true;
        }

        public int Count => _inner.Count;

        public bool IsReadOnly => false;

        public IEnumerator<ApplicationPart> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        public event EventHandler? CollectionChanged;

        private void OnCollectionChanged()
        {
            CollectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
