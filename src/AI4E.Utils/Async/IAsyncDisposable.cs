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

namespace AI4E.Utils.Async
{
    /// <summary>
    /// Represents a type that requires asynchronous disposal.
    /// </summary>
    public interface IAsyncDisposable : IDisposable
    {
        /// <summary>
        /// Starts the asynchronous disposal. Get <see cref="Disposal"/> to get notified of the disposal state.
        /// </summary>
        new void Dispose();

        /// <summary>
        /// Asynchronously disposes of the current instance. 
        /// This is functionally equivalent with calling <see cref="Dispose"/> and retrieving <see cref="Disposal"/>.
        /// </summary>
        /// <returns>A task representing the asynchronous disposal of the instance.</returns>
        Task DisposeAsync();

        /// <summary>
        /// Gets a task representing the asynchronous disposal of the instance.
        /// </summary>
        Task Disposal { get; }
    }
}
