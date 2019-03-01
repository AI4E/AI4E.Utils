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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AI4E.Utils.Memory
{
    public static partial class MemoryExtensions
    {
        public static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> s)
        {
            if (s.IsEmpty)
                return s;

            var span = s.Span;
            var start = 0;

            for (; start < s.Length; start++)
            {
                if (!char.IsWhiteSpace(span[start]))
                {
                    break;
                }
            }

            if (start == s.Length)
            {
                return ReadOnlyMemory<char>.Empty;
            }

            var count = 1;

            for (; count + start < s.Length; count++)
            {
                if (char.IsWhiteSpace(span[count]))
                {
                    break;
                }
            }

            return s.Slice(start, count);
        }

        [Obsolete("Use MemoryExtensions.SequenceEqual")]
        public static bool SequenceEqual<T>(this ReadOnlyMemory<T> left, ReadOnlyMemory<T> right, IEqualityComparer<T> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));

            var leftSpan = left.Span;
            var rightSpan = right.Span;

            if (leftSpan.IsEmpty)
            {
                return rightSpan.IsEmpty;
            }

            if (rightSpan.IsEmpty)
                return false;

            if (leftSpan.Length != rightSpan.Length)
                return false;

            for (var i = 0; i < leftSpan.Length; i++)
            {
                if (!comparer.Equals(leftSpan[i], rightSpan[i]))
                    return false;
            }

            return true;
        }

        public static bool IsEmptyOrWhiteSpace(this ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
                return true;

            for (var j = 0; j < span.Length; j++)
            {
                if (!char.IsWhiteSpace(span[j]))
                {
                    return false;
                }
            }

            return true;
        }

        public static ReadOnlyMemory<T> Slice<T>(this ReadOnlyMemory<T> memory, int start, int exclusiveEnd)
        {
            return memory.Slice(start, exclusiveEnd - start);
        }

        public static string ConvertToString(this ReadOnlyMemory<char> memory)
        {
            if (memory.IsEmpty)
            {
                return string.Empty;
            }

            if (MemoryMarshal.TryGetString(memory, out var text, out var start, out var length))
            {
                // If the memory is only a part of the string we had to substring anyway.
                if (start == 0 && length == text.Length)
                {
                    return text;
                }
            }

            var result = new string('\0', memory.Length);
            var resultAsMemory = MemoryMarshal.AsMemory(result.AsMemory());
            memory.CopyTo(resultAsMemory);
            return result;
        }

        public static string InternAsString(this ReadOnlyMemory<char> memory)
        {
            return MemoryInterning<char>.Instance.InternAsString(memory);
        }
    }
}
