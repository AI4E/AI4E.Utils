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
    public readonly struct TaskCancellationTokenSource : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public TaskCancellationTokenSource(Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            Task = task;
            _cancellationTokenSource = new CancellationTokenSource();
            task.ContinueWith((_, cts) => ((CancellationTokenSource)cts).Cancel(), _cancellationTokenSource);
        }

        public TaskCancellationTokenSource(Task task, params CancellationToken[] linkedTokens)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (linkedTokens == null)
                throw new ArgumentNullException(nameof(linkedTokens));

            Task = task;

            if (!linkedTokens.Any())
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }
            else
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedTokens);
            }

            task.ContinueWith((_, cts) => ((CancellationTokenSource)cts).Cancel(), _cancellationTokenSource);
        }

        public Task Task { get; }
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? new CancellationToken(canceled: true);

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
