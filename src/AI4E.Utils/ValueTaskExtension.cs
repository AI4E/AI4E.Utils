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
using AI4E.Utils.Async;

namespace AI4E.Utils
{
    public static class ValueTaskExtension
    {
        public static ValueTask WithCancellation(this ValueTask task, CancellationToken cancellation)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (!cancellation.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            if (cancellation.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellation).AsValueTask();
            }

            return InternalWithCancellation(task, cancellation);
        }

        private static async ValueTask InternalWithCancellation(ValueTask task, CancellationToken cancellation)
        {
            var tcs = ValueTaskCompletionSource.Create();

            Execute(tcs, task);

            using (cancellation.Register(() => tcs.TrySetCanceled(cancellation), useSynchronizationContext: false))
            {
                await tcs.Task;
            }
        }

        private static async void Execute(ValueTaskCompletionSource tcs, ValueTask task)
        {
            try
            {
                await task;
                tcs.TrySetResult();
            }
            catch (Exception exc)
            {
                tcs.TrySetExceptionOrCanceled(exc);
            }
        }

        public static ValueTask<T> WithCancellation<T>(this ValueTask<T> task, CancellationToken cancellation)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (!cancellation.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            if (cancellation.IsCancellationRequested)
            {
                return Task.FromCanceled<T>(cancellation).AsValueTask();
            }

            return InternalWithCancellation(task, cancellation);
        }

        private static async ValueTask<T> InternalWithCancellation<T>(ValueTask<T> task, CancellationToken cancellation)
        {
            var tcs = ValueTaskCompletionSource<T>.Create();

            Execute(tcs, task);

            using (cancellation.Register(() => tcs.TrySetCanceled(cancellation), useSynchronizationContext: false))
            {
                return await tcs.Task;
            }
        }

        private static async void Execute<T>(ValueTaskCompletionSource<T> tcs, ValueTask<T> task)
        {
            try
            {
                tcs.TrySetResult(await task);
            }
            catch (Exception exc)
            {
                tcs.TrySetExceptionOrCanceled(exc);
            }
        }
    }
}
