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
using System.Runtime.CompilerServices;
using System.Text;

namespace AI4E.Utils.Memory
{
    public ref struct BinarySpanReader
    {
        private readonly bool _useLittleEndian;

        public BinarySpanReader(ReadOnlySpan<byte> span, ByteOrder byteOrder = ByteOrder.BigEndian)
        {
            if (byteOrder < ByteOrder.Native || byteOrder > ByteOrder.LittleEndian)
            {
                throw new ArgumentException("Invalid enum value.", nameof(byteOrder));
            }

            Span = span;
            ByteOrder = byteOrder;
            Length = 0;

            if (byteOrder == ByteOrder.Native)
            {
                _useLittleEndian = BitConverter.IsLittleEndian;
            }
            else
            {
                _useLittleEndian = byteOrder == ByteOrder.LittleEndian;
            }
        }

        public ReadOnlySpan<byte> Span { get; }
        public ByteOrder ByteOrder { get; }

        public ReadOnlySpan<byte> ReadSpan => Span.Slice(start: 0, length: Length);
        public int Length { get; private set; }

        public bool CanAdvance(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return true;

            return Span.Length - Length >= count;
        }

        public bool TryAdvance(int count)
        {
            if (count == 0)
                return true;

            if (CanAdvance(count))
            {
                Length += count;
                return true;
            }

            return false;
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public byte ReadByte()
        {
            return Span.Slice(Length++)[0];
        }

        public sbyte ReadSByte()
        {
            return unchecked((sbyte)ReadByte());
        }

        public ReadOnlySpan<byte> Read()
        {
            var count = Read7BitEncodedInt();
            return Read(count);
        }

        public ReadOnlySpan<byte> Read(int count)
        {
            EnsureSpace(count);

            var result = Span.Slice(Length, count);
            Length += count;

            return result;
        }

        public ushort ReadUInt16()
        {
            ushort result;

            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadUInt16LittleEndian(Span.Slice(Length));
            }
            else
            {
                result = BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(Length));
            }

            Length += 2;

            return result;
        }

        public short ReadInt16()
        {
            short result;

            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadInt16LittleEndian(Span.Slice(Length));
            }
            else
            {
                result = BinaryPrimitives.ReadInt16BigEndian(Span.Slice(Length));
            }

            Length += 2;
            return result;
        }

        public uint ReadUInt32()
        {
            uint result;

            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadUInt32LittleEndian(Span.Slice(Length));
            }
            else
            {
                result = BinaryPrimitives.ReadUInt32BigEndian(Span.Slice(Length));
            }

            Length += 4;
            return result;
        }

        public int ReadInt32()
        {
            int result;
            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadInt32LittleEndian(Span.Slice(Length));
            }
            else
            {
                result = BinaryPrimitives.ReadInt32BigEndian(Span.Slice(Length));
            }

            Length += 4;
            return result;
        }

        public ulong ReadUInt64()
        {
            ulong result;
            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadUInt64LittleEndian(Span.Slice(Length));
            }
            else
            {
                result = BinaryPrimitives.ReadUInt64BigEndian(Span.Slice(Length));
            }

            Length += 8;
            return result;
        }

        public long ReadInt64()
        {
            long result;

            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadInt64LittleEndian(Span.Slice(Length));
            }
            else
            {
                result = BinaryPrimitives.ReadInt64BigEndian(Span.Slice(Length));
            }

            Length += 8;
            return result;
        }

        public float ReadSingle()
        {
            var int32 = ReadInt32();
            return Unsafe.As<int, float>(ref int32); // *(float*)(&value)
        }

        public double ReadDouble()
        {
            var int64 = ReadInt64();
            return BitConverter.Int64BitsToDouble(int64);
        }

        public string ReadString()
        {
            var bytes = Read();
            return Encoding.UTF8.GetString(bytes);
        }

        private void EnsureSpace(int count)
        {
            if (!CanAdvance(count))
            {
                throw new Exception("Not enough space left"); // TODO
            }
        }

        private int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            var count = 0;
            var shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                {
                    throw new FormatException("Bad7BitInt32"); // TODO
                }

                // ReadByte handles end of stream cases for us.
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }
    }
}

#pragma warning restore CA1815
