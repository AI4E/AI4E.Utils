using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying.Test
{
    public sealed class MultiplexStream : Stream
    {
        private readonly Stream _rx;
        private readonly Stream _tx;

        public MultiplexStream(Stream rx, Stream tx)
        {
            if (rx == null)
                throw new ArgumentNullException(nameof(rx));

            if (tx == null)
                throw new ArgumentNullException(nameof(tx));

            _rx = rx;
            _tx = tx;
        }

        public override bool CanRead => _rx.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => _rx.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
            _tx.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _rx.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _tx.Write(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _rx.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _tx.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}
