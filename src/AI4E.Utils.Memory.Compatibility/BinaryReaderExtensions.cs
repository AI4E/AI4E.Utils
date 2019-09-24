using System.Diagnostics;
using System.Linq.Expressions;

namespace System.IO
{
    public static class AI4EUtilsMemoryCompatibilityBinaryReaderExtensions
    {
        private static readonly ReadBytesShim? _readBytesShim = BuildReadBytesShim(typeof(BinaryReader));
        private static readonly ReadCharsShim? _readCharsShim = BuildReadCharsShim(typeof(BinaryReader));

        private static ReadBytesShim? BuildReadBytesShim(Type binaryReaderType)
        {
            var readMethod = binaryReaderType.GetMethod(nameof(Read), new[] { typeof(Span<byte>) });

            if (readMethod == null)
                return null;

            Debug.Assert(readMethod.ReturnType == typeof(int));

            var binaryReaderParameter = Expression.Parameter(binaryReaderType, "reader");
            var bufferParameter = Expression.Parameter(typeof(Span<byte>), "buffer");
            var methodCall = Expression.Call(binaryReaderParameter, readMethod, bufferParameter);
            return Expression.Lambda<ReadBytesShim>(methodCall, binaryReaderParameter, bufferParameter).Compile();
        }

        private static ReadCharsShim? BuildReadCharsShim(Type binaryReaderType)
        {
            var readMethod = binaryReaderType.GetMethod(nameof(Read), new[] { typeof(Span<char>) });

            if (readMethod == null)
                return null;

            Debug.Assert(readMethod.ReturnType == typeof(int));

            var binaryReaderParameter = Expression.Parameter(binaryReaderType, "reader");
            var charsParameter = Expression.Parameter(typeof(Span<char>), "chars");
            var methodCall = Expression.Call(binaryReaderParameter, readMethod, charsParameter);
            return Expression.Lambda<ReadCharsShim>(methodCall, binaryReaderParameter, charsParameter).Compile();
        }

        private delegate int ReadBytesShim(BinaryReader reader, Span<byte> buffer);
        private delegate int ReadCharsShim(BinaryReader reader, Span<char> chars);

        public static int Read(this BinaryReader binaryReader, Span<byte> buffer)
        {
            if (_readBytesShim != null)
            {
                return _readBytesShim(binaryReader, buffer);
            }

#pragma warning disable CA1062
            var underlyingStream = binaryReader.BaseStream;
#pragma warning restore CA1062
            Debug.Assert(underlyingStream != null);
            return underlyingStream!.Read(buffer);
        }

        public static int Read(this BinaryReader binaryReader, Span<char> buffer)
        {
            if (_readCharsShim != null)
            {
                return _readCharsShim(binaryReader, buffer);
            }

            // TODO: TryGetEncoding

#pragma warning disable CA1062
            var chars = binaryReader.ReadChars(buffer.Length);
#pragma warning restore CA1062
            chars.CopyTo(buffer.Slice(0, chars.Length));
            return chars.Length;
        }
    }
}
