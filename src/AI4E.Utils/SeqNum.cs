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

#pragma warning disable CA2225

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace AI4E.Utils
{
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public readonly struct SeqNum : IEquatable<SeqNum>, IComparable<SeqNum>
    {
        public SeqNum(int rawValue)
        {
            RawValue = rawValue;
        }

#pragma warning disable CA1822
        public int RawValue { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
#pragma warning restore CA1822

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(SeqNum other)
        {
            return unchecked(-(other.RawValue - RawValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(SeqNum other)
        {
            return other.RawValue == RawValue;
        }

        public override bool Equals(object? obj)
        {
            return obj is SeqNum seqNum && Equals(seqNum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return RawValue.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
#pragma warning disable CA1305
            return RawValue.ToString();
#pragma warning restore CA1305
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(IFormatProvider formatProvider)
        {
            return RawValue.ToString(formatProvider);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(SeqNum left, SeqNum right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(SeqNum left, SeqNum right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(SeqNum left, SeqNum right)
        {
            return left.CompareTo(right) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(SeqNum left, SeqNum right)
        {
            return left.CompareTo(right) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(SeqNum left, SeqNum right)
        {
            return left.CompareTo(right) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(SeqNum left, SeqNum right)
        {
            return left.CompareTo(right) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator int(SeqNum seqNum)
        {
            return seqNum.RawValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator SeqNum(int rawValue)
        {
            return new SeqNum(rawValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum operator +(SeqNum seqNum, int value)

        {
            return new SeqNum(unchecked(seqNum.RawValue + value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum operator +(int value, SeqNum seqNum)
        {
            return new SeqNum(unchecked(seqNum.RawValue + value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum operator -(SeqNum seqNum, int value)
        {
            return new SeqNum(unchecked(seqNum.RawValue - value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum operator -(int value, SeqNum seqNum)
        {
            return new SeqNum(unchecked(seqNum.RawValue - value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int operator -(SeqNum left, SeqNum right)
        {
            return unchecked(left.RawValue - right.RawValue);
        }
    }

    public static class SeqNumInterlocked
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum Add(ref SeqNum location, int value)
        {
            return (SeqNum)Interlocked.Add(ref Reinterpret(ref location), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum Increment(ref SeqNum location)
        {
            return (SeqNum)Interlocked.Increment(ref Reinterpret(ref location));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum Decrement(ref SeqNum location)
        {
            return (SeqNum)Interlocked.Decrement(ref Reinterpret(ref location));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum Exchange(ref SeqNum location, SeqNum value)
        {
            return (SeqNum)Interlocked.Exchange(ref Reinterpret(ref location), value.RawValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SeqNum CompareExchange(ref SeqNum location, SeqNum value, SeqNum comparand)
        {
            return (SeqNum)Interlocked.CompareExchange(ref Reinterpret(ref location), value.RawValue, comparand.RawValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref int Reinterpret(ref SeqNum location)
        {
            return ref Unsafe.As<SeqNum, int>(ref location);
        }
    }
}

#pragma warning restore CA2225
