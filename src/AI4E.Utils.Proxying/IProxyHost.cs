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

namespace AI4E.Utils.Proxying
{
    /// <summary>
    /// Represents a proxy host.
    /// </summary>
    public interface IProxyHost : IDisposable
    {
        /// <summary>
        /// Asynchronously creates a new instance of the specified type on the remote end-point and returns a proxy for it.
        /// </summary>
        /// <typeparam name="TRemote">The type of instance to create.</typeparam>
        /// <param name="cancellation">
        /// A <see cref="CancellationToken"/> used to cancel the asynchronous operation, or <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// When evaluated, the tasks result contains the proxy for the remote object.
        /// </returns>
        Task<IProxy<TRemote>> CreateAsync<TRemote>(CancellationToken cancellation)
            where TRemote : class;

        /// <summary>
        /// Asynchronously creates a new instance of the specified type on the remote end-point and returns a proxy for it.
        /// </summary>
        /// <typeparam name="TRemote">The type of instance to create.</typeparam>
        /// <param name="parameter">An array of objects that are used to construct the remote object.</param>
        /// <param name="cancellation">
        /// A <see cref="CancellationToken"/> used to cancel the asynchronous operation, or <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// When evaluated, the tasks result contains the proxy for the remote object.
        /// </returns>
        Task<IProxy<TRemote>> CreateAsync<TRemote>(object[] parameter, CancellationToken cancellation)
            where TRemote : class;

        /// <summary>
        /// Asynchronously loads an instace of the specified type from the remoted service provider and returns a proxy for it.
        /// </summary>
        /// <typeparam name="TRemote">The type of instance to load.</typeparam>
        /// <param name="cancellation">
        /// A <see cref="CancellationToken"/> used to cancel the asynchronous operation, or <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// When evaluated, the tasks result contains the proxy for the remote object.
        /// </returns>
        Task<IProxy<TRemote>> LoadAsync<TRemote>(CancellationToken cancellation)
            where TRemote : class;
    }
}
