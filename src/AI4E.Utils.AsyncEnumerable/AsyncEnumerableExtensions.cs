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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

#if NETSTD20
using System.Linq;
#endif

// TODO: Change namespace to System.Collections.Generic
namespace AI4E.Utils.AsyncEnumerable
{
    public static class AsyncEnumerableExtensions
    {
        // Performs an ordinary select except when an exception occurs in the selector, than it ignores the exception and continues.
        public static IAsyncEnumerable<TResult> SelectOrContinue<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, Task<TResult>> asyncSelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (asyncSelector == null)
                throw new ArgumentNullException(nameof(asyncSelector));

            return new AsyncSelectEnumerable<TSource, TResult>(source, asyncSelector);
        }

        private sealed class AsyncSelectEnumerable<TSource, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<TSource, Task<TResult>> _asyncSelector;

            public AsyncSelectEnumerable(IAsyncEnumerable<TSource> source, Func<TSource, Task<TResult>> asyncSelector)
            {
                _source = source;
                _asyncSelector = asyncSelector;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new AsyncSelectEnumerator(_source, _asyncSelector, cancellationToken);
            }

            private sealed class AsyncSelectEnumerator : IAsyncEnumerator<TResult>
            {
                private readonly IAsyncEnumerator<TSource> _enumerator;
                private readonly Func<TSource, Task<TResult>> _asyncSelector;

                public AsyncSelectEnumerator(
                    IAsyncEnumerable<TSource> source,
                    Func<TSource, Task<TResult>> asyncSelector,
                    CancellationToken cancellation
                    )
                {
                    _enumerator = source.GetAsyncEnumerator(cancellation);
                    _asyncSelector = asyncSelector;

                    Current = default;
                }

                public async ValueTask<bool> MoveNextAsync()
                {
                    bool result;

                    do
                    {
                        result = await _enumerator.MoveNextAsync();

                        if (result)
                        {
                            try
                            {
                                Current = await _asyncSelector(_enumerator.Current);
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        else
                        {
                            Current = default;
                        }

                        break;
                    }
                    while (result);

                    return result;
                }

                public TResult Current { get; private set; }

                public ValueTask DisposeAsync()
                {
                    return _enumerator.DisposeAsync();
                }
            }
        }

        // TODO: Rename to YieldAsync?
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(in this ValueTask<T> task)
        {
            return new SingleAsyncEnumerable<T>(task);
        }

        private sealed class SingleAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly ValueTask<T> _task;

            public SingleAsyncEnumerable(in ValueTask<T> task)
            {
                _task = task;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new SingleAsyncEnumerator(_task);
            }

            private sealed class SingleAsyncEnumerator : IAsyncEnumerator<T>
            {
                private readonly ValueTask<T> _task;
                private bool _initialized = false;

                public SingleAsyncEnumerator(in ValueTask<T> task)
                {
                    _task = task;
                }

                public T Current { get; private set; }

                public ValueTask DisposeAsync() { return default; }

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (_initialized)
                    {
                        Current = default;
                        return false;
                    }

                    _initialized = true;

                    Current = await _task;
                    return true;
                }
            }
        }

        public static ValueTaskAwaiter<T[]> GetAwaiter<T>(this IAsyncEnumerable<T> asyncEnumerable)
        {
            if (asyncEnumerable == null)
                throw new ArgumentNullException(nameof(asyncEnumerable));

#if NETSTD20
            return asyncEnumerable.ToArrayAsync().GetAwaiter();
#else
            async ValueTask<T[]> Preevaluate()
            {
                var list = new List<T>();

                await foreach (var t in asyncEnumerable)
                {
                    list.Add(t);
                }

                return list.ToArray();
            }

            return Preevaluate().GetAwaiter();
#endif 
        }

        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this Task<IEnumerable<T>> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            return new ComputedAsyncEnumerable<T>(enumerable);
        }

        private sealed class ComputedAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly Task<IEnumerable<T>> _enumerable;

            public ComputedAsyncEnumerable(Task<IEnumerable<T>> enumerable)
            {
                Assert(enumerable != null);
                _enumerable = enumerable;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new ComputedAsyncEnumerator(_enumerable);
            }

            private sealed class ComputedAsyncEnumerator : IAsyncEnumerator<T>
            {
                private readonly Task<IEnumerable<T>> _enumerable;
                private IEnumerator<T> _enumerator;
                private bool _isDisposed = false;

                public ComputedAsyncEnumerator(Task<IEnumerable<T>> enumerable)
                {
                    Assert(enumerable != null);
                    _enumerable = enumerable;
                }

                public async ValueTask<bool> MoveNextAsync()
                {
                    ThrowIfDisposed();

                    if (_enumerator == null)
                    {
                        _enumerator = (await _enumerable).GetEnumerator();
                    }

                    return _enumerator.MoveNext();
                }

                public T Current => ThrowIfDisposed(_enumerator == null ? default : _enumerator.Current);

                public ValueTask DisposeAsync()
                {
                    if (!_isDisposed)
                    {
                        _isDisposed = true;
                        _enumerator?.Dispose();
                    }

                    return default;
                }

                private void ThrowIfDisposed()
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);
                }

                private Q ThrowIfDisposed<Q>(Q arg)
                {
                    ThrowIfDisposed();
                    return arg;
                }
            }
        }

        public static IAsyncEnumerable<T> Evaluate<T>(this IAsyncEnumerable<Task<T>> enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            return new EvaluationAsyncEnumerable<T>(enumerable);
        }

        public static async IAsyncEnumerable<T> Evaluate<T>(this IAsyncEnumerable<ValueTask<T>> enumerable)
        {
            await foreach (var t in enumerable)
            {
                yield return await t;
            }
        }

        private sealed class EvaluationAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<Task<T>> _enumerable;

            public EvaluationAsyncEnumerable(IAsyncEnumerable<Task<T>> enumerable)
            {
                Assert(enumerable != null);
                _enumerable = enumerable;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)

            {
                return new EvaluationAsyncEnumerator(_enumerable, cancellationToken);
            }

            private sealed class EvaluationAsyncEnumerator : IAsyncEnumerator<T>
            {
                private readonly IAsyncEnumerator<Task<T>> _enumerator;

                public EvaluationAsyncEnumerator(IAsyncEnumerable<Task<T>> enumerable, CancellationToken cancellation)
                {
                    _enumerator = enumerable.GetAsyncEnumerator(cancellation);
                }

                public T Current { get; private set; }

                public ValueTask DisposeAsync()
                {
                    return _enumerator.DisposeAsync();
                }

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (!await _enumerator.MoveNextAsync())
                    {
                        return false;
                    }

                    Current = await _enumerator.Current;
                    return true;
                }
            }
        }
    }
}
