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
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Async
{
    public sealed class OneTimeOperation
    {
        private readonly Func<Task> _operation;
        private readonly TaskCompletionSource<object> _executionSource = new TaskCompletionSource<object>();
        private readonly object _lock = new object();

        private Task _executeTask;
        private volatile bool _hasStarted = false;

        public OneTimeOperation(Func<Task> operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _operation = operation;
        }

        public Task ExecuteAsync(CancellationToken cancellation)
        {
            return ExecuteAsync().WithCancellation(cancellation);
        }

        public Task ExecuteAsync()
        {
            Execute();
            return Execution;
        }

        public void Execute()
        {
            if (_hasStarted) // Volatile read op.
                return;

            lock (_executionSource)
            {
                // We use a dedicated flag for specifying whether the operation was already started 
                // instead of simply check _executeTask for beeing set already to allow 
                // recursive calls to Execute() in the executed operation.

                if (_hasStarted)
                    return;

                _hasStarted = true;

                Debug.Assert(_executeTask == null);

                _executeTask = ExecuteInternalAsync();
            }
        }

        private async Task ExecuteInternalAsync()
        {
#if DEBUG
            var executionSourceSetLocally = false;
#endif
            try
            {
                try
                {
                    await _operation();
                }
                catch (OperationCanceledException exc)
                {
                    bool successfullySetExecutionSource;

                    if (exc.CancellationToken == default)
                    {
                        successfullySetExecutionSource = _executionSource.TrySetCanceled();
                    }
                    else
                    {
                        successfullySetExecutionSource = _executionSource.TrySetCanceled(exc.CancellationToken);
                    }

#if DEBUG
                    Debug.Assert(successfullySetExecutionSource);
                    executionSourceSetLocally = true;
#endif
                }
                catch (Exception exc)
                {
                    var successfullySetExecutionSource = _executionSource.TrySetException(exc);

#if DEBUG
                    Debug.Assert(successfullySetExecutionSource);
                    executionSourceSetLocally = true;
#endif
                }
            }
            finally
            {
                var executionSourceSet = _executionSource.TrySetResult(null);
#if DEBUG
                Debug.Assert(executionSourceSet || executionSourceSetLocally);
#endif
            }
        }

        public Task Execution => _executionSource.Task;
    }
}
