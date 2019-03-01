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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AI4E.Utils
{
    public static class TaskExtension
    {
        public static bool IsRunning(this Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            return !(task.IsCanceled || task.IsCompleted || task.IsFaulted);
        }

        #region IgnoreCancellation

        public static void IgnoreCancellation(this Task task, ILogger logger)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    var exception = t.Exception.InnerException;

                    if (!(exception is OperationCanceledException))
                    {
                        if (logger != null)
                        {
                            logger.LogError(exception, "An exception occured in the task.");
                        }
                        else
                        {
                            Debug.WriteLine("An exception occured in the task.");
                            Debug.WriteLine(exception.ToString());
                        }
                    }
                }
            });
        }

        public static Task IgnoreCancellationAsync(this Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var tcs = new TaskCompletionSource<object>();

            task.ContinueWith(t =>
            {
                if (t.Exception != null &&
                    t.Exception.InnerExceptions.Any(e => !(e is OperationCanceledException)))
                {
                    tcs.SetException(t.Exception);
                }
                else
                {
                    tcs.SetResult(null);
                }
            });

            return tcs.Task;
        }

        public static void IgnoreCancellation(this Task task)
        {
            IgnoreCancellation(task, logger: null);
        }

        #endregion

        public static void HandleExceptions(this ValueTask valueTask, ILogger logger = null)
        {
            if (valueTask.IsCompletedSuccessfully)
            {
                return;
            }

            if (valueTask.IsCompleted)
            {
                try
                {
                    valueTask.GetAwaiter().GetResult();
                }
                catch (Exception exc)
                {
                    ExceptionHelper.LogException(exc, logger);
                }

                return;
            }

            HandleExceptions(valueTask.AsTask(), logger);
        }

        public static void HandleExceptions<T>(this ValueTask<T> valueTask, ILogger logger = null)
        {
            if (valueTask.IsCompletedSuccessfully)
            {
                return;
            }

            if (valueTask.IsCompleted)
            {
                try
                {
                    valueTask.GetAwaiter().GetResult();
                }
                catch (Exception exc)
                {
                    ExceptionHelper.LogException(exc, logger);
                }

                return;
            }

            HandleExceptions(valueTask.AsTask(), logger);
        }

        public static void HandleExceptions(this Task task, ILogger logger = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    if (logger != null)
                    {
                        logger.LogError(t.Exception.InnerException, "An exception occured unexpectedly.");
                    }
                    else
                    {
                        Debug.WriteLine("An exception occured unexpectedly.");
                        Debug.WriteLine(t.Exception.InnerException.ToString());
                    }
                }
            });
        }

        public static Task HandleExceptionsAsync(this Task task, ILogger logger = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            return ExceptionHelper.HandleExceptions(async () => await task, logger, Task.CompletedTask);
        }

        public static Task<T> HandleExceptionsAsync<T>(this Task<T> task, T defaultValue = default, ILogger logger = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            return ExceptionHelper.HandleExceptions(async () => await task, logger, Task.FromResult(defaultValue));
        }

        public static Task WithCancellation(this Task task, CancellationToken cancellation)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (!cancellation.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            if (cancellation.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellation);
            }

            return InternalWithCancellation(task, cancellation);
        }

        private static async Task InternalWithCancellation(Task task, CancellationToken cancellation)
        {
            var tcs = new TaskCompletionSource<object>();
            var cancellationTask = tcs.Task;

            using (cancellation.Register(() => tcs.TrySetCanceled(cancellation), useSynchronizationContext: false))
            {
                var completed = await Task.WhenAny(tcs.Task, task).ConfigureAwait(false);

                if (completed == cancellationTask)
                {
                    Debug.Assert(cancellation.IsCancellationRequested);

                    task.HandleExceptions();
                }

                await completed;
            }
        }

        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellation)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (!cancellation.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            if (cancellation.IsCancellationRequested)
            {
                return Task.FromCanceled<T>(cancellation);
            }

            return InternalWithCancellation(task, cancellation);
        }

        private static async Task<T> InternalWithCancellation<T>(Task<T> task, CancellationToken cancellation)
        {
            var tcs = new TaskCompletionSource<T>();
            var cancellationTask = tcs.Task;

            using (cancellation.Register(() => tcs.TrySetCanceled(cancellation), useSynchronizationContext: false))
            {
                var completed = await Task.WhenAny(tcs.Task, task).ConfigureAwait(false);

                if (completed == cancellationTask)
                {
                    Debug.Assert(cancellation.IsCancellationRequested);

                    task.HandleExceptions();
                }

                return await completed;
            }
        }

        public static object GetResult(this Task t)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t));

            t.GetAwaiter().GetResult();

            if (t.IsFaulted ||
                t.IsCanceled ||
               !t.GetType().IsGenericType ||
                t.GetType().GetGenericArguments()[0] == Type.GetType("System.Threading.Tasks.VoidTaskResult"))
            {
                return null;
            }

            return t.GetType().GetProperty("Result").GetValue(t);
        }

        public static Task<T> WithResult<T>(this Task t, T result)
        {
            if (t == null)
                throw new ArgumentNullException(nameof(t));

            return t.ContinueWith(_ => result);
        }

        public static ValueTask AsValueTask(this Task task)
        {
            return new ValueTask(task);
        }

        public static ValueTask<T> AsValueTask<T>(this Task<T> task)
        {
            return new ValueTask<T>(task);
        }
    }

    public sealed class ExceptionHelper
    {
        public static void HandleExceptions(Action action, ILogger logger = null)
        {
            try
            {
                action();
            }
            catch (Exception exc)
            {
                if (logger != null)
                {
                    logger.LogError(exc, "An exception occured unexpectedly.");
                }
                else
                {
                    Debug.WriteLine("An exception occured unexpectedly.");
                    Debug.WriteLine(exc.ToString());
                }
            }
        }

        public static T HandleExceptions<T>(Func<T> func, ILogger logger = null, T defaultValue = default)
        {
            try
            {
                return func();
            }
            catch (Exception exc)
            {
                LogException(exc, logger);
            }

            return defaultValue;
        }

        public static void LogException(Exception exc, ILogger logger = null)
        {
            if (logger != null)
            {
                logger.LogError(exc, "An exception occured unexpectedly");
            }
            else
            {
                Debug.WriteLine("An exception occured unexpectedly.");
                Debug.WriteLine(exc.ToString());
            }
        }
    }
}
