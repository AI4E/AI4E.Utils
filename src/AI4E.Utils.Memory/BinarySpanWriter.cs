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

 #pragma warning disable CA1815

using System;
using System.Buffers.Binary;
using System.Text;

namespace AI4E.Utils.Memory
{
    public ref struct BinarySpanWriter
    {
        private int _offset;
        private readonly bool _useLittleEndian;

        public BinarySpanWriter(Span<byte> span, ByteOrder byteOrder = ByteOrder.BigEndian)
        {
            if (byteOrder < ByteOrder.Native || byteOrder > ByteOrder.LittleEndian)
            {
                throw new ArgumentException("Invalid enum value.", nameof(byteOrder));
            }

            Span = span;
            ByteOrder = byteOrder;
            _offset = 0;

            if (byteOrder == ByteOrder.Native)
            {
                _useLittleEndian = BitConverter.IsLittleEndian;
            }
            else
            {
                _useLittleEndian = byteOrder == ByteOrder.LittleEndian;
            }
        }

        public Span<byte> Span { get; }
        public ByteOrder ByteOrder { get; }

        public Span<byte> WrittenSpan => Span.Slice(start: 0, length: _offset);
        public int Length => WrittenSpan.Length;

        public bool CanAdvance(int count)
        {
            if (count < 0)
                throw new ArgumentNullException(nameof(count));

            if (count == 0)
                return true;

            return Span.Length - _offset >= count;
        }

        public bool TryAdvance(int count)
        {
            if (count == 0)
                return true;

            if (CanAdvance(count))
            {
                _offset += count;
                return true;
            }

            return false;
        }

        public void WriteBool(bool value)
        {
            WriteByte(unchecked((byte)(value ? 1 : 0)));
        }

        public void WriteByte(byte value)
        {
            Span.Slice(_offset++)[0] = value;
        }

        public void WriteSByte(sbyte value)
        {
            WriteByte(unchecked((byte)value));
        }

        public void Write(byte[] buffer, bool lengthPrefix = false)
        {
            Write(buffer.AsSpan(), lengthPrefix);
        }

        public void Write(byte[] buffer, int index, int count, bool lengthPrefix = false)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (buffer.Length - index < count)
            {
                throw new ArgumentException(); // TODO
            }

            Write(buffer.AsSpan(index, count), lengthPrefix);
        }

        public void Write(ReadOnlySpan<byte> span, bool lengthPrefix = false)
        {
            if (span.IsEmpty)
                return;

            EnsureSpace(span.Length + (lengthPrefix ? 4 : 0));

            if (lengthPrefix)
            {
                WriteInt32(span.Length);
            }

            span.CopyTo(Span.Slice(_offset));

            _offset += span.Length;
        }

        public void WriteUInt16(ushort value)
        {
            if (_useLittleEndian)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(Span.Slice(_offset), value);
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(_offset), value);
            }

            _offset += 2;
        }

        public void WriteInt16(short value)
        {
            if (_useLittleEndian)
            {
                BinaryPrimitives.WriteInt16LittleEndian(Span.Slice(_offset), value);
            }
            else
            {
                BinaryPrimitives.WriteInt16BigEndian(Span.Slice(_offset), value);
            }

            _offset += 2;
        }

        public void WriteUInt32(uint value)
        {
            if (_useLittleEndian)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(Span.Slice(_offset), value);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(Span.Slice(_offset), value);
            }

            _offset += 4;
        }

        public void WriteInt32(int value)
        {
            if (_useLittleEndian)
            {
                BinaryPrimitives.WriteInt32LittleEndian(Span.Slice(_offset), value);
            }
            else
            {
                BinaryPrimitives.WriteInt32BigEndian(Span.Slice(_offset), value);
            }

            _offset += 4;
        }

        public void WriteUInt64(ulong value)
        {
            if (_useLittleEndian)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(Span.Slice(_offset), value);
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(Span.Slice(_offset), value);
            }

            _offset += 8;
        }

        public void WriteInt64(long value)
        {
            if (_useLittleEndian)
            {
                BinaryPrimitives.WriteInt64LittleEndian(Span.Slice(_offset), value);
            }
            else
            {
                BinaryPrimitives.WriteInt64BigEndian(Span.Slice(_offset), value);
            }

            _offset += 8;
        }

        public void WriteSingle(float value)
        {
            var int32 = BitConverter.ToInt32(BitConverter.GetBytes(value), 0); // TODO: *(int*)(&value)

            WriteInt32(int32);
        }

        public void WriteDouble(double value)
        {
            WriteInt64(BitConverter.DoubleToInt64Bits(value));
        }

        public void Write(ReadOnlySpan<char> chars, bool lengthPrefix = true)
        {
            var bytesWritten = Encoding.UTF8.GetBytes(chars, Span.Slice(_offset + (lengthPrefix ? 4 : 0)));

            if (lengthPrefix)
            {
                WriteInt32(bytesWritten);
            }

            _offset += bytesWritten;
        }

        private void EnsureSpace(int count)
        {
            if (!CanAdvance(count))
            {
                throw new Exception("Not enough space left"); // TODO
            }
        }
    }
}

#pragma warning restore CA1815
