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

using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using AI4E.Utils.Memory.Compatibility;

namespace System
{
    public static class AI4EUtilsMemoryCompatibilityGuidExtensions
    {
        private static readonly TryFormatShim? _tryFormatShim = BuildTryFormatShim(typeof(Guid));
        private static readonly TryWriteBytesShim? _tryWriteBytesShim = BuildTryWriteBytesShim(typeof(Guid));

        private static TryFormatShim? BuildTryFormatShim(Type guidType)
        {
            var tryFormatMethod = guidType.GetMethod("TryFormat",
                                                     BindingFlags.Public | BindingFlags.Instance,
                                                     Type.DefaultBinder,
                                                     new Type[] { typeof(Span<char>), typeof(int).MakeByRefType(), typeof(ReadOnlySpan<char>) },
                                                     modifiers: null);

            if (tryFormatMethod == null)
            {
                return null;
            }

            Debug.Assert(tryFormatMethod.ReturnType == typeof(bool));

            var guidParameter = Expression.Parameter(typeof(Guid), "guid");
            var destinationParameter = Expression.Parameter(typeof(Span<char>), "destination");
            var charsWrittenParameter = Expression.Parameter(typeof(int).MakeByRefType(), "charsWritten");
            var formatParameter = Expression.Parameter(typeof(ReadOnlySpan<char>), "format");

            var call = Expression.Call(guidParameter, tryFormatMethod, destinationParameter, charsWrittenParameter, formatParameter);
            var lambda = Expression.Lambda<TryFormatShim>(call, guidParameter, destinationParameter, charsWrittenParameter, formatParameter);
            return lambda.Compile();
        }

        private static TryWriteBytesShim? BuildTryWriteBytesShim(Type guidType)
        {
            var tryWriteBytesMethod = guidType.GetMethod("TryWriteBytes",
                                                         BindingFlags.Public | BindingFlags.Instance,
                                                         Type.DefaultBinder,
                                                         new Type[] { typeof(Span<byte>) },
                                                         modifiers: null);

            if (tryWriteBytesMethod == null)
            {
                return null;
            }

            Debug.Assert(tryWriteBytesMethod.ReturnType == typeof(bool));

            var guidParameter = Expression.Parameter(typeof(Guid), "guid");
            var destinationParameter = Expression.Parameter(typeof(Span<byte>), "destination");

            var call = Expression.Call(guidParameter, tryWriteBytesMethod, destinationParameter);
            var lambda = Expression.Lambda<TryWriteBytesShim>(call, guidParameter, destinationParameter);
            return lambda.Compile();
        }

        private delegate bool TryFormatShim(Guid guid, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format);
        private delegate bool TryWriteBytesShim(Guid guid, Span<byte> destination);

#pragma warning disable CA1720
        public static bool TryFormat(in this Guid guid, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
#pragma warning restore CA1720
        {
            if (_tryFormatShim != null)
            {
                return _tryFormatShim(guid, destination, out charsWritten, format);
            }

            var stringFormat = (format.IsEmpty || format.IsWhiteSpace()) ? null : StringHelper.Create(format);
            var result = guid.ToString(stringFormat, CultureInfo.InvariantCulture);

            if (result.Length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            result.AsSpan().CopyTo(destination.Slice(start: 0, result.Length));

            charsWritten = result.Length;
            return true;
        }

#pragma warning disable CA1720
        public static bool TryWriteBytes(in this Guid guid, Span<byte> destination)
#pragma warning restore CA1720
        {
            if (_tryWriteBytesShim != null)
            {
                return _tryWriteBytesShim(guid, destination);
            }

            const int _guidLength = 16;

            if (destination.Length < _guidLength)
                return false;

            unsafe
            {
                fixed (Guid* guidPtr = &guid)
                {
                    var source = new Span<byte>(guidPtr, _guidLength);
                    source.CopyTo(destination.Slice(start: 0, length: _guidLength));
                }
            }

            return true;
        }

    }
}
