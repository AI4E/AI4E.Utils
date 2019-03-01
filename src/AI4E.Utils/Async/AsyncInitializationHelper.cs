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
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace AI4E.Utils.Async
{
    public readonly struct AsyncInitializationHelper : IAsyncInitialization
    {
        private readonly Task _initialization;
        private readonly CancellationTokenSource _cancellation;

        public AsyncInitializationHelper(Func<CancellationToken, Task> initialization)
        {
            if (initialization == null)
                throw new ArgumentNullException(nameof(initialization));

            _cancellation = new CancellationTokenSource();
            _initialization = initialization(_cancellation.Token);
        }

        public AsyncInitializationHelper(Func<Task> initialization)
        {
            if (initialization == null)
                throw new ArgumentNullException(nameof(initialization));

            _cancellation = null;
            _initialization = initialization();
        }

        internal AsyncInitializationHelper(Task initialization, CancellationTokenSource cancellation)
        {
            _initialization = initialization;
            _cancellation = cancellation;
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
                await Initialization;
                return true;
            }
            catch { }

            return false;
        }
    }

    public readonly struct AsyncInitializationHelper<T> : IAsyncInitialization
    {
        private readonly Task<T> _initialization;
        private readonly CancellationTokenSource _cancellation;

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
            _initialization = InitInternalAsync(initialization); ;
        }

        public Task<T> Initialization => _initialization ?? Task.FromResult<T>(default);

        private async Task<T> InitInternalAsync(Func<CancellationToken, Task<T>> initialization)
        {
            Assert(initialization != null);

            await Task.Yield();

            return await initialization(_cancellation.Token);
        }

        private async Task<T> InitInternalAsync(Func<Task<T>> initialization)
        {
            Assert(initialization != null);

            await Task.Yield();

            return await initialization();
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
                var result = await Initialization;
                return (true, result);
            }
            catch { }

            return (false, default);
        }

        public static implicit operator AsyncInitializationHelper(AsyncInitializationHelper<T> source)
        {
            return new AsyncInitializationHelper(source._initialization, source._cancellation);
        }
    }
}
