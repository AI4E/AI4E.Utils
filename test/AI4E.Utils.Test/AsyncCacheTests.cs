using System;
using System.Threading;
using System.Threading.Tasks;
using AI4E.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI4E.Utils
{
    [TestClass]
    public class AsyncCacheTests
    {
        [TestMethod]
        public async Task GetInitialValueTest()
        {
            var revision = 0;

            async Task<int> update()
            {
                var r = Interlocked.Increment(ref revision);
                await Task.Delay(10);
                return r;
            }

            var asyncCache = new AsyncCache<int>(update);

            var result = await asyncCache.Task;

            Assert.AreEqual(revision, result);
        }

        [TestMethod]
        public async Task UpdateValueTest()
        {
            var revision = 0;

            async Task<int> update()
            {
                var r = Interlocked.Increment(ref revision);
                await Task.Delay(10);
                return r;
            }

            var asyncCache = new AsyncCache<int>(update);
            await asyncCache.Task;
            var result = await asyncCache.UpdateAsync();

            Assert.AreEqual(revision, result);
        }

        [TestMethod]
        public async Task UpdateHandedOutTaskTest()
        {
            var tcs = new TaskCompletionSource<object>();
            var revision = 0;

            async Task<int> update()
            {
                var r = Interlocked.Increment(ref revision);
                await tcs.Task;
                return r;
            }

            var asyncCache = new AsyncCache<int>(update);
            var task = asyncCache.Task;

            asyncCache.Update();
            tcs.SetResult(null);

            var result = await task;

            Assert.AreEqual(revision, result);
        }

        [TestMethod]
        public void DisposeTest()
        {
            var revision = 0;
            CancellationToken cancellation;

            async Task<int> update(CancellationToken c)
            {
                cancellation = c;
                var r = Interlocked.Increment(ref revision);
                await Task.Delay(50, c);
                return r;
            }

            var asyncCache = new AsyncCache<int>(update);
            var task = asyncCache.Task;
            asyncCache.Dispose();

            Assert.IsTrue(cancellation.IsCancellationRequested);
        }

        [TestMethod]
        public void UpdateAfterDisposeThrowsTest()
        {
            Task<int> update(CancellationToken c)
            {
                return Task.FromResult(0);
            }

            var asyncCache = new AsyncCache<int>(update);
            asyncCache.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                asyncCache.Update();
            });
        }

        [TestMethod]
        public async Task AwaitingTaskAfterDisposeThrowsTest()
        {
            var revision = 0;
            CancellationToken cancellation;

            async Task<int> update(CancellationToken c)
            {
                cancellation = c;
                var r = Interlocked.Increment(ref revision);
                await Task.Delay(50);
                return r;
            }

            var asyncCache = new AsyncCache<int>(update);
            var task = asyncCache.Task;

            asyncCache.Dispose();

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
            {
                await task;
            });
        }

        [TestMethod]
        public void AccessingTaskAfterDisposeThrowsTest()
        {
            Task<int> update(CancellationToken c)
            {
                return Task.FromResult(0);
            }

            var asyncCache = new AsyncCache<int>(update);

            asyncCache.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(() =>
            {
                _ = asyncCache.Task;
            });
        }

        [TestMethod]
        public async Task OldUpdateOperationCanceledOnUpdateTest()
        {
            var revision = 0;
            CancellationToken cancellation;

            async Task<int> update(CancellationToken c)
            {
                var r = Interlocked.Increment(ref revision);

                if (r == 1)
                {
                    cancellation = c;
                }

                await Task.Delay(50, c);
                return r;
            }

            var asyncCache = new AsyncCache<int>(update);

            asyncCache.Update();
            asyncCache.Update();

            Assert.IsTrue(cancellation.IsCancellationRequested);
            Assert.AreEqual(revision, await asyncCache.Task);
        }

        [TestMethod]
        public async Task UpdateOperationThrowsTest()
        {
            async Task<int> update(CancellationToken c)
            {
                await Task.Delay(5);
                throw new InvalidTimeZoneException();
            }

            var asyncCache = new AsyncCache<int>(update);

            await Assert.ThrowsExceptionAsync<InvalidTimeZoneException>(async () => await asyncCache.Task);
        }
    }
}
