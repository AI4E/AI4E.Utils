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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils
{
    public readonly struct TaskCancellationTokenSource : IDisposable, IEquatable<TaskCancellationTokenSource>
    {
        private readonly CancellationTokenSource? _cancellationTokenSource;
        private readonly Task? _task;

        public TaskCancellationTokenSource(Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            _task = task;

            if (task.IsCompleted)
            {
                _cancellationTokenSource = null;
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();

            task.ContinueWith(
                (_, cts) => ((CancellationTokenSource)cts!).Cancel(),
                _cancellationTokenSource,
                TaskScheduler.Default);
        }

        public TaskCancellationTokenSource(Task task, params CancellationToken[] linkedTokens)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (linkedTokens == null)
                throw new ArgumentNullException(nameof(linkedTokens));

            _task = task;

            if (task.IsCompleted || linkedTokens.Any(p => p.IsCancellationRequested))
            {
                _cancellationTokenSource = null;
                return;
            }

            if (!linkedTokens.Any())
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }
            else
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedTokens);
            }

            static void CancelSource(Task _, object obj)
            {
                var cts = (CancellationTokenSource)obj;

                if (cts.IsCancellationRequested)
                    return;

                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException) { }
            }

            task.ContinueWith(
                CancelSource!,
                _cancellationTokenSource,
                TaskScheduler.Default);
        }

        public Task Task => _task ?? Task.CompletedTask;
        public CancellationToken CancellationToken
        {
            get
            {
                if (_cancellationTokenSource == null)
                {
                    return CreateCanceledToken();
                }

                // Prevent throwing an ObjectDisposedException
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return CreateCanceledToken();
                }

                try
                {
                    return _cancellationTokenSource.Token;
                }
                catch (ObjectDisposedException)
                {
                    return CreateCanceledToken();
                }
            }
        }

        private static CancellationToken CreateCanceledToken()
        {
            return new CancellationToken(canceled: true);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        public bool Equals(TaskCancellationTokenSource other)
        {
            return other._task == _task;
        }

        public override bool Equals(object? obj)
        {
            return obj is TaskCancellationTokenSource taskCancellationTokenSource
                && Equals(taskCancellationTokenSource);
        }

        public override int GetHashCode()
        {
            return _task?.GetHashCode() ?? 0;
        }

        public static bool operator ==(TaskCancellationTokenSource left, TaskCancellationTokenSource right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TaskCancellationTokenSource left, TaskCancellationTokenSource right)
        {
            return !left.Equals(right);
        }


    }
}
