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
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Processing
{
    public sealed class TriggerableAsyncProcess : IAsyncProcess, ITriggerableAsyncProcess
    {
        #region Fields

        private readonly IAsyncProcess _dynamicProcess;
        private readonly AsyncProcessScheduler _scheduler = new AsyncProcessScheduler();
        private readonly Func<CancellationToken, Task> _operation;
        private int _operating = 0; // 0 = Idle, 1 = Running

        #endregion

        /// <summary>
        /// Creates a new instance of the <see cref="Process"/> type with the specified execution operation.
        /// </summary>
        /// <param name="operation">The asynchronous execution operation.</param>
        public TriggerableAsyncProcess(Func<CancellationToken, Task> operation) // The operation is guaranteed not to run concurrently.
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _operation = operation;
            _dynamicProcess = new AsyncProcess(DynamicExecute);
        }

        #region Properties

        public Task Startup => _dynamicProcess.Startup;
        public Task Termination => _dynamicProcess.Termination;

        /// <summary>
        /// Gets the state of the process.
        /// </summary>
        public TriggerableAsyncProcessState State => (TriggerableAsyncProcessState)((int)_dynamicProcess.State & (Volatile.Read(ref _operating) << 4));

        AsyncProcessState IAsyncProcess.State => _dynamicProcess.State;

        #endregion

        /// <summary>
        /// Starts the dynamic process operation.
        /// </summary>
        public void Start()
        {
            _dynamicProcess.Start();
        }

        public Task StartAsync(CancellationToken cancellation)
        {
            return _dynamicProcess.StartAsync(cancellation);
        }

        /// <summary>
        /// Terminates the dynamic process operation.
        /// </summary>
        public void Terminate()
        {
            _dynamicProcess.Terminate();
        }

        public Task TerminateAsync(CancellationToken cancellation)
        {
            return _dynamicProcess.TerminateAsync(cancellation);
        }

        public void TriggerExecution()
        {
            _scheduler.Trigger();
        }

        /// <summary>
        /// Registers a dynamic process trigger.
        /// </summary>
        /// <param name="trigger">The trigger that shall be registered.</param>
        public void RegisterTrigger(ITrigger trigger)
        {
            _scheduler.AddTrigger(trigger);
        }

        /// <summary>
        /// Unregisteres a dyanamic process trigger.
        /// </summary>
        /// <param name="trigger">The trigger that shall be unregistered.</param>
        public void UnregisterTrigger(ITrigger trigger)
        {
            _scheduler.RemoveTrigger(trigger);
        }

        private async Task DynamicExecute(CancellationToken cancellation)
        {
            while (cancellation.ThrowOrContinue())
            {
                await _scheduler.NextTrigger().ConfigureAwait(false);

                await StaticExecute(cancellation).ConfigureAwait(false);
            }
        }

        private async Task StaticExecute(CancellationToken cancellation)
        {
            if (Interlocked.Exchange(ref _operating, 1) != 0)
                return;

            try
            {
                await _operation(cancellation).ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref _operating, 0);
            }
        }
    }
}
