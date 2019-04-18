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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#if !SUPPORTS_ASYNC_DISPOSABLE
using AI4E.Utils.Async;
#endif

namespace AI4E.Utils
{
    public static class ObjectExtension
    {
        public static void DisposeIfDisposable(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        // TODO: Return ValueTask
        public static Task DisposeIfDisposableAsync(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (obj is IAsyncDisposable asyncDisposable)
            {
#if SUPPORTS_ASYNC_DISPOSABLE
                return asyncDisposable.DisposeAsync().AsTask();
#else
                 return asyncDisposable.DisposeAsync();
#endif
            }

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return Task.CompletedTask;
        }

        public static IEnumerable<object> Yield(this object obj)
        {
            yield return obj;
        }

        public static IEnumerable<T> Yield<T>(this T t)
        {
            yield return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Assert<T>(this T t, Func<T, bool> assertion)
        {
            Debug.Assert(assertion(t));

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Assert<T>(this T t, Func<T, bool> precondition, Func<T, bool> assertion)
        {
            if (precondition(t))
            {
                Debug.Assert(assertion(t));
            }

            return t;
        }

        public static async Task<T> AssertAsync<T>(this Task<T> task, Func<T, bool> assertion)
        {
            return (await task).Assert(assertion);
        }

        public static async Task<T> AssertAsync<T>(this Task<T> task, Func<T, bool> precondition, Func<T, bool> assertion)
        {
            return (await task).Assert(precondition, assertion);
        }
    }
}
