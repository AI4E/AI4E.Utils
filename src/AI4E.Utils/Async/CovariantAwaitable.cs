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
using System.Threading.Tasks;

namespace AI4E.Utils.Async
{
    public class CovariantAwaitable<TResult> : IAwaitable<TResult>
    {
        internal readonly Task<TResult> _task;
        private readonly bool _continueOnCapturedContext;

        public CovariantAwaitable(Task<TResult> task) : this(task, true) { }

        public CovariantAwaitable(Task<TResult> task, bool continueOnCapturedContext)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            _task = task;
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        public Task<TResult> AsTask()
        {
            return _task;
        }

        public IAwaiter<TResult> GetAwaiter()
        {
            return new CovariantAwaiter(this);
        }

        public IAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            if (continueOnCapturedContext == _continueOnCapturedContext)
                return this;

            return new CovariantAwaitable<TResult>(_task, continueOnCapturedContext);
        }

        public bool IsCompleted => _task.IsCompleted;

        public bool IsCompletedSuccessfully => _task.Status == TaskStatus.RanToCompletion;

        public bool IsFaulted => _task.IsFaulted;

        public bool IsCanceled => _task.IsCanceled;

        public TResult Result => _task.GetAwaiter().GetResult();

        ICovariantAwaiter<TResult> ICovariantAwaitable<TResult>.GetAwaiter()
        {
            return GetAwaiter();
        }

        ICovariantAwaitable<TResult> ICovariantAwaitable<TResult>.ConfigureAwait(bool continueOnCapturedContext)
        {
            return ConfigureAwait(continueOnCapturedContext);
        }

        private class CovariantAwaiter : IAwaiter<TResult>
        {
            private readonly CovariantAwaitable<TResult> _task;

            internal CovariantAwaiter(CovariantAwaitable<TResult> task)
            {
                Debug.Assert(task != null);

                _task = task;
            }

            public bool IsCompleted => _task.IsCompleted;

            public bool IsCompletedSuccessfully => _task.IsCompletedSuccessfully;

            public bool IsFaulted => _task.IsFaulted;

            public bool IsCanceled => _task.IsCanceled;

            public TResult GetResult()
            {
                return _task.AsTask().GetAwaiter().GetResult();
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                _task.AsTask().ConfigureAwait(_task._continueOnCapturedContext).GetAwaiter().UnsafeOnCompleted(continuation);
            }

            public void OnCompleted(Action continuation)
            {
                _task.AsTask().ConfigureAwait(_task._continueOnCapturedContext).GetAwaiter().OnCompleted(continuation);
            }
        }
    }

    public static class CovariantAwaitable
    {
        public static IAwaitable<TResult> FromTask<TResult>(Task<TResult> task)
        {
            return new CovariantAwaitable<TResult>(task);
        }

        public static IAwaitable<TResult> FromResult<TResult>(TResult result)
        {
            return new CovariantAwaitable<TResult>(Task.FromResult(result));
        }
    }
}
