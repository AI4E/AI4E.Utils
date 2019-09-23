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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Async
{
#pragma warning disable CA1001 
    public readonly struct AsyncInitializationHelper : IAsyncInitialization, IEquatable<AsyncInitializationHelper>
#pragma warning restore CA1001
    {
        private readonly Task? _initialization; // This is null in case of a default struct value
        private readonly CancellationTokenSource? _cancellation;

        public AsyncInitializationHelper(Func<CancellationToken, Task> initialization)
        {
            if (initialization == null)
                throw new ArgumentNullException(nameof(initialization));

            this = default;

            _cancellation = new CancellationTokenSource();
            _initialization = InitInternalAsync(initialization);
        }

        public AsyncInitializationHelper(Func<Task> initialization)
        {
            if (initialization == null)
                throw new ArgumentNullException(nameof(initialization));

            this = default;

            _cancellation = null;
            _initialization = InitInternalAsync(initialization);
        }

        internal AsyncInitializationHelper(Task? initialization, CancellationTokenSource? cancellation)
        {
            _initialization = initialization;
            _cancellation = cancellation;
        }

        private async Task InitInternalAsync(Func<CancellationToken, Task> initialization)
        {
            Debug.Assert(_cancellation != null);

            await Task.Yield();

            try
            {
                await initialization(_cancellation!.Token).ConfigureAwait(false);
            }
            finally
            {
                _cancellation!.Dispose();
            }
        }

        private async Task InitInternalAsync(Func<Task> initialization)
        {
            await Task.Yield();
            await initialization().ConfigureAwait(false);
        }

        public Task Initialization => _initialization ?? Task.CompletedTask;

        public void Cancel()
        {
            _cancellation?.Cancel();
        }

        public async Task<bool> CancelAsync()
        {
            Cancel();

            try
            {
                await Initialization.ConfigureAwait(false);
                return true;
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool Equals(AsyncInitializationHelper other)
        {
            return other._initialization == _initialization;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is AsyncInitializationHelper asyncInitializationHelper
                && Equals(asyncInitializationHelper);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _initialization?.GetHashCode() ?? 0;
        }

        public static bool operator ==(AsyncInitializationHelper left, AsyncInitializationHelper right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AsyncInitializationHelper left, AsyncInitializationHelper right)
        {
            return !left.Equals(right);
        }
    }

#pragma warning disable CA1001
    public readonly struct AsyncInitializationHelper<T> : IAsyncInitialization, IEquatable<AsyncInitializationHelper<T>>
#pragma warning restore CA1001
    {
        private readonly Task<T>? _initialization; // This is null in case of a default struct value
        private readonly CancellationTokenSource? _cancellation;

        public AsyncInitializationHelper(Func<CancellationToken, Task<T>> initialization)
        {
            if (initialization == null)
                throw new ArgumentNullException(nameof(initialization));

            this = default;

            _cancellation = new CancellationTokenSource();
            _initialization = InitInternalAsync(initialization);
        }

        public AsyncInitializationHelper(Func<Task<T>> initialization)
        {
            if (initialization == null)
                throw new ArgumentNullException(nameof(initialization));

            this = default;

            _cancellation = null;
            _initialization = InitInternalAsync(initialization);
        }

        public Task<T> Initialization => _initialization ?? Task.FromResult<T>(default!); // TODO: We may not return null here!

        private async Task<T> InitInternalAsync(Func<CancellationToken, Task<T>> initialization)
        {
            Debug.Assert(_cancellation != null);

            await Task.Yield();

            try
            {
                return await initialization(_cancellation!.Token).ConfigureAwait(false);
            }
            finally
            {
                _cancellation!.Dispose();
            }
        }

        private async Task<T> InitInternalAsync(Func<Task<T>> initialization)
        {
            await Task.Yield();

            return await initialization().ConfigureAwait(false);
        }

        Task IAsyncInitialization.Initialization => Initialization;

        public void Cancel()
        {
            _cancellation?.Cancel();
        }

        public async Task<(bool success, T result)> CancelAsync()
        {
            Cancel();

            try
            {
                var result = await Initialization.ConfigureAwait(false);
                return (true, result);
            }
#pragma warning disable CA1031
            catch
#pragma warning restore CA1031
            {
                return (false, default);
            }
        }

        public static implicit operator AsyncInitializationHelper(AsyncInitializationHelper<T> source)
        {
            return source.ToAsyncInitializationHelper();
        }

        public AsyncInitializationHelper ToAsyncInitializationHelper()
        {
            return new AsyncInitializationHelper(_initialization, _cancellation);
        }

        /// <inheritdoc />
        public bool Equals(AsyncInitializationHelper<T> other)
        {
            return other._initialization == _initialization;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is AsyncInitializationHelper asyncInitializationHelper
                && Equals(asyncInitializationHelper);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _initialization?.GetHashCode() ?? 0;
        }

        public static bool operator ==(AsyncInitializationHelper<T> left, AsyncInitializationHelper<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AsyncInitializationHelper<T> left, AsyncInitializationHelper<T> right)
        {
            return !left.Equals(right);
        }
    }
}
