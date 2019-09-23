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
using System.Threading.Tasks;

namespace System.Threading
{
    public static class AI4EUtilsSemaphoreSlimExtension
    {
        // True if the lock could be taken immediately, false otherwise.
        public static ValueTask<bool> LockOrWaitAsync(this SemaphoreSlim semaphore, CancellationToken cancellation)
        {
#pragma warning disable CA1062
            if (semaphore.Wait(0))
#pragma warning restore CA1062
            {
                Debug.Assert(semaphore.CurrentCount == 0);
                return new ValueTask<bool>(true);
            }

            return WaitAsync(semaphore, cancellation);
        }

        private static async ValueTask<bool> WaitAsync(SemaphoreSlim semaphore, CancellationToken cancellation)
        {
            await semaphore.WaitAsync(cancellation).ConfigureAwait(false);
            Debug.Assert(semaphore.CurrentCount == 0);

            return false;
        }
    }
}
