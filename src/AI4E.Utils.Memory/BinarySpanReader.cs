using System;
using System.Buffers.Binary;
using System.Text;
using AI4E.Utils.Memory.Compatibility;

namespace AI4E.Utils.Memory
{
    public ref struct BinarySpanReader
    {
        private int _offset;
        private readonly bool _useLittleEndian;

        public BinarySpanReader(ReadOnlySpan<byte> span, ByteOrder byteOrder = ByteOrder.BigEndian)
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
                _useLittleEndian = (byteOrder == ByteOrder.LittleEndian);
            }
        }

        public ReadOnlySpan<byte> Span { get; }
        public ByteOrder ByteOrder { get; }

        public ReadOnlySpan<byte> ReadSpan => Span.Slice(start: 0, length: _offset);
        public int Length => ReadSpan.Length;

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

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public byte ReadByte()
        {
            return Span.Slice(_offset++)[0];
        }

        public sbyte ReadSByte()
        {
            return unchecked((sbyte)ReadByte());
        }

        public ReadOnlySpan<byte> Read()
        {
            var count = ReadInt32();
            return Read(count);
        }

        public ReadOnlySpan<byte> Read(int count)
        {
            EnsureSpace(count);

            var result = Span.Slice(_offset, count);
            _offset += count;

            return result;
        }

        public ushort ReadUInt16()
        {
            ushort result;

            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadUInt16LittleEndian(Span.Slice(_offset));
            }
            else
            {
                result = BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(_offset));
            }

            _offset += 2;

            return result;
        }

        public short ReadInt16()
        {
            short result;

            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadInt16LittleEndian(Span.Slice(_offset));
            }
            else
            {
                result = BinaryPrimitives.ReadInt16BigEndian(Span.Slice(_offset));
            }

            _offset += 2;
            return result;
        }

        public uint ReadUInt32()
        {
            uint result;

            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadUInt32LittleEndian(Span.Slice(_offset));
            }
            else
            {
                result = BinaryPrimitives.ReadUInt32BigEndian(Span.Slice(_offset));
            }

            _offset += 4;
            return result;
        }

        public int ReadInt32()
        {
            int result;
            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadInt32LittleEndian(Span.Slice(_offset));
            }
            else
            {
                result = BinaryPrimitives.ReadInt32BigEndian(Span.Slice(_offset));
            }

            _offset += 4;
            return result;
        }

        public ulong ReadUInt64()
        {
            ulong result;
            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadUInt64LittleEndian(Span.Slice(_offset));
            }
            else
            {
                result = BinaryPrimitives.ReadUInt64BigEndian(Span.Slice(_offset));
            }

            _offset += 8;
            return result;
        }

        public long ReadInt64()
        {
            long result;

            if (_useLittleEndian)
            {
                result = BinaryPrimitives.ReadInt64LittleEndian(Span.Slice(_offset));
            }
            else
            {
                result = BinaryPrimitives.ReadInt64BigEndian(Span.Slice(_offset));
            }

            _offset += 8;
            return result;
        }

        public float ReadSingle()
        {
            var int32 = ReadInt32();
            return BitConverter.ToSingle(BitConverter.GetBytes(int32), 0); // TODO: *(float*)(&value)
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
    }
}
