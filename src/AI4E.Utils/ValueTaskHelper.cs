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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace AI4E.Utils
{
    /// <summary>
    /// Provides helper methods for the <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> types.
    /// </summary>
    public static class ValueTaskHelper
    {
        /// <summary>
        /// Creates a value-task that completes when all of the value-tasks in the source enumerable completed.
        /// </summary>
        /// <typeparam name="T">The type of the completed value-task.</typeparam>
        /// <param name="tasks">The enumerable of value-tasks to wait on for completion.</param>
        /// <param name="preserveOrder">A boolean value indicating whether the order of the tasks shall be preserved.</param>
        /// <returns>A value-task that represents the completion of all of the supplied value-tasks.</returns>
        public static ValueTask<IEnumerable<T>> WhenAll<T>(this IEnumerable<ValueTask<T>> tasks, bool preserveOrder = true)
        {
            // We do not capture tasks to prevent allocation for the captured data.
            static ValueTask<IEnumerable<T>> PreservedOrder(IEnumerable<ValueTask<T>> valueTasks)
            {
                List<T> result;
                List<Task> tasksToAwait;

                var wasCanceled = false;
                List<Exception> exceptions = null;

                if (valueTasks is ICollection<ValueTask<T>> collection)
                {
                    result = new List<T>(capacity: collection.Count);
                    tasksToAwait = new List<Task>(capacity: collection.Count);
                }
                else if (valueTasks is IReadOnlyCollection<ValueTask<T>> readOnlyCollection)
                {
                    result = new List<T>(capacity: readOnlyCollection.Count);
                    tasksToAwait = new List<Task>(capacity: readOnlyCollection.Count);
                }
                else
                {
                    result = new List<T>();
                    tasksToAwait = new List<Task>();
                }

                var i = 0;

                foreach (var valueTask in valueTasks)
                {
                    if (valueTask.IsCompletedSuccessfully)
                    {
                        lock (result)
                        {
                            Assert(result.Count == i);
                            result.Add(valueTask.Result);
                        }
                    }
                    else
                    {
                        lock (result)
                        {
                            result.Add(default);
                        }

                        var task = valueTask.AsTask();
                        var index = i; // This is copied to a new variable in order to capture the current value and not a future one.

                        var taskToAwait = task.ContinueWith(t =>
                        {
                            Assert(t.IsCompleted);

                            if (t.IsFaulted)
                            {
                                var exceptionList = Volatile.Read(ref exceptions);

                                if (exceptionList == null)
                                {
                                    exceptionList = new List<Exception>();
                                    exceptionList = Interlocked.CompareExchange(ref exceptions, exceptionList, null) ?? exceptionList;
                                }

                                exceptionList.Add(t.Exception.InnerException); // TODO: Unwrap the exception
                            }
                            else if (t.IsCanceled)
                            {
                                Volatile.Write(ref wasCanceled, true);
                            }
                            else
                            {
                                lock (result)
                                {
                                    result[index] = t.Result;
                                }
                            }
                        });

                        tasksToAwait.Add(taskToAwait);
                    }

                    i++;
                }

                if (tasksToAwait.Count == 0)
                {
                    return new ValueTask<IEnumerable<T>>(result);
                }

                var taskCompletionSource = new TaskCompletionSource<IEnumerable<T>>();

                Task.WhenAll(tasksToAwait).ContinueWith(t =>
                {
                    if (exceptions != null)
                    {
                        taskCompletionSource.TrySetException(exceptions);
                    }
                    else if (wasCanceled)
                    {
                        taskCompletionSource.TrySetCanceled();
                    }
                    else
                    {
                        taskCompletionSource.TrySetResult(result);
                    }
                });

                return new ValueTask<IEnumerable<T>>(taskCompletionSource.Task);
            }

            // We do not capture tasks to prevent allocation for the captured data.
            static async ValueTask<IEnumerable<T>> NotPreservedOrder(IEnumerable<ValueTask<T>> valueTasks)
            {
                List<T> result;
                List<Task<T>> tasksToAwait;

                if (valueTasks is ICollection<ValueTask<T>> collection)
                {
                    result = new List<T>(capacity: collection.Count);
                    tasksToAwait = new List<Task<T>>(capacity: collection.Count);
                }
                else if (valueTasks is IReadOnlyCollection<ValueTask<T>> readOnlyCollection)
                {
                    result = new List<T>(capacity: readOnlyCollection.Count);
                    tasksToAwait = new List<Task<T>>(capacity: readOnlyCollection.Count);
                }
                else
                {
                    result = new List<T>();
                    tasksToAwait = new List<Task<T>>();
                }

                foreach (var valueTask in valueTasks)
                {
                    if (valueTask.IsCompletedSuccessfully)
                    {
                        result.Add(valueTask.Result);
                    }
                    else
                    {
                        tasksToAwait.Add(valueTask.AsTask());
                    }
                }

                result.AddRange(await Task.WhenAll(tasksToAwait).ConfigureAwait(false));

                return result;
            }

            if (preserveOrder)
            {
                return PreservedOrder(tasks);
            }

            return NotPreservedOrder(tasks);
        }

        /// <summary>
        /// Creates a value-task that completes when all of the value-tasks in the source enumerable completed.
        /// </summary>
        /// <param name="tasks">The enumerable of value-tasks to wait on for completion.</param>
        /// <returns>A value-task that represents the completion of all of the supplied value-tasks.</returns>
        public static ValueTask WhenAll(this IEnumerable<ValueTask> tasks)
        {
            List<Task> tasksToAwait = null;

            if (tasks is ICollection<ValueTask> collection)
            {
                tasksToAwait = new List<Task>(capacity: collection.Count);
            }
            else if (tasks is IReadOnlyCollection<ValueTask> readOnlyCollection)
            {
                tasksToAwait = new List<Task>(capacity: readOnlyCollection.Count);
            }

            foreach (var valueTask in tasks)
            {
                if (!valueTask.IsCompletedSuccessfully)
                {
                    if (tasksToAwait == null)
                    {
                        tasksToAwait = new List<Task>();
                    }

                    tasksToAwait.Add(valueTask.AsTask());
                }
            }

            if (tasksToAwait == null || !tasksToAwait.Any())
            {
                return default;
            }

            return new ValueTask(Task.WhenAll(tasksToAwait));
        }
    }
}
