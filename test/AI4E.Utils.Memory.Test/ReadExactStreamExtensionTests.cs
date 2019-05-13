using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI4E.Utils.Memory.Test
{
    [TestClass]
    public sealed class ReadExactStreamExtensionTests
    {
        [TestMethod]
        public void DataAvailableAsyncTest()
        {
            var inBuffer = Enumerable.Range(0, 10).Select(p => (byte)p).ToArray();
            var outBuffer = new byte[10];
            var stream = new MemoryStream();

            stream.Write(inBuffer, 0, inBuffer.Length);
            stream.Position = 0;

            var task = stream.ReadExactAsync(outBuffer, cancellation: default).AsTask();

            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            Assert.IsTrue(inBuffer.SequenceEqual(outBuffer));
        }

        [TestMethod]
        public async Task DataNotAvailableAsyncTest()
        {
            var outBuffer = new byte[10];
            var stream = new MemoryStream();

            await Assert.ThrowsExceptionAsync<EndOfStreamException>(async () =>
            {
                await stream.ReadExactAsync(outBuffer, cancellation: default);
            });
        }

        [TestMethod]
        public async Task ThrowsOnNullStreamAsyncTest()
        {
            var outBuffer = new byte[10];
            Stream stream = null;

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await stream.ReadExactAsync(outBuffer, cancellation: default);
            });
        }

        [TestMethod]
        public void DataAvailableTest()
        {
            var inBuffer = Enumerable.Range(0, 10).Select(p => (byte)p).ToArray();
            var outBuffer = new byte[10];
            var stream = new MemoryStream();

            stream.Write(inBuffer, 0, inBuffer.Length);
            stream.Position = 0;

            stream.ReadExact(outBuffer.AsSpan());

            Assert.IsTrue(inBuffer.SequenceEqual(outBuffer));
        }

        [TestMethod]
        public void DataNotAvailableTest()
        {
            var outBuffer = new byte[10];
            var stream = new MemoryStream();

            Assert.ThrowsException<EndOfStreamException>(() =>
            {
                stream.ReadExact(outBuffer);
            });
        }

        [TestMethod]
        public void ThrowsOnNullStreamTest()
        {
            var outBuffer = new byte[10];
            Stream stream = null;

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                stream.ReadExact(outBuffer);
            });
        }
    }
}
