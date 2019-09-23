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

#nullable disable

using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying
{
    /// <summary>
    /// A base type for transparent proxies. This type is not meant to be used directly.
    /// </summary>
    /// <typeparam name="T">The type of dynamic proxy instance.</typeparam>
#pragma warning disable CA1063
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TransparentProxy<T> : DispatchProxy, IProxyInternal
#pragma warning restore CA1063
            where T : class
    {
        internal IProxyInternal Proxy { get; private set; }

        #region IProxyInternal

        object IProxyInternal.LocalInstance => Proxy.LocalInstance;
        Type IProxyInternal.RemoteType => Proxy.RemoteType;
        int IProxyInternal.Id => Proxy.Id;

        ActivationMode IProxyInternal.ActivationMode => Proxy.ActivationMode;
        object[] IProxyInternal.ActivationParamers => Proxy.ActivationParamers;
        bool IProxyInternal.IsActivated => Proxy.IsActivated;

        void IProxyInternal.Activate(Type objectType)
        {
            Proxy.Activate(objectType);
        }

        ValueTask<Type> IProxyInternal.GetObjectTypeAsync(CancellationToken cancellation)
        {
            return Proxy.GetObjectTypeAsync(cancellation);
        }

        void IProxyInternal.Register(ProxyHost host, int proxyId, Action unregisterAction)
        {
            Proxy.Register(host, proxyId, unregisterAction);
        }

        Task<object> IProxyInternal.ExecuteAsync(MethodInfo method, object[] args)
        {
            return Proxy.ExecuteAsync(method, args);
        }

#pragma warning disable CA1063, CA1033, CA1816
        void IDisposable.Dispose()
#pragma warning restore CA1063, CA1033, CA1816
        {
            Proxy.Dispose();
        }
#pragma warning disable CA1063, CA1033, CA1816
        ValueTask IAsyncDisposable.DisposeAsync()
#pragma warning restore CA1063, CA1033, CA1816
        {
            return Proxy.DisposeAsync();
        }

        #endregion

        /// <inheritdoc />
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            var task = Proxy.ExecuteAsync(targetMethod, args);

            if (targetMethod.ReturnType.IsTaskType(out var resultType))
            {
                // Convert the task to the correct type.
                return task.Convert(resultType);
            }
            else if (typeof(void).IsAssignableFrom(targetMethod.ReturnType))
            {
                // There is no result => Fire and forget
                return null;
            }
            else
            {
                // We have to wait for the task's result.
                return task.GetResultOrDefault();
            }
        }

        private void Configure(IProxyInternal proxy)
        {
            Proxy = proxy;
        }

        internal static T Create(IProxyInternal proxy)
        {
            object transparentProxy = Create<T, TransparentProxy<T>>();

            ((TransparentProxy<T>)transparentProxy).Configure(proxy);

            return (T)transparentProxy;
        }
    }
}

#nullable enable
