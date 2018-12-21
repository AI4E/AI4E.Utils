using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.Debug;

namespace AI4E.Utils
{
    public static class ValueTaskHelper
    {
        public static ValueTask<IEnumerable<T>> WhenAll<T>(this IEnumerable<ValueTask<T>> tasks, bool preserveOrder = true)
        {
            // We do not capture tasks to prevent allocation for the captured data.
            ValueTask<IEnumerable<T>> PreservedOrder(IEnumerable<ValueTask<T>> valueTasks)
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
            async ValueTask<IEnumerable<T>> NotPreservedOrder(IEnumerable<ValueTask<T>> valueTasks)
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
    }
}
