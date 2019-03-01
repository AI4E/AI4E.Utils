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
using System.Text;

namespace AI4E.Utils.Memory
{
    public static class BinaryWriterExtension
    {
        public static void WriteWithLengthPrefix(this BinaryWriter writer, byte[] bytes)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            writer.Write(bytes.Length);

            if (bytes.Length > 0)
            {
                writer.Write(bytes);
            }
        }

        public static void WriteUtf8(this BinaryWriter writer, ReadOnlySpan<char> str)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var bytes = Encoding.UTF8.GetBytes(str.ToArray()); // TODO: This will copy everything into an array
            var length = bytes.Length;

            writer.Write(length);

            if (length > 0)
            {
                writer.Write(bytes);
            }
        }
    }
}
