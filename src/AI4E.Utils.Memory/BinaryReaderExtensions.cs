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

using System.Buffers;
using System.Diagnostics;
using System.Text;
using AI4E.Utils;

namespace System.IO
{
    public static class AI4EUtilsMemoryBinaryReaderExtensions
    {
        public static byte[] ReadBytes(this BinaryReader reader)
        {
#pragma warning disable CA1062
            var length = PrefixCodingHelper.Read7BitEncodedInt(reader);
#pragma warning restore CA1062

            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            var result = reader.ReadBytes(length);

            if (result.Length < length)
            {
                throw new EndOfStreamException();
            }

            return result;
        }

        public static SlicedMemoryOwner<byte> ReadBytes(this BinaryReader reader, MemoryPool<byte> memoryPool)
        {
            if (memoryPool is null)
                throw new ArgumentNullException(nameof(memoryPool));

#pragma warning disable CA1062
            var length = PrefixCodingHelper.Read7BitEncodedInt(reader);
#pragma warning restore CA1062

            if (length == 0)
            {
                return default;
            }

            var result = memoryPool.RentExact(length);

            try
            {
                var bytesRead = reader.Read(result.Memory.Span);

                if (bytesRead < length)
                {
                    throw new EndOfStreamException();
                }

                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        public static string ReadUtf8(this BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var bytesCount = PrefixCodingHelper.Read7BitEncodedInt(reader);

            if (bytesCount == 0)
                return string.Empty;

            using var bytesOwner = MemoryPool<byte>.Shared.RentExact(bytesCount);
            var bytesRead = reader.Read(bytesOwner.Memory.Span);

            if (bytesRead < bytesCount)
            {
                throw new EndOfStreamException();
            }

            return Encoding.UTF8.GetString(bytesOwner.Memory.Span);
        }

        public static SlicedMemoryOwner<char> ReadUtf8(this BinaryReader reader, MemoryPool<char> memoryPool)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var bytesCount = PrefixCodingHelper.Read7BitEncodedInt(reader);

            if (bytesCount == 0)
                return default;

            using var bytesOwner = MemoryPool<byte>.Shared.RentExact(bytesCount);
            var bytesRead = reader.Read(bytesOwner.Memory.Span);

            if (bytesRead < bytesCount)
            {
                throw new EndOfStreamException();
            }

            var charCount = Encoding.UTF8.GetCharCount(bytesOwner.Memory.Span);
            var result = memoryPool.RentExact(charCount);

            try
            {
                var charsRead = Encoding.UTF8.GetChars(bytesOwner.Memory.Span, result.Memory.Span);
                Debug.Assert(charsRead == charCount);
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }
    }
}
