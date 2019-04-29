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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying
{
    /// <summary>
    /// Represents a proxy.
    /// </summary>
    public interface IProxy : IAsyncDisposable
    {
        Task Disposal { get; }

        /// <summary>
        /// Gets the local proxied instance or null if this is a remote proxy.
        /// </summary>
        object LocalInstance { get; }

        /// <summary>
        /// Gets the static type of the proxy.
        /// </summary>
        Type RemoteType { get; }

        /// <summary>
        /// Asynchronously returns the dynamic type of the proxied instance.
        /// </summary>
        /// <param name="cancellation">A <see cref="CancellationToken"/> used to cancel the asynchronous operation or <see cref="CancellationToken.None"/>.</param>
        /// <returns>
        /// A <see cref="ValueTask{Type}"/> that represents the asynchronous operation.
        /// When evaluated, the tasks result contains the dynamic type of the proxied instance.
        /// </returns>
        ValueTask<Type> GetObjectTypeAsync(CancellationToken cancellation = default);

        /// <summary>
        /// Casts the proxy to a proxy with the specified remote type.
        /// </summary>
        /// <typeparam name="TCast">The remote type of the result proxy.</typeparam>
        /// <returns>The cast proxy.</returns>
        /// <exception cref="ArgumentException">Thrown if the object type is not assignable to <typeparamref name="TCast"/>.</exception>
        IProxy<TCast> Cast<TCast>() where TCast : class;
    }

    /// <summary>
    /// Represents a proxy of the specified type.
    /// </summary>
    /// <typeparam name="TRemote">The static type of the proxied object.</typeparam>
    public interface IProxy<TRemote> : IProxy
        where TRemote : class
    {
        /// <summary>
        /// Gets the local proxied instance or null if this is a remote proxy.
        /// </summary>
        new TRemote LocalInstance { get; }

        /// <summary>
        /// Asynchronously invokes a member on the proxy instance.
        /// </summary>
        /// <param name="expression">The expression that described the invokation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ExecuteAsync(Expression<Action<TRemote>> expression);

        /// <summary>
        /// Asynchronously invokes a member on the proxy instance.
        /// </summary>
        /// <param name="expression">The expression that described the invokation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ExecuteAsync(Expression<Func<TRemote, Task>> expression);

        /// <summary>
        /// Asynchronously invokes a member on the proxy instance.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="expression">The expression that described the invokation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// When evaluated, the tasks result contains the result of the invokation.
        /// </returns>
        Task<TResult> ExecuteAsync<TResult>(Expression<Func<TRemote, TResult>> expression);

        /// <summary>
        /// Asynchronously invokes a member on the proxy instance.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="expression">The expression that described the invokation.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation.
        /// When evaluated, the tasks result contains the result of the invokation.
        /// </returns>
        Task<TResult> ExecuteAsync<TResult>(Expression<Func<TRemote, Task<TResult>>> expression);

        /// <summary>
        /// Returns a transparent proxy for the proxy.
        /// </summary>
        /// <returns>The transparent proxy.</returns>
        /// <exception cref="NotSupportedException">Thrown if <typeparamref name="TRemote"/> is not an interface.</exception>
        TRemote AsTransparentProxy();
    }
}
