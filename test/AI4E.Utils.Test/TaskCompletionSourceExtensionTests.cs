using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI4E.Utils
{
    [TestClass]
    public class TaskCompletionSourceExtensionTests
    {
        [TestMethod]
        public void TaskCanceledExceptionTest()
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;

            tcs.SetExceptionOrCanceled(new TaskCanceledException());

            Assert.IsTrue(task.IsCanceled);
            Assert.IsFalse(task.IsFaulted);
        }

        [TestMethod]
        public async Task OperationCanceledExceptionWithCancellationTokenTest()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var cancellationToken = cancellationTokenSource.Token;
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;

            tcs.SetExceptionOrCanceled(new OperationCanceledException(cancellationToken));

            Assert.IsTrue(task.IsCanceled);
            Assert.IsFalse(task.IsFaulted);

            try
            {
                await task;
            }
            catch (OperationCanceledException exc)
            {
                Assert.AreEqual(cancellationToken, exc.CancellationToken);
            }
        }

        [TestMethod]
        public void OperationCanceledExceptionWithoutCancellationTokenTest()
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;

            tcs.SetExceptionOrCanceled(new OperationCanceledException());

            Assert.IsTrue(task.IsCanceled);
            Assert.IsFalse(task.IsFaulted);
        }

        [TestMethod]
        public void OrdinaryExceptionTest()
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;

            tcs.SetExceptionOrCanceled(new Exception());

            Assert.IsTrue(task.IsFaulted);
            Assert.IsFalse(task.IsCanceled);
        }

        [TestMethod]
        public void CompletedTcsSetExceptionOrCanceledTest()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);

            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                tcs.SetExceptionOrCanceled(new Exception());
            });
        }

        [TestMethod]
        public void CompletedTcsTrySetExceptionOrCanceledTest()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            var success = tcs.TrySetExceptionOrCanceled(new Exception());

            Assert.IsFalse(success);
        }
    }
}
