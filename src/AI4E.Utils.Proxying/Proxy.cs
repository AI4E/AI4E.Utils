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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AI4E.Utils.Async;

namespace AI4E.Utils.Proxying
{
    internal sealed class Proxy<TRemote> : IProxy<TRemote>, IProxyInternal
        where TRemote : class
    {
        static Proxy()
        {
            var remoteType = typeof(TRemote);
            ProxyHost.AddLoadedRemoteType(remoteType);
        }

        private ProxyHost _host;
        private Action _unregisterAction;
        private readonly Type _objectType;
        private readonly bool _ownsInstance;
        private readonly AsyncDisposeHelper _disposeHelper;

        #region C'tor

        internal Proxy(TRemote instance, bool ownsInstance)
        {
            LocalInstance = instance;

            _disposeHelper = new AsyncDisposeHelper(DisposeInternalAsync);
            _ownsInstance = ownsInstance;
        }

        internal Proxy(ProxyHost host, int id, Type objectType)
        {
            _host = host;
            Id = id;
            _objectType = objectType;
            _disposeHelper = new AsyncDisposeHelper(DisposeInternalAsync);
        }

        #endregion

        public TRemote LocalInstance { get; }
        object IProxyInternal.LocalInstance => LocalInstance;
        object IProxy.LocalInstance => LocalInstance;
        internal bool IsRemoteProxy => LocalInstance == null;
        public Type RemoteType => typeof(TRemote);

        public Type ObjectType => IsRemoteProxy ? _objectType : LocalInstance.GetType();

        public int Id { get; private set; }

        #region Disposal

        public Task Disposal => _disposeHelper.Disposal;

        public void Dispose()
        {
            _disposeHelper.Dispose();
        }

        public Task DisposeAsync()
        {
            return _disposeHelper.DisposeAsync();
        }

        private async Task DisposeInternalAsync()
        {
            if (IsRemoteProxy)
            {
                Debug.Assert(_host != null);

                await _host.Deactivate(Id, cancellation: default);
            }
            else
            {
                _unregisterAction?.Invoke();

                if (_ownsInstance)
                {
                    if (LocalInstance is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else if (LocalInstance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        ~Proxy()
        {
            try
            {
                _disposeHelper.Dispose();
            }
            catch (ObjectDisposedException) { }
        }

        #endregion

        public async Task ExecuteAsync(Expression<Action<TRemote>> expression)
        {
            try
            {
                using (var guard = _disposeHelper.GuardDisposal(cancellation: default))
                {
                    if (IsRemoteProxy)
                    {
                        await _host.SendMethodCallAsync<object>(expression.Body, Id, false);
                    }
                    else
                    {
                        var compiled = expression.Compile();

                        compiled.Invoke(LocalInstance);
                    }
                }
            }
            catch (OperationCanceledException) when (_disposeHelper.IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public async Task ExecuteAsync(Expression<Func<TRemote, Task>> expression)
        {
            try
            {
                using (var guard = _disposeHelper.GuardDisposal(cancellation: default))
                {
                    if (IsRemoteProxy)
                    {
                        await _host.SendMethodCallAsync<object>(expression.Body, Id, true);
                        return;
                    }

                    var compiled = expression.Compile();

                    await compiled.Invoke(LocalInstance);
                }
            }
            catch (OperationCanceledException) when (_disposeHelper.IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression<Func<TRemote, TResult>> expression)
        {
            try
            {
                using (var guard = _disposeHelper.GuardDisposal(cancellation: default))
                {
                    if (IsRemoteProxy)
                    {
                        return await _host.SendMethodCallAsync<TResult>(expression.Body, Id, false);
                    }

                    var compiled = expression.Compile();
                    return compiled.Invoke(LocalInstance);
                }
            }
            catch (OperationCanceledException) when (_disposeHelper.IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression<Func<TRemote, Task<TResult>>> expression)
        {
            try
            {
                using (var guard = _disposeHelper.GuardDisposal(cancellation: default))
                {
                    if (IsRemoteProxy)
                    {
                        return await _host.SendMethodCallAsync<TResult>(expression.Body, Id, true);
                    }

                    var compiled = expression.Compile();

                    return await compiled.Invoke(LocalInstance);
                }
            }
            catch (OperationCanceledException) when (_disposeHelper.IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public async Task<object> ExecuteAsync(MethodInfo method, object[] args)
        {
            Debug.Assert(IsRemoteProxy);

            try
            {
                using (var guard = _disposeHelper.GuardDisposal(cancellation: default))
                {
                    return await _host.SendMethodCallAsync<object>(method, args, Id, typeof(Task).IsAssignableFrom(method.ReturnType));
                }
            }
            catch (OperationCanceledException) when (_disposeHelper.IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public void Register(ProxyHost host, int proxyId, Action unregisterAction)
        {
            if (unregisterAction == null)
                throw new ArgumentNullException(nameof(unregisterAction));

            _host = host;
            Id = proxyId;
            _unregisterAction = unregisterAction;
        }

        public IProxy<TCast> Cast<TCast>()
            where TCast : class
        {
            if (!typeof(TCast).IsAssignableFrom(ObjectType))
                throw new ArgumentException($"Unable to cast the proxy. The type {ObjectType} cannot be cast to type {typeof(TCast)}.");

            return new CastProxy<TRemote, TCast>(this);
        }

        public TRemote AsTransparentProxy()
        {
            return AsTransparentProxy<TRemote>();
        }

        public TCast AsTransparentProxy<TCast>()
            where TCast : class
        {
            if (!typeof(TCast).IsInterface)
                throw new NotSupportedException("The proxy type must be an interface.");

            var result = TransparentProxy<TCast>.Create(this);
            var type = result.GetType();
            ProxyHost.AddTransparentProxyType(type);

            return result;
        }
    }
}
