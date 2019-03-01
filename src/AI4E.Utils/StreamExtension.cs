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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils
{
    public static class StreamExtension
    {
        public static async Task ReadExactAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellation)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            while (count > 0)
            {
                var readBytes = await stream.ReadAsync(buffer, offset, count, cancellation);

                if (readBytes == 0)
                    throw new EndOfStreamException();

                count -= readBytes;
                offset += readBytes;

                Debug.Assert(!(count < 0));
            }
        }

        public static void ReadExact(this Stream stream, byte[] buffer, int offset, int count)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            while (count > 0)
            {
                var readBytes = stream.Read(buffer, offset, count);

                if (readBytes == 0)
                    throw new EndOfStreamException();

                count -= readBytes;
                offset += readBytes;

                Debug.Assert(!(count < 0));
            }
        }

        public static async Task<byte[]> ToArrayAsync(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (stream == Stream.Null)
            {
                return Array.Empty<byte>();
            }

            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }

            using (memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);

                return memoryStream.ToArray();
            }
        }

        public static async ValueTask<MemoryStream> ReadToMemoryAsync(this Stream stream, CancellationToken cancellation)
        {
            if (stream is MemoryStream result)
            {
                return result;
            }

            if (stream.CanSeek)
            {
                if (stream.Length > int.MaxValue)
                    throw new InvalidOperationException("The streams size exceeds the readable limit.");

                result = new MemoryStream(checked((int)stream.Length));
            }
            else
            {
                result = new MemoryStream();
            }

            await stream.CopyToAsync(result, bufferSize: 1024, cancellation);
            result.Position = 0;
            return result;
        }
    }   
}
