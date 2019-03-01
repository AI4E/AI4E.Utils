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

using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Processing
{
    /// <summary>
    /// Represents an asynchronous process.
    /// </summary>
    public interface IAsyncProcess
    {
        /// <summary>
        /// Gets the state of the process.
        /// </summary>
        AsyncProcessState State { get; }

        Task Startup { get; }
        Task Termination { get; }

        void Start();
        void Terminate();

        /// <summary>
        /// Asynchronously starts the process.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartAsync(CancellationToken cancellation = default);

        /// <summary>
        /// Asynchronously terminates the process.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TerminateAsync(CancellationToken cancellation = default);
    }

    /// <summary>
    /// Represents the state of an asynchronous process.
    /// </summary>
    public enum AsyncProcessState
    {
        /// <summary>
        /// The processis either in is initial state or terminated.
        /// </summary>
        Terminated = 0,

        /// <summary>
        /// The process is running currently.
        /// </summary>
        Running = 1,

        /// <summary>
        /// The process terminated with an exception. 
        /// Call <see cref=IAsyncProcess.TerminateAsync"/> to rethrow the exception.
        /// </summary>
        Failed = 2
    }
}
