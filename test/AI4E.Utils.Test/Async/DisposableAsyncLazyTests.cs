using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx;

namespace AI4E.Utils.Async
{
    [TestClass]
    public class DisposableAsyncLazyTests
    {
        [TestMethod]
        public void IsNotAutostartedTest()
        {
            var lazy = new DisposableAsyncLazy<byte>(
                _ => { throw null; },
                _ => { throw null; },
                DisposableAsyncLazyOptions.None);

            Assert.IsFalse(lazy.IsStarted);
        }

        [TestMethod]
        public async Task FactoryTest()
        {
            var lazy = new DisposableAsyncLazy<byte>(
                cancellation => { return Task.FromResult<byte>(12); },
                _ => { throw null; },
                DisposableAsyncLazyOptions.None);

            Assert.AreEqual(12, await lazy);
        }

        //[TestMethod]
        public async Task ExecuteOnCallingThreadTest()
        {
            using (var context = new AsyncContext())
            {
                SynchronizationContext.SetSynchronizationContext(context.SynchronizationContext);

                var threadId = Thread.CurrentThread.ManagedThreadId;

                var lazy = new DisposableAsyncLazy<byte>(
                    cancellation =>
                    {
                        Assert.AreEqual(threadId, Thread.CurrentThread.ManagedThreadId);

                        return Task.FromResult<byte>(12);
                    },
                    _ => { throw null; },
                    DisposableAsyncLazyOptions.None);

                await lazy;
            }
        }

        [TestMethod]
        public async Task AutostartTest()
        {
            var lazy = new DisposableAsyncLazy<byte>(
                cancellation => { return Task.FromResult<byte>(12); },
                _ => { throw null; },
                DisposableAsyncLazyOptions.Autostart);

            Assert.IsTrue(lazy.IsStarted);
            Assert.AreEqual(12, await lazy);
        }

        [TestMethod]
        public async Task RetryOnFailureTest()
        {
            var @try = 1;

            var lazy = new DisposableAsyncLazy<byte>(
                cancellation =>
                {
                    if (@try == 1)
                    {
                        @try++;
                        throw new Exception();
                    }

                    return Task.FromResult<byte>(12);
                },
                _ => { throw null; },
                DisposableAsyncLazyOptions.RetryOnFailure);

            Assert.IsFalse(lazy.IsStarted);
            await Assert.ThrowsExceptionAsync<Exception>(async () => await lazy);
            Assert.AreEqual(12, await lazy);
        }

        [TestMethod]
        public async Task DisposeTest()
        {
            var disposeCalled = false;

            var lazy = new DisposableAsyncLazy<byte>(
                _ => { return Task.FromResult<byte>(12); },
                p => { Assert.AreEqual(12, p); disposeCalled = true; return Task.CompletedTask; },
                DisposableAsyncLazyOptions.None);

            await lazy;

            await lazy.DisposeAsync();
            Assert.IsTrue(disposeCalled);
        }

        [TestMethod]
        public async Task DisposeIfNotYetStartedTest()
        {
            var disposeCalled = false;

            var lazy = new DisposableAsyncLazy<byte>(
                _ => { return Task.FromResult<byte>(12); },
                p => { Assert.AreEqual(12, p); disposeCalled = true; return Task.CompletedTask; },
                DisposableAsyncLazyOptions.None);

            await lazy.DisposeAsync();
            Assert.IsFalse(disposeCalled);
        }

        [TestMethod]
        public async Task DisposeIfDisposedTest()
        {
            var disposeCalled = false;

            var lazy = new DisposableAsyncLazy<byte>(
                _ => { return Task.FromResult<byte>(12); },
                p => { Assert.AreEqual(12, p); disposeCalled = true; return Task.CompletedTask; },
                DisposableAsyncLazyOptions.None);

            await lazy;

            await lazy.DisposeAsync();
            disposeCalled = false;
            await lazy.DisposeAsync();
            Assert.IsFalse(disposeCalled);
        }

        [TestMethod]
        public async Task DisposalThrowsTest()
        {
            var lazy = new DisposableAsyncLazy<byte>(
                _ => { return Task.FromResult<byte>(12); },
                p => { throw new Exception(); },
                DisposableAsyncLazyOptions.None);

            await lazy;

            await Assert.ThrowsExceptionAsync<Exception>(async () =>
            {
                await lazy.DisposeAsync();
            });
        }
    }
}
