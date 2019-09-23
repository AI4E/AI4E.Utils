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
using System.Diagnostics;

namespace AI4E.Utils.Memory
{
    public static class ArrayPoolExtension
    {
        public static ArrayPoolReleaser<T> RentExact<T>(this ArrayPool<T> arrayPool, int length, out Memory<T> memory)
        {
            if (arrayPool == null)
                throw new ArgumentNullException(nameof(arrayPool));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (length == 0)
            {
                memory = Memory<T>.Empty;
                return default;
            }

            var array = arrayPool.Rent(length);
            try
            {
                memory = array.AsMemory().Slice(start: 0, length);
                return new ArrayPoolReleaser<T>(arrayPool, array);
            }
            catch
            {
                arrayPool.Return(array);
                throw;
            }
        }

        public static ArrayPoolReleaser<T> Rent<T>(this ArrayPool<T> arrayPool, int minimumLength, out T[] array)
        {
            if (arrayPool == null)
                throw new ArgumentNullException(nameof(arrayPool));

            if (minimumLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumLength));

            if (minimumLength == 0)
            {
                array = Array.Empty<T>();
                return default;
            }

            array = arrayPool.Rent(minimumLength);
            try
            {
                return new ArrayPoolReleaser<T>(arrayPool, array);
            }
            catch
            {
                arrayPool.Return(array);
                throw;
            }
        }
    }

    public readonly struct ArrayPoolReleaser<T> : IDisposable, IEquatable<ArrayPoolReleaser<T>>
    {
        private readonly ArrayPool<T>? _arrayPool;
        private readonly T[]? _array;

        internal ArrayPoolReleaser(ArrayPool<T> arrayPool, T[] array)
        {
            Debug.Assert(arrayPool != null);
            Debug.Assert(array != null);

            _arrayPool = arrayPool;
            _array = array;
        }

        public void Dispose()
        {
            _arrayPool?.Return(_array!);
        }

        public bool Equals(ArrayPoolReleaser<T> other)
        {
            return (_arrayPool, _array) == (other._arrayPool, other._array);
        }

        public override bool Equals(object? obj)
        {
            return obj is ArrayPoolReleaser<T> arrayPoolReleaser
                 && Equals(arrayPoolReleaser);
        }

        public override int GetHashCode()
        {
            return (_arrayPool, _array).GetHashCode();
        }

        public static bool operator ==(ArrayPoolReleaser<T> left, ArrayPoolReleaser<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArrayPoolReleaser<T> left, ArrayPoolReleaser<T> right)
        {
            return !left.Equals(right);
        }
    }
}
