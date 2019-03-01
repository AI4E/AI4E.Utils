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


/* Based on
* --------------------------------------------------------------------------------------------------------------------
* Roslyn (https://github.com/dotnet/roslyn)
* Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.
* --------------------------------------------------------------------------------------------------------------------
*/

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AI4E.Utils
{
    /// <summary>
    /// Generic implementation of object pooling pattern with predefined pool size limit. The main
    /// purpose is that limited number of frequently used objects can be kept in the pool for
    /// further recycling.
    /// 
    /// Notes: 
    /// 1) it is not the goal to keep all returned objects. Pool is not meant for storage. If there
    ///    is no space in the pool, extra returned objects will be dropped.
    /// 
    /// 2) it is implied that if object was obtained from a pool, the caller will return it back in
    ///    a relatively short time. Keeping checked out objects for long durations is ok, but 
    ///    reduces usefulness of pooling. Just new up your own.
    /// 
    /// Not returning objects to the pool in not detrimental to the pool's work, but is a bad practice. 
    /// Rationale: 
    ///    If there is no intent for reusing the object, do not use pool - just use "new". 
    /// </summary>
    public sealed class ObjectPool<T>
        where T : class
    {
        [DebuggerDisplay("{Value,nq}")]
        private struct Element
        {
            internal T _value;
        }

        // Storage for the pool objects. The first item is stored in a dedicated field because we
        // expect to be able to satisfy most requests from it.
        private T _firstItem;
        private readonly Element[] _items;

        // factory is stored for the lifetime of the pool. We will call this only when pool needs to
        // expand. compared to "new T()", Func gives more flexibility to implementers and faster
        // than "new T()".
        private readonly Func<T> _factory;
        private readonly Func<T, bool> _isReusable;
        private readonly bool _detectLeaks;

        public static int DefaultSize => ObjectPool.DefaultSize;

        public ObjectPool(Func<T> factory)
            : this(factory, _ => true, DefaultSize)
        { }

        public ObjectPool(Func<T> factory, Func<T, bool> isReusable, int size, bool detectLeaks = false)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (isReusable == null)
                throw new ArgumentNullException(nameof(isReusable));

            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));

            _factory = factory;
            _isReusable = isReusable;
            _detectLeaks = detectLeaks;
            _items = new Element[size - 1];
        }

        private T CreateInstance()
        {
            var inst = _factory();
            return inst;
        }

        /// <summary>
        /// Produces an instance.
        /// </summary>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that Free will try to store recycled objects close to the start thus statistically 
        /// reducing how far we will typically search.
        /// </remarks>
        public T Rent()
        {
            // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
            // Note that the initial read is optimistically not synchronized. That is intentional. 
            // We will interlock only when we have a candidate. in a worst case we may miss some
            // recently returned objects. Not a big deal.
            var inst = _firstItem;
            if (inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst))
            {
                inst = RentSlow();
            }

            if (_detectLeaks)
            {
                var tracker = new LeakTracker();
                _leakTrackers.Add(inst, tracker);

                var frame = CaptureStackTrace();
                tracker._trace = frame;
            }

            return inst;
        }

        private T RentSlow()
        {
            var items = _items;

            for (var i = 0; i < items.Length; i++)
            {
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                var inst = items[i]._value;
                if (inst != null)
                {
                    if (inst == Interlocked.CompareExchange(ref items[i]._value, null, inst))
                    {
                        return inst;
                    }
                }
            }

            return CreateInstance();
        }

        /// <summary>
        /// Returns objects to the pool.
        /// </summary>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that Free will try to store recycled objects close to the start thus statistically 
        /// reducing how far we will typically search in Allocate.
        /// </remarks>
        public void Return(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (_firstItem == obj)
                return;

            var items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                var value = items[i]._value;
                if (value == null)
                {
                    break;
                }

                if (value == obj)
                {
                    return;
                }
            }

            if (_detectLeaks)
            {
                ForgetTrackedObject(obj);
            }

            if (!_isReusable(obj))
            {
                return;
            }

            if (_firstItem == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                _firstItem = obj;
            }
            else
            {
                ReturnSlow(obj);
            }
        }

        private void ReturnSlow(T obj)
        {
            var items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i]._value == null)
                {
                    // Intentionally not using interlocked here. 
                    // In a worst case scenario two objects may be stored into same slot.
                    // It is very unlikely to happen and will only mean that one of the objects will get collected.
                    items[i]._value = obj;
                    break;
                }
            }
        }

        /// <summary>
        /// Removes an object from leak tracking.  
        /// 
        /// This is called when an object is returned to the pool.  It may also be explicitly 
        /// called if an object allocated from the pool is intentionally not being returned
        /// to the pool.  This can be of use with pooled arrays if the consumer wants to 
        /// return a larger array to the pool than was originally allocated.
        /// </summary>
        internal void ForgetTrackedObject(T old, T replacement = null)
        {
            if (_leakTrackers.TryGetValue(old, out var tracker))
            {
                tracker.Dispose();
                _leakTrackers.Remove(old);
            }
            else
            {
                var trace = CaptureStackTrace();
                Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nObject of type {typeof(T)} was freed, but was not from pool. \n Callstack: \n {trace} TRACEOBJECTPOOLLEAKS_END");
            }

            if (replacement != null)
            {
                tracker = new LeakTracker();
                _leakTrackers.Add(replacement, tracker);
            }
        }

        private static Lazy<Type> _stackTraceType = new Lazy<Type>(() => Type.GetType("System.Diagnostics.StackTrace"));

        private static object CaptureStackTrace()
        {
            return Activator.CreateInstance(_stackTraceType.Value);
        }

        private static readonly ConditionalWeakTable<T, LeakTracker> _leakTrackers = new ConditionalWeakTable<T, LeakTracker>();

        private class LeakTracker : IDisposable
        {
            private volatile bool _disposed;
            internal volatile object _trace = null;

            public void Dispose()
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }

            private string GetTrace()
            {
                return _trace == null ? "" : _trace.ToString();
            }

            ~LeakTracker()
            {
                if (!_disposed && !Environment.HasShutdownStarted)
                {
                    var trace = GetTrace();

                    // If you are seeing this message it means that object has been allocated from the pool 
                    // and has not been returned back. This is not critical, but turns pool into rather 
                    // inefficient kind of "new".
                    Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nPool detected potential leaking of {typeof(T)}. \n Location of the leak: \n {GetTrace()} TRACEOBJECTPOOLLEAKS_END");
                }
            }
        }
    }

    public static class ObjectPool
    {
        public static int DefaultSize { get; } = Environment.ProcessorCount * 2;

        public static ObjectPool<T> Create<T>()
            where T : class, new()
        {
            return new ObjectPool<T>(() => new T());
        }

        public static ObjectPool<T> Create<T>(Func<T> factory)
            where T : class
        {
            return new ObjectPool<T>(factory);
        }

        public static ObjectPool<T> Create<T>(Func<T> factory, Func<T, bool> isReusable, int size, bool detectLeaks = false)
            where T : class
        {
            return new ObjectPool<T>(factory, isReusable, size, detectLeaks);
        }

        public static ObjectPool<T> Create<T>(Func<T, bool> isReusable, int size, bool detectLeaks = false)
           where T : class, new()
        {
            return new ObjectPool<T>(() => new T(), isReusable, size, detectLeaks);
        }
    }

    public static class ObjectPoolExtension
    {
        public static PooledObjectReturner<T> Rent<T>(this ObjectPool<T> objectPool, out T obj)
            where T : class
        {
            if (objectPool == null)
                throw new ArgumentNullException(nameof(objectPool));

            obj = objectPool.Rent();

            return new PooledObjectReturner<T>(objectPool, obj);
        }

        public readonly struct PooledObjectReturner<T> : IDisposable
            where T : class
        {
            private readonly ObjectPool<T> _objectPool;
            private readonly T _obj;

            public PooledObjectReturner(ObjectPool<T> objectPool, T obj)
            {
                if (objectPool == null)
                    throw new ArgumentNullException(nameof(objectPool));

                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));

                _objectPool = objectPool;
                _obj = obj;
            }

            public void Dispose()
            {
                _objectPool.Return(_obj);
            }
        }
    }
}
