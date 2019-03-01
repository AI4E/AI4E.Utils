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
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AI4E.Utils.Async;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace AI4E.Utils
{
    public sealed class DisposeAwareStream : Stream, IAsyncDisposable
    {
        private readonly NetworkStream _underlyingStream;
        private readonly Func<Task> _disposeOperation;
        private readonly ILogger<DisposeAwareStream> _logger;
        private readonly AsyncDisposeHelper _disposeHelper;
        private readonly AsyncLock _writeLock = new AsyncLock();

        public DisposeAwareStream(NetworkStream underlyingStream,
                                  Func<Task> disposeOperation,
                                  ILogger<DisposeAwareStream> logger = null)
        {
            if (underlyingStream == null)
                throw new ArgumentNullException(nameof(underlyingStream));

            if (disposeOperation == null)
                throw new ArgumentNullException(nameof(disposeOperation));

            _underlyingStream = underlyingStream;
            _disposeOperation = disposeOperation;
            _logger = logger;

            _disposeHelper = new AsyncDisposeHelper(DisposeInternalAsync);
        }

        #region Looped through ops

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _underlyingStream.FlushAsync(cancellationToken);
        }

        public override void Flush()
        {
            _underlyingStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _underlyingStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _underlyingStream.SetLength(value);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _underlyingStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override bool CanRead => _underlyingStream.CanRead;

        public override bool CanSeek => _underlyingStream.CanSeek;

        public override bool CanWrite => _underlyingStream.CanWrite;

        public override long Length => _underlyingStream.Length;

        public override long Position
        {
            get => _underlyingStream.Position;
            set => _underlyingStream.Position = value;
        }

        #endregion

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _underlyingStream.ReadAsync(buffer, offset, count, cancellationToken);

                if (result == 0)
                {
                    await _disposeHelper.DisposeAsync().HandleExceptionsAsync(_logger);
                }

                return result;
            }
            catch (Exception exc) when (exc is SocketException || exc is IOException || exc is ObjectDisposedException)
            {
                await _disposeHelper.DisposeAsync().HandleExceptionsAsync(_logger);
                throw;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                var result = _underlyingStream.Read(buffer, offset, count);

                if (result == 0)
                {
                    ExceptionHelper.HandleExceptions(() => _disposeHelper.Dispose(), _logger);
                }

                return result;
            }
            catch (Exception exc) when (exc is SocketException || exc is IOException || exc is ObjectDisposedException)
            {
                ExceptionHelper.HandleExceptions(() => _disposeHelper.Dispose(), _logger);
                throw;
            }
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                await _underlyingStream.WriteAsync(buffer, offset, count, cancellationToken);
            }
            catch (Exception exc) when (exc is SocketException || exc is IOException || exc is ObjectDisposedException)
            {
                await _disposeHelper.DisposeAsync().HandleExceptionsAsync(_logger);
                throw;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                _underlyingStream.Write(buffer, offset, count);
            }
            catch (Exception exc) when (exc is SocketException || exc is IOException || exc is ObjectDisposedException)
            {
                ExceptionHelper.HandleExceptions(() => _disposeHelper.Dispose(), _logger);

                throw;
            }
        }

        #region Disposal

        public Task Disposal => _disposeHelper.Disposal;

        public Task DisposeAsync()
        {
            return _disposeHelper.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposeHelper.Dispose();
            }
        }

        private async Task DisposeInternalAsync()
        {
            ExceptionHelper.HandleExceptions(() => _underlyingStream.Close(), _logger);
            await _disposeOperation().HandleExceptionsAsync(_logger);
        }

        #endregion
    }
}
