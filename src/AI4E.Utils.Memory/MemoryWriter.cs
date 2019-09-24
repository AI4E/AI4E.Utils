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
using static System.Diagnostics.Debug;

namespace AI4E.Utils.Memory
{
    public struct MemoryWriter<T>
    {
        private readonly Memory<T> _memory;
        private int _position;

        public MemoryWriter(Memory<T> memory)
        {
            _memory = memory;
            _position = 0;
        }

        public void Append(ReadOnlySpan<T> span)
        {
            var spanX = _memory.Span;

            Assert(_position + span.Length <= _memory.Length);
            span.CopyTo(spanX.Slice(_position));
            _position += span.Length;
        }

        public void Append(T c)
        {
            var span = _memory.Span;

            Assert(_position + 1 <= _memory.Length);
            span[_position] = c;
            _position += 1;
        }

        public Memory<T> GetMemory()
        {
            return _memory.Slice(0, _position);
        }
    }
}

#pragma warning restore CA1815
