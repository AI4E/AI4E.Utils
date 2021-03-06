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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AI4E.Utils
{
#pragma warning disable CA1710
    public sealed class WeakDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
#pragma warning restore CA1710
        where TValue : class
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, WeakReference<TValue>> _entries;
        private readonly ConcurrentQueue<TKey> _cleanupQueue = new ConcurrentQueue<TKey>();
        private readonly Finalizer<TValue> _finalizer = new Finalizer<TValue>();

        public WeakDictionary()
        {
            _entries = new ConcurrentDictionary<TKey, WeakReference<TValue>>();
        }

        public WeakDictionary(IEqualityComparer<TKey> equalityComparer)
        {
            if (equalityComparer == null)
                throw new ArgumentNullException(nameof(equalityComparer));

            _entries = new ConcurrentDictionary<TKey, WeakReference<TValue>>(equalityComparer);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] [NotNullWhen(true)] out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            Cleanup();

            value = default!;

            return _entries.TryGetValue(key, out var weakReference) &&
                   weakReference.TryGetTarget(out value!);
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            Cleanup();
            return GetOrAddInternal(key, factory);
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Cleanup();
            return GetOrAddInternal(key, _ => value);
        }

        public bool TryRemove(TKey key, [MaybeNullWhen(false)] [NotNullWhen(true)] out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            Cleanup();

            value = default!;
            return _entries.TryRemove(key, out var weakReference)
                && weakReference.TryGetTarget(out value!);
        }

        public bool TryRemove(TKey key, TValue comparand)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (comparand == null)
                throw new ArgumentNullException(nameof(comparand));

            Cleanup();

            WeakReference<TValue>? weakReference;

            do
            {
                if (!_entries.TryGetValue(key, out weakReference)
                    || !weakReference.TryGetTarget(out var value)
                    || !value.Equals(comparand))
                {
                    return false;
                }
            }
            while (!_entries.Remove(key, weakReference));

            return true;
        }

        private TValue GetOrAddInternal(TKey key, Func<TKey, TValue> factory)
        {
            TValue? newValue = null;
            WeakReference<TValue>? newWeakReference = null;
            Action<TValue>? cleanCallback = null;
            var valueCreated = false;

            while (true)
            {
                if (_entries.TryGetValue(key, out var weakReference))
                {
                    if (weakReference.TryGetTarget(out var value))
                    {
                        if (valueCreated)
                        {
                            _finalizer.RemoveHandler(newValue!, cleanCallback!);
                        }

                        return value;
                    }

                    if (!valueCreated)
                    {
                        (newValue, newWeakReference, cleanCallback) = CreateValue(key, factory);
                        valueCreated = true;
                    }

                    if (_entries.TryUpdate(key, newWeakReference!, weakReference))
                    {
                        return newValue!;
                    }
                }
                else
                {
                    if (!valueCreated)
                    {
                        (newValue, newWeakReference, cleanCallback) = CreateValue(key, factory);
                        valueCreated = true;
                    }

                    if (_entries.TryAdd(key, newWeakReference!))
                    {
                        return newValue!;
                    }
                }
            }
        }

        private (TValue newValue, WeakReference<TValue> newWeakReference, Action<TValue> cleanCallback) CreateValue(TKey key, Func<TKey, TValue> factory)
        {
            var newValue = factory(key);

            if (newValue == null)
            {
                throw new InvalidOperationException($"The value provided by '{nameof(factory)}' must not be null.");
            }

            var newWeakReference = new WeakReference<TValue>(newValue);

#pragma warning disable IDE0039

            Action<TValue> cleanCallback = _ => _cleanupQueue.Enqueue(key);

#pragma warning restore IDE0039

            _finalizer.AddHandler(newValue, cleanCallback);

            return (newValue, newWeakReference, cleanCallback);
        }

        private void Cleanup()
        {
            while (_cleanupQueue.TryDequeue(out var key))
            {
                while (_entries.TryGetValue(key, out var weakReference) && !weakReference.TryGetTarget(out _) && !_entries.Remove(key, weakReference)) ;
            }
        }

        #region IEnumerable

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var entry in _entries)
            {
                if (entry.Value.TryGetTarget(out var value))
                {
                    yield return new KeyValuePair<TKey, TValue>(entry.Key, value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public sealed class Finalizer<T> where T : class
    {
        private readonly ConditionalWeakTable<T, SingleFinalizer> _weakTable = new ConditionalWeakTable<T, SingleFinalizer>();

        public void AddHandler(T value, Action<T> action)
        {
            var finalizer = _weakTable.GetValue(value, _ => new SingleFinalizer(value));

            finalizer.AddHandler(action);
        }

        public void RemoveHandler(T value, Action<T> action)
        {
            if (_weakTable.TryGetValue(value, out var finalizer))
            {
                finalizer.RemoveHandler(action);
            }
        }

        private sealed class SingleFinalizer
        {
            private readonly T _value;
            private volatile Action<T>? _action;

            public SingleFinalizer(T value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _value = value;
            }

            public void AddHandler(Action<T> action)
            {
                Action<T>? current = _action, // Volatile read op
                           start,
                           desired;

                do
                {
                    start = current;

                    desired = start + action;

                    current = Interlocked.CompareExchange(ref _action, desired, start);
                }
                while (start != current);
            }

            public void RemoveHandler(Action<T> action)
            {
                Action<T>? current = _action, // Volatile read op
                           start,
                           desired;

                do
                {
                    start = current;

                    desired = start - action;

                    current = Interlocked.CompareExchange(ref _action, desired, start);
                }
                while (start != current);
            }

            ~SingleFinalizer()
            {
                _action?.Invoke(_value); // Volatile read op
            }
        }

    }
}
