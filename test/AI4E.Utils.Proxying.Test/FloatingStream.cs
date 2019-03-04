/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * Copyright (c) 2018-2019 - 2019-2019 Andreas Truetschel and contributors.
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace AI4E.Utils.Proxying.Test
{
    public sealed class FloatingStream : Stream
    {
        private readonly AsyncProducerConsumerQueue<ArraySegment<byte>> _queue = new AsyncProducerConsumerQueue<ArraySegment<byte>>();
        private readonly CancellationTokenSource _disposedCancellationSource = new CancellationTokenSource();
        private ArraySegment<byte> _current = new ArraySegment<byte>();

        public FloatingStream() { }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count == 0)
                return;

            if (buffer.Length - offset < count)
                throw new ArgumentException(); // TODO

            if (_disposedCancellationSource.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().FullName);

            _queue.Enqueue(new ArraySegment<byte>(buffer, offset, count));
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (buffer.Length - offset < count)
                throw new ArgumentException(); // TODO

            if (_current.Array == null || _current.Count == 0)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposedCancellationSource.Token);

                try
                {
                    _current = await _queue.DequeueAsync(cts.Token);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }

            Debug.Assert(_current.Array != null);
            Debug.Assert(_current.Count > 0);

            var bytesToCopy = Math.Min(count, _current.Count);

            Array.Copy(_current.Array, _current.Offset, buffer, offset, bytesToCopy);

            if (_current.Count == bytesToCopy)
            {
                _current = default;
            }
            else
            {
                _current = new ArraySegment<byte>(_current.Array, _current.Offset + bytesToCopy, _current.Count - bytesToCopy);
            }

            return bytesToCopy;
        }

        protected override void Dispose(bool disposing)
        {
            _disposedCancellationSource.Cancel();
        }
    }
}
