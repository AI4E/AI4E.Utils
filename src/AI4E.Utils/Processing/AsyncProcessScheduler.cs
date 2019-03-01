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
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace AI4E.Utils.Processing
{
    public sealed class AsyncProcessScheduler
    {
        private volatile ImmutableHashSet<ITrigger> _triggers = ImmutableHashSet<ITrigger>.Empty;
        private readonly AsyncManualResetEvent _event = new AsyncManualResetEvent();

        private CancellationTokenSource _cancelSource = null;
        private bool _triggersTouched = false;
        private readonly object _lock = new object();

        public AsyncProcessScheduler() { }

        public void Trigger()
        {
            _event.Set();
        }

        public Task NextTrigger()
        {
            return InternalNextTrigger();
        }

        private async Task InternalNextTrigger()
        {
            bool needsRerun;

            do
            {
                // Reload all triggers
                var triggers = _triggers; // Volatile read op.

                needsRerun = false;

                CancellationTokenSource cancellationSource;

                lock (_lock)
                {
                    if (_triggersTouched)
                    {
                        _triggersTouched = false;
                        needsRerun = true;
                        continue;
                    }

                    _cancelSource = new CancellationTokenSource();
                    cancellationSource = _cancelSource;
                }

                // Set a new cancellation source
                using (cancellationSource)
                {
                    // Get a list of all triggers
                    var awaitedTasks = triggers.Select(p => p.NextTriggerAsync(_cancelSource.Token)).Append(_event.WaitAsync(_cancelSource.Token));

                    // Asynchronously wait for any tasks to complete.
                    var completedTask = await Task.WhenAny(awaitedTasks);

                    try
                    {
                        // Trigger any thrown exceptions.
                        await completedTask;
                    }
                    catch (OperationCanceledException) when (_cancelSource.IsCancellationRequested)
                    {
                        needsRerun = true;
                    }

                    // Cancel all trigger tasks that were not completed.
                    _cancelSource.Cancel();

                    lock (_lock)
                    {
                        _cancelSource = null;
                    }
                }
            }
            while (needsRerun);

            _event.Reset();
        }

        public void AddTrigger(ITrigger trigger)
        {
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));

            ImmutableHashSet<ITrigger> current = _triggers,
                                       start,
                                       desired;

            do
            {
                start = current;
                desired = start.Add(trigger);
                current = Interlocked.CompareExchange(ref _triggers, desired, start);
            }
            while (start != current);

            if (start != desired)
            {
                lock (_lock)
                {
                    if (_cancelSource == null)
                    {
                        _triggersTouched = true;
                    }
                    else
                    {
                        _cancelSource.Cancel();
                    }
                }
            }
        }

        public void RemoveTrigger(ITrigger trigger)
        {
            if (trigger == null)
                throw new ArgumentNullException(nameof(trigger));

            ImmutableHashSet<ITrigger> current = _triggers,
                                       start,
                                       desired;

            do
            {
                start = current;
                desired = start.Remove(trigger);
                current = Interlocked.CompareExchange(ref _triggers, desired, start);
            }
            while (start != current);

            if (start != desired)
            {
                lock (_lock)
                {
                    if (_cancelSource == null)
                    {
                        _triggersTouched = true;
                    }
                    else
                    {
                        _cancelSource.Cancel();
                    }
                }
            }
        }
    }
}
