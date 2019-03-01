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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace AI4E.Utils.AsyncEnumerable
{
    public sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly Func<IAsyncEnumerator<T>> _factory;

        public AsyncEnumerable(Func<IAsyncEnumerator<T>> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factory = factory;
        }

        public AsyncEnumerable(Func<CancellationToken, IAsyncEnumerator<T>> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factory = () => new CancellableAsyncEnumerator(factory);
        }

        public IAsyncEnumerator<T> GetEnumerator()
        {
            return _factory();
        }

        private sealed class CancellableAsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly Func<CancellationToken, IAsyncEnumerator<T>> _factory;
            private IAsyncEnumerator<T> _enumerator;

            public CancellableAsyncEnumerator(Func<CancellationToken, IAsyncEnumerator<T>> factory)
            {
                Assert(factory != null);
                _factory = factory;
            }

            public Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                if (_enumerator == null)
                {
                    _enumerator = _factory(cancellationToken);

                    if (_enumerator == null)
                        throw new InvalidOperationException();
                }

                return _enumerator.MoveNext(cancellationToken);
            }

            public T Current
            {
                get
                {
                    if (_enumerator == null)
                    {
                        return default;
                    }

                    return _enumerator.Current;
                }
            }

            public void Dispose()
            {
                _enumerator?.Dispose();
            }
        }
    }
}
