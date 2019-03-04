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
using System.Reflection;
using System.Threading.Tasks;
using AI4E.Utils.Async;

namespace AI4E.Utils.Proxying
{
    /// <summary>
    /// A base type for transparent proxies. This type is not meant to be used directly.
    /// </summary>
    /// <typeparam name="TRemote">The type of remote proxy.</typeparam>
    /// <typeparam name="TCast">The type of dynamic proxy instance.</typeparam>
    public class TransparentProxy<TRemote, TCast> : DispatchProxy, ProxyHost.IProxyInternal
            where TRemote : class
            where TCast : class
    {
        internal ProxyHost.IProxyInternal Proxy { get; private set; }

        #region IProxyInternal

        object ProxyHost.IProxyInternal.LocalInstance => Proxy.LocalInstance;

        Type ProxyHost.IProxyInternal.RemoteType => Proxy.RemoteType;
        Type ProxyHost.IProxyInternal.ObjectType => Proxy.ObjectType;
        int ProxyHost.IProxyInternal.Id => Proxy.Id;

        void ProxyHost.IProxyInternal.Register(ProxyHost host, int proxyId, Action unregisterAction) => Proxy.Register(host, proxyId, unregisterAction);

        Task<object> ProxyHost.IProxyInternal.ExecuteAsync(MethodInfo method, object[] args) => Proxy.ExecuteAsync(method, args);

        void IDisposable.Dispose()
        {
            Proxy.Dispose();
        }

        void IAsyncDisposable.Dispose()
        {
            Proxy.Dispose();
        }

        Task IAsyncDisposable.DisposeAsync()
        {
            return Proxy.DisposeAsync();
        }

        Task IAsyncDisposable.Disposal => Proxy.Disposal;

        #endregion

        /// <inheritdoc />
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            var task = Proxy.ExecuteAsync(targetMethod, args);

            if (typeof(Task).IsAssignableFrom(targetMethod.ReturnType))
            {
                if (targetMethod.ReturnType.IsGenericType &&
                   targetMethod.ReturnType.GetGenericArguments()[0] != Type.GetType("System.Threading.Tasks.VoidTaskResult"))
                {
                    // Convert the task to the correct type.
                    return TaskConvertHelper.ConvertTask(task, targetMethod.ReturnType.GetGenericArguments()[0]);
                }

                return task;
            }
            else if (typeof(void).IsAssignableFrom(targetMethod.ReturnType))
            {
                // There is no result => Fire and forger
                return null;
            }
            else
            {
                // We have to wait for the task's result.
                task.ConfigureAwait(false).GetAwaiter().GetResult();
                return task.GetResult();
            }
        }

        private void Configure(ProxyHost.IProxyInternal proxy)
        {
            Proxy = proxy;
        }

        internal static TCast Create(ProxyHost.IProxyInternal proxy)
        {
            object transparentProxy = Create<TCast, TransparentProxy<TRemote, TCast>>();

            ((TransparentProxy<TRemote, TCast>)transparentProxy).Configure(proxy);

            return (TCast)transparentProxy;
        }
    }
}
