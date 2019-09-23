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

namespace AI4E.Utils.Async
{
    public sealed class AsyncLifetimeManager : IAsyncInitialization, IAsyncDisposable, IDisposable
    {
        private readonly DisposableAsyncLazy<byte> _underlyingManager;

        #region C'tor

        public AsyncLifetimeManager(Func<CancellationToken, Task> initialization, Func<Task> disposal, bool executeOnCallingThread = true)
        {
            var options = GetOptions(executeOnCallingThread);
            _underlyingManager = new DisposableAsyncLazy<byte>(
                factory: AsFactory(initialization),
                disposal: AsDisposal(disposal),
                options);
        }

        public AsyncLifetimeManager(Func<Task> initialization, Func<Task> disposal, bool executeOnCallingThread = true)
        {
            var options = GetOptions(executeOnCallingThread);
            _underlyingManager = new DisposableAsyncLazy<byte>(
                factory: AsFactory(initialization),
                disposal: AsDisposal(disposal),
                options);
        }

        public AsyncLifetimeManager(Func<CancellationToken, Task> initialization, bool executeOnCallingThread = true)
        {
            var options = GetOptions(executeOnCallingThread);
            _underlyingManager = new DisposableAsyncLazy<byte>(
                factory: AsFactory(initialization),
                options);
        }

        public AsyncLifetimeManager(Func<Task> initialization, bool executeOnCallingThread = true)
        {
            var options = GetOptions(executeOnCallingThread);
            _underlyingManager = new DisposableAsyncLazy<byte>(
                factory: AsFactory(initialization),
                options);
        }

        #endregion

        #region Helpers

        private static DisposableAsyncLazyOptions GetOptions(bool executeOnCallingThread)
        {
            var options = DisposableAsyncLazyOptions.Autostart;

            if (executeOnCallingThread)
            {
                options |= DisposableAsyncLazyOptions.ExecuteOnCallingThread;
            }

            return options;
        }

        private static Func<CancellationToken, Task<byte>> AsFactory(Func<Task> initialization)
        {
            return async cancellation =>
            {
                await initialization().ConfigureAwait(false);
                return 0;
            };
        }

        private static Func<CancellationToken, Task<byte>> AsFactory(Func<CancellationToken, Task> initialization)
        {
            return async cancellation =>
            {
                await initialization(cancellation).ConfigureAwait(false);
                return 0;
            };
        }

        private static Func<byte, Task> AsDisposal(Func<Task> disposal)
        {
            return _ => disposal();
        }

        #endregion

        public Task Initialization => _underlyingManager.Task;

        #region Disposal

        public Task Disposal => _underlyingManager.Disposal;

        public void Dispose()
        {
            _underlyingManager.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _underlyingManager.DisposeAsync();
        }

        #endregion
    }
}
