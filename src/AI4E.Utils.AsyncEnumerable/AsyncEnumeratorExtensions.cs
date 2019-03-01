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

/* Based on
 * --------------------------------------------------------------------------------------------------------------------
 * AsyncEnumerator (https://github.com/Andrew-Hanlon/AsyncEnumerator)
 * MIT License
 * 
 * Copyright (c) 2017 Andrew Hanlon
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.AsyncEnumerable
{
    public static class AsyncEnumeratorExtensions
    {
        public static async Task ForeachAsync<T>(this IAsyncEnumerable<T> enumerable,
                                                 Func<T, Task> action,
                                                 CancellationToken cancellation = default)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            IAsyncEnumerator<T> enumerator = null;

            try
            {
                enumerator = enumerable.GetEnumerator();

                while (await enumerator.MoveNext(cancellation))
                {
                    await action(enumerator.Current).WithCancellation(cancellation);
                }
            }
            finally
            {
                enumerator?.Dispose();
            }
        }

        public static async Task ForeachAsync<T>(this IAsyncEnumerable<T> enumerable,
                                                 Func<T, CancellationToken, Task> action,
                                                 CancellationToken cancellation = default)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            IAsyncEnumerator<T> enumerator = null;

            try
            {
                enumerator = enumerable.GetEnumerator();

                while (await enumerator.MoveNext(cancellation))
                {
                    await action(enumerator.Current, cancellation);
                }
            }
            finally
            {
                enumerator?.Dispose();
            }
        }

        public static async Task ForeachAsync<T>(this IAsyncEnumerable<T> enumerable,
                                                 Action<T> action,
                                                 CancellationToken cancellation = default)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            IAsyncEnumerator<T> enumerator = null;

            try
            {
                enumerator = enumerable.GetEnumerator();

                while (await enumerator.MoveNext(cancellation))
                {
                    action(enumerator.Current);
                }
            }
            finally
            {
                enumerator?.Dispose();
            }
        }
    }
}
