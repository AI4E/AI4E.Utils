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
using System.Buffers;
using static System.Diagnostics.Debug;

namespace AI4E.Utils.Memory
{
    public static partial class MemoryExtensions
    {
        public static ArrayPoolExtension.ArrayPoolReleaser<byte> Base64Decode(this string str, out Memory<byte> bytes)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            return Base64Decode(str.AsSpan(), out bytes);
        }

        public static ArrayPoolExtension.ArrayPoolReleaser<byte> Base64Decode(in this ReadOnlySpan<char> chars, out Memory<byte> bytes)
        {
            var minBytesLength = Base64Coder.ComputeBase64DecodedLength(chars);
            var releaser = ArrayPool<byte>.Shared.RentExact(minBytesLength, out bytes);

            try
            {
                var success = Base64Coder.TryFromBase64Chars(chars, bytes.Span, out var bytesWritten);
                Assert(success);

                bytes = bytes.Slice(start: 0, length: bytesWritten);

                return releaser;
            }
            catch
            {
                releaser.Dispose();
                throw;
            }
        }
    }
}
