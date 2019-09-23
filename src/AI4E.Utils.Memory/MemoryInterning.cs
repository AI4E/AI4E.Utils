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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using AI4E.Utils.Memory;
using static System.Diagnostics.Debug;

namespace AI4E.Utils.Memory
{
    public static class MemoryInterning
    {
        public static MemoryInterning<T> GetInstance<T>()
            where T : IEquatable<T>
        {
            return MemoryInterning<T>.Instance;
        }
    }

#pragma warning disable CA1001
    public sealed class MemoryInterning<T>
#pragma warning restore CA1001
        where T : IEquatable<T>
    {
        private static readonly IEqualityComparer<ReadOnlyMemory<T>> _memoryEqualityComparer = new MemoryEqualityComparer();

        internal static MemoryInterning<T> Instance { get; } = new MemoryInterning<T>();

        private readonly ConcurrentDictionary<ReadOnlyMemory<T>, ReadOnlyMemory<T>> _lookup;
        private readonly ThreadLocal<Dictionary<ReadOnlyMemory<T>, ReadOnlyMemory<T>>> _tlsLookup;

        private MemoryInterning()
        {
            _lookup = new ConcurrentDictionary<ReadOnlyMemory<T>, ReadOnlyMemory<T>>(_memoryEqualityComparer);
            _tlsLookup = new ThreadLocal<Dictionary<ReadOnlyMemory<T>, ReadOnlyMemory<T>>>(() => new Dictionary<ReadOnlyMemory<T>, ReadOnlyMemory<T>>(), trackAllValues: false);
        }

        public ReadOnlyMemory<T> Intern(ReadOnlyMemory<T> memory)
        {
            var tlsLookup = _tlsLookup.Value;
            Debug.Assert(tlsLookup != null);
            if (!tlsLookup!.TryGetValue(memory, out var internedValue))
            {
                internedValue = _lookup.GetOrAdd(memory, InternValue);
                tlsLookup.Add(memory, internedValue);
            }

            return internedValue;
        }

        private ReadOnlyMemory<T> InternValue(ReadOnlyMemory<T> memory)
        {
            if (typeof(T) == typeof(char))
            {
                var charMemory = Unsafe.As<ReadOnlyMemory<T>, ReadOnlyMemory<char>>(ref memory);

                if (MemoryMarshal.TryGetString(charMemory, out var str, out var start, out var length))
                {
                    if (start == 0 && length == str.Length)
                        return memory;

                    var result = charMemory.ToString().AsMemory();
                    return Unsafe.As<ReadOnlyMemory<char>, ReadOnlyMemory<T>>(ref result);
                }
                else
                {
                    var result = new string('\0', charMemory.Length).AsMemory();
                    charMemory.CopyTo(MemoryMarshal.AsMemory(result));
                    return Unsafe.As<ReadOnlyMemory<char>, ReadOnlyMemory<T>>(ref result);
                }
            }

            return memory.ToArray();
        }

        private sealed class MemoryEqualityComparer : IEqualityComparer<ReadOnlyMemory<T>>
        {
            public bool Equals(ReadOnlyMemory<T> x, ReadOnlyMemory<T> y)
            {
                return x.Span.SequenceEqual(y.Span);
            }

            public int GetHashCode(ReadOnlyMemory<T> obj)
            {
                return obj.Span.SequenceHashCode();
            }
        }
    }

    public static class MemoryInterningExtension
    {
        public static string InternAsString(this MemoryInterning<char> memoryInterning, ReadOnlyMemory<char> memory)
        {
            if (memoryInterning == null)
                throw new ArgumentNullException(nameof(memoryInterning));

            var internedValue = memoryInterning.Intern(memory);
            if (!MemoryMarshal.TryGetString(internedValue, out var result, out var start, out var length) || start != 0 || result.Length != length)
            {
                Assert(false); // The MemoryInterning type guarantees that this is a string, but we provide a fallback for production for any case.

                return internedValue.ToString();
            }

            return result;
        }
    }
}
