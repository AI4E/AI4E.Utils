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
using System.Threading.Tasks;
using AI4E.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI4E.Utils
{
    [TestClass]
    public class ValueTaskHelperTests
    {
        [TestMethod]
        public void EmptyTaskCollectionTest()
        {
            var task = Enumerable.Empty<ValueTask<int>>().WhenAll(preserveOrder: true);

            Assert.IsTrue(task.IsCompletedSuccessfully);
            var result = task.GetAwaiter().GetResult(); // We are allowed only once to get the result.
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void EmptyTaskCollection2Test()
        {
            var task = Enumerable.Empty<ValueTask<int>>().WhenAll( preserveOrder: false);

            Assert.IsTrue(task.IsCompletedSuccessfully);
            var result = task.GetAwaiter().GetResult(); // We are allowed only once to get the result.
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void EmptyTaskCollectionNonGenericTest()
        {
            var task = Enumerable.Empty<ValueTask>().WhenAll();

            Assert.IsTrue(task.IsCompletedSuccessfully);
            task.GetAwaiter().GetResult();
        }

        [TestMethod]
        public async Task WhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: true);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between

            taskSources[1].SetResult(1);
            await Task.Delay(10);
            taskSources[2].SetResult(2);
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            await Task.Delay(20); // Wait some time to allow the continuations to execute

            Assert.IsTrue(task.IsCompletedSuccessfully);
            var result = task.GetAwaiter().GetResult(); // We are allowed only once to get the result.
            Assert.IsNotNull(result);
            Assert.IsTrue(new[] { 0, 1, 2 }.SequenceEqual(result));
        }

        [TestMethod]
        public async Task PartiallyCompletedWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetResult(1);

            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: true);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult(2);
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            await Task.Delay(20); // Wait some time to allow the continuations to execute

            Assert.IsTrue(task.IsCompletedSuccessfully);
            var result = task.GetAwaiter().GetResult(); // We are allowed only once to get the result.
            Assert.IsNotNull(result);
            Assert.IsTrue(new[] { 0, 1, 2 }.SequenceEqual(result));
        }

        [TestMethod]
        public async Task PartiallyCompletedExceptionWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetResult(1);

            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: true);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetException(new CustomException());
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            await Task.Delay(20); // Wait some time to allow the continuations to execute

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);

            await Assert.ThrowsExceptionAsync<CustomException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedException2WhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetException(new CustomException());


            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: true);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult(2);
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            await Task.Delay(20); // Wait some time to allow the continuations to execute

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);

            await Assert.ThrowsExceptionAsync<CustomException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedCanceledWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetResult(1);

            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: true);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetCanceled();
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            await Task.Delay(20); // Wait some time to allow the continuations to execute

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedCanceled2WhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetCanceled();


            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: true);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult(2);
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            await Task.Delay(20); // Wait some time to allow the continuations to execute

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task WhenAllNonPreserveOrderTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: false);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between

            taskSources[1].SetResult(1);
            await Task.Delay(10);
            taskSources[2].SetResult(2);
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            Assert.IsTrue(task.IsCompletedSuccessfully);
            var result = task.GetAwaiter().GetResult(); // We are allowed only once to get the result.
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            Assert.IsTrue(new[] { 0, 1, 2 }.SequenceEqual(result.OrderBy(p => p)));
        }

        [TestMethod]
        public async Task PartiallyCompletedNonPreserveOrderWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetResult(1);

            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: false);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult(2);
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            Assert.IsTrue(task.IsCompletedSuccessfully);
            var result = task.GetAwaiter().GetResult(); // We are allowed only once to get the result.
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            Assert.IsTrue(new[] { 0, 1, 2 }.SequenceEqual(result.OrderBy(p => p)));
        }

        [TestMethod]
        public async Task PartiallyCompletedExceptionNonPreserveOrderWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetResult(1);

            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: false);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetException(new CustomException());
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);

            await Assert.ThrowsExceptionAsync<CustomException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedExceptionNonPreserveOrder2WhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetException(new CustomException());


            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: false);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult(2);
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);

            await Assert.ThrowsExceptionAsync<CustomException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedCanceledNonPreserveOrderWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetResult(1);

            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: false);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetCanceled();
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedCanceledNonPreserveOrder2WhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource<int>[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource<int>.Create();
            }

            taskSources[1].SetCanceled();


            var task = taskSources.Select(p => p.Task).WhenAll(preserveOrder: false);

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult(2);
            await Task.Delay(10);
            taskSources[0].SetResult(0);

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task WhenAllNonGenericTest()
        {
            var taskSources = new ValueTaskCompletionSource[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource.Create();
            }

            var task = taskSources.Select(p => p.Task).WhenAll();

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between

            taskSources[1].SetResult();
            await Task.Delay(10);
            taskSources[2].SetResult();
            await Task.Delay(10);
            taskSources[0].SetResult();

            Assert.IsTrue(task.IsCompletedSuccessfully);
            task.GetAwaiter().GetResult();
        }

        [TestMethod]
        public async Task PartiallyCompletedNonGenericWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource.Create();
            }

            taskSources[1].SetResult();

            var task = taskSources.Select(p => p.Task).WhenAll();

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult();
            await Task.Delay(10);
            taskSources[0].SetResult();

            Assert.IsTrue(task.IsCompletedSuccessfully);
            task.GetAwaiter().GetResult(); // We are allowed only once to get the result.
        }

        [TestMethod]
        public async Task PartiallyCompletedExceptionNonGenericWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource.Create();
            }

            taskSources[1].SetResult();

            var task = taskSources.Select(p => p.Task).WhenAll();

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetException(new CustomException());
            await Task.Delay(10);
            taskSources[0].SetResult();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);

            await Assert.ThrowsExceptionAsync<CustomException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedExceptionNonGeneric2WhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource.Create();
            }

            taskSources[1].SetException(new CustomException());


            var task = taskSources.Select(p => p.Task).WhenAll();

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult();
            await Task.Delay(10);
            taskSources[0].SetResult();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsFaulted);

            await Assert.ThrowsExceptionAsync<CustomException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedCanceledNonGenericWhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource.Create();
            }

            taskSources[1].SetResult();

            var task = taskSources.Select(p => p.Task).WhenAll();

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetCanceled();
            await Task.Delay(10);
            taskSources[0].SetResult();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public async Task PartiallyCompletedCanceledNonGeneric2WhenAllTest()
        {
            var taskSources = new ValueTaskCompletionSource[3];

            for (var i = 0; i < taskSources.Count(); i++)
            {
                taskSources[i] = ValueTaskCompletionSource.Create();
            }

            taskSources[1].SetCanceled();


            var task = taskSources.Select(p => p.Task).WhenAll();

            Assert.IsFalse(task.IsCompleted);

            // Complete the tasks in non-sort order and wait some time in between
            taskSources[2].SetResult();
            await Task.Delay(10);
            taskSources[0].SetResult();

            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(task.IsCanceled);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await task;
            });
        }

        private sealed class CustomException : Exception { }
    }
}
