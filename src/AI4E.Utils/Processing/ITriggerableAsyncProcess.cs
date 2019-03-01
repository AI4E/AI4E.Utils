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

namespace AI4E.Utils.Processing
{
    /// <summary>
    /// Represents a triggerable async process.
    /// </summary>
    public interface ITriggerableAsyncProcess : IAsyncProcess
    {
        /// <summary>
        /// Gets the process state.
        /// </summary>
        new TriggerableAsyncProcessState State { get; }

        /// <summary>
        /// Registers a trigger.
        /// </summary>
        /// <param name="trigger">The trigger to register.</param>
        /// /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="trigger"/> is null.</exception>
        void RegisterTrigger(ITrigger trigger);

        /// <summary>
        /// Unregisters a trigger.
        /// </summary>
        /// <param name="trigger">The trigger to unregister.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="trigger"/> is null.</exception>
        void UnregisterTrigger(ITrigger trigger);

        /// <summary>
        /// Triggers the execution explicitely.
        /// </summary>
        void TriggerExecution();
    }

    /// <summary>
    /// Represents the state of a triggerable process.
    /// </summary>
    public enum TriggerableAsyncProcessState
    {
        /// <summary>
        /// The process is in its initial state or terminated.
        /// </summary>
        Terminated = 0x00, // Static: Idle Dynamic: Terminated

        /// <summary>
        /// The process waits to be scheduled due to a trigger signal.
        /// </summary>
        WaitingForActivation = 0x01, // Static: Idle, Dynamic: Running

        /// <summary>
        /// The process terminated failing.
        /// </summary>
        Failed = 0x02, // Static: Idle, Dynamic: Failed

        /// <summary>
        /// The process is running once due to an external signal but is not beeing scheduled.
        /// </summary>
        RunningOnce = 0x10, // Static: Running, Dynamic: Terminated

        /// <summary>
        /// The process is running.
        /// </summary>
        Running = 0x11, // Static: Running, Dynamic: Running

        /// <summary>
        /// The process is currently running due to an external signal but its scheduled execution failed.
        /// </summary>
        RunningOnceFailed = 0x12, // Static: Running, Dynamic: Failed // TODO: Better name?
    }
}
