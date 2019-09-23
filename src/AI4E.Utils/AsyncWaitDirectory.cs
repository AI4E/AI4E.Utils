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
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AI4E.Utils
{
    public sealed class AsyncWaitDirectory<TKey>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, (TaskCompletionSource<object?> tcs, int refCount)> _entries;

        public AsyncWaitDirectory()
        {
            _entries = new Dictionary<TKey, (TaskCompletionSource<object?> tcs, int refCount)>();
        }

        public async Task WaitForNotificationAsync(TKey key, CancellationToken cancellation)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            cancellation.ThrowIfCancellationRequested();

            var waitEntry = AllocateWaitEntry(key);

            using (cancellation.Register(() => FreeWaitEntry(key)))
            {
                await waitEntry.WithCancellation(cancellation).ConfigureAwait(false);
            }
        }

        public void Notify(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            TaskCompletionSource<object?> tcs;

            lock (_entries)
            {
                if (!_entries.TryGetValue(key, out var entry))
                {
                    return;
                }

                tcs = entry.tcs;
                _entries.Remove(key);
            }

            tcs.SetResult(null);
        }

        private Task AllocateWaitEntry(TKey key)
        {
            TaskCompletionSource<object?> tcs;

            lock (_entries)
            {
                var refCount = 0;

                if (_entries.TryGetValue(key, out var entry))
                {
                    tcs = entry.tcs;
                    refCount = entry.refCount;
                }
                else
                {
                    tcs = new TaskCompletionSource<object?>();
                }

                _entries[key] = (tcs, refCount + 1);
            }

            return tcs.Task;
        }

        private void FreeWaitEntry(TKey key)
        {
            lock (_entries)
            {
                if (!_entries.TryGetValue(key, out var entry))
                {
                    return;
                }

                Debug.Assert(entry.refCount >= 1);

                if (entry.refCount == 1)
                {
                    _entries.Remove(key);
                }
                else
                {
                    _entries[key] = (entry.tcs, entry.refCount - 1);
                }
            }
        }
    }
}
