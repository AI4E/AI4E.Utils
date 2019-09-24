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

using System.Text;
using System.Buffers;
using AI4E.Utils;
using System.Diagnostics;

namespace System.IO
{
    public static class AI4EUtilsMemoryBinaryWriterExtensions
    {
        public static void WriteBytes(this BinaryWriter writer, ReadOnlySpan<byte> bytes)
        {
#pragma warning disable CA1062
            PrefixCodingHelper.Write7BitEncodedInt(writer, bytes.Length);
#pragma warning restore CA1062 
            writer.Write(bytes);
        }

        public static void WriteUtf8(this BinaryWriter writer, ReadOnlySpan<char> chars)
        {
            var bytesCount = Encoding.UTF8.GetByteCount(chars);

#pragma warning disable CA1062
            PrefixCodingHelper.Write7BitEncodedInt(writer, bytesCount);
#pragma warning restore CA1062

            if (bytesCount > 0)
            {
                using var bytesOwner = MemoryPool<byte>.Shared.RentExact(bytesCount);

                var bytesRead = Encoding.UTF8.GetBytes(chars, bytesOwner.Memory.Span);
                Debug.Assert(bytesRead == bytesCount);
                writer.Write(bytesOwner.Memory.Span);
            }
        }
    }
}
