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

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public static class AI4EUtilsStreamExtension
    {
        public static async Task ReadExactAsync(
            this Stream stream,
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellation)
        {
            while (count > 0)
            {
#pragma warning disable CA1062
                var readBytes = await stream.ReadAsync(buffer, offset, count, cancellation)
#pragma warning restore CA1062
                    .ConfigureAwait(false);

                if (readBytes == 0)
                    throw new EndOfStreamException();

                count -= readBytes;
                offset += readBytes;

                Debug.Assert(!(count < 0));
            }
        }

        public static void ReadExact(this Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
#pragma warning disable CA1062
                var readBytes = stream.Read(buffer, offset, count);
#pragma warning restore CA1062

                if (readBytes == 0)
                    throw new EndOfStreamException();

                count -= readBytes;
                offset += readBytes;

                Debug.Assert(!(count < 0));
            }
        }

        public static async Task<byte[]> ToArrayAsync(this Stream stream)
        {
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
#pragma warning disable CA1062
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
#pragma warning restore CA1062
                  

                return memoryStream.ToArray();
            }
        }

        public static async ValueTask<MemoryStream> ReadToMemoryAsync(
            this Stream stream,
            CancellationToken cancellation)
        {
            if (stream is MemoryStream result)
            {
                return result;
            }

#pragma warning disable CA1062
            if (stream.CanSeek)
#pragma warning restore CA1062
            {
                if (stream.Length > int.MaxValue)
                    throw new InvalidOperationException("The streams size exceeds the readable limit.");

                result = new MemoryStream(checked((int)stream.Length));
            }
            else
            {
                result = new MemoryStream();
            }

            await stream.CopyToAsync(result, bufferSize: 1024, cancellation).ConfigureAwait(false);
            result.Position = 0;
            return result;
        }
    }
}
