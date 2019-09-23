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

namespace Microsoft.Extensions.ObjectPool
{
    /// <summary>
    /// Provides extensions for the <see cref="ObjectPool{T}"/> type.
    /// </summary>
    public static class ObjectPoolExtension
    {
        /// <summary>
        /// Rent an object from the pool and returns an object that can be used to return the object.
        /// </summary>
        /// <typeparam name="T">The type of object to rent.</typeparam>
        /// <param name="objectPool">The object pool.</param>
        /// <param name="obj">Contains the rented object.</param>
        /// <returns>A instance of type <see cref="PooledObjectReturner{T}"/> that can be used to return the rented object to the pool.</returns>
#pragma warning disable CA1720
        public static PooledObjectReturner<T> Get<T>(this ObjectPool<T> objectPool, out T obj)
#pragma warning restore CA1720
            where T : class
        {
            if (objectPool == null)
                throw new ArgumentNullException(nameof(objectPool));

            obj = objectPool.Get();

            return new PooledObjectReturner<T>(objectPool, obj);
        }
    }

    internal sealed class PooledObjectReturnerSource
    {
        private static readonly ObjectPool<PooledObjectReturnerSource> _pool
            = new DefaultObjectPool<PooledObjectReturnerSource>(new PooledObjectReturnerSourcePooledObjectPolicy());

        public int Token { get; private set; } = 0;

        public bool IsDisposed(int token)
        {
            return Token != token;
        }

        public void Dispose(int token)
        {
            if (token == Token)
            {
                Token++;
                _pool.Return(this);
            }
        }

        public static PooledObjectReturnerSource Allocate()
        {
            return _pool.Get();
        }

        private sealed class PooledObjectReturnerSourcePooledObjectPolicy : IPooledObjectPolicy<PooledObjectReturnerSource>
        {
            public PooledObjectReturnerSource Create()
            {
                return new PooledObjectReturnerSource();
            }

            public bool Return(PooledObjectReturnerSource obj)
            {
                return obj.Token != 0;
            }
        }
    }

    /// <summary>
    /// Represents a pooled object returner that returns the object to the pool when disposed.
    /// </summary>
    /// <typeparam name="T">The type of pooled object.</typeparam>
    public readonly struct PooledObjectReturner<T> : IDisposable, IEquatable<PooledObjectReturner<T>>
        where T : class
    {
        private readonly ObjectPool<T>? _objectPool;
        private readonly T? _obj;
        private readonly int _token;
        private readonly PooledObjectReturnerSource? _source;

        internal PooledObjectReturner(ObjectPool<T> objectPool, T obj)
        {
            _objectPool = objectPool;
            _obj = obj;
            _source = PooledObjectReturnerSource.Allocate();
            _token = _source.Token;
        }

        /// <summary>
        /// Returns the pooled object to the pool.
        /// </summary>
        public void Dispose()
        {
            if (_objectPool == null
                || _obj is null
                || _source == null
                || _source.IsDisposed(_token))
            {
                return;
            }

            _objectPool.Return(_obj);
            _source.Dispose(_token);
        }

        public bool Equals(PooledObjectReturner<T> other)
        {
            return (_source, _token) == (other._source, other._token);
        }

        public override bool Equals(object? obj)
        {
            return obj is PooledObjectReturner<T> pooledObjectReturner
                && Equals(pooledObjectReturner);
        }

        public override int GetHashCode()
        {
            return (_source, _token).GetHashCode();
        }

        public static bool operator ==(PooledObjectReturner<T> left, PooledObjectReturner<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PooledObjectReturner<T> left, PooledObjectReturner<T> right)
        {
            return !left.Equals(right);
        }
    }
}
