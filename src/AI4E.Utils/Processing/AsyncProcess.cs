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

namespace AI4E.Utils.Processing
{
    public sealed class AsyncProcess : IAsyncProcess
    {
        private readonly Func<CancellationToken, Task> _operation;
        private readonly object _lock = new object();

        private Task _execution = Task.CompletedTask;
        private CancellationTokenSource _cancellationSource;

        private TaskCompletionSource<object> _startNotificationSource;
        private TaskCompletionSource<object> _terminationNotificationSource;

        public AsyncProcess(Func<CancellationToken, Task> operation, bool start = false)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _operation = operation;

            if (start)
            {
                Start();
            }
        }

        public Task Startup => _startNotificationSource?.Task ?? Task.CompletedTask;
        public Task Termination => _terminationNotificationSource?.Task ?? Task.CompletedTask;

        public AsyncProcessState State
        {
            get
            {
                if (_execution.IsRunning())
                    return AsyncProcessState.Running;

                if (_execution.IsCompleted)
                    return AsyncProcessState.Terminated;

                return AsyncProcessState.Failed;
            }
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_execution.IsRunning())
                    return;

                _startNotificationSource = new TaskCompletionSource<object>();
                _terminationNotificationSource = new TaskCompletionSource<object>();
                _cancellationSource = new CancellationTokenSource();
                _execution = Execute();
            }
        }

        public Task StartAsync(CancellationToken cancellation)
        {
            Start();

            var result = _startNotificationSource.Task;

            if (cancellation.CanBeCanceled)
            {
                result = result.WithCancellation(cancellation);
            }

            return result;
        }

        public void Terminate()
        {
            lock (_lock)
            {
                if (!_execution.IsRunning())
                    return;

                _cancellationSource.Cancel();
            }
        }

        public Task TerminateAsync()
        {
            Terminate();
            return _terminationNotificationSource?.Task ?? Task.CompletedTask;
        }

        public Task TerminateAsync(CancellationToken cancellation)
        {
            var result = TerminateAsync();

            if (cancellation.CanBeCanceled)
            {
                result = result.WithCancellation(cancellation);
            }

            return result;
        }

        private async Task Execute()
        {
            try
            {
                var cancellation = _cancellationSource.Token;

                await Task.Yield();

                try
                {
                    _startNotificationSource.SetResult(null);

                    await _operation(cancellation).ConfigureAwait(false);

                    if (!cancellation.IsCancellationRequested)
                    {
                        throw new UnexpectedProcessTerminationException();
                    }
                }
                catch (OperationCanceledException) when (_cancellationSource.IsCancellationRequested) { }
                catch (Exception exc)
                {
                    _terminationNotificationSource.SetException(exc);
                }
            }
            finally
            {
                _terminationNotificationSource.TrySetResult(null);
            }
        }
    }
}
