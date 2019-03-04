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
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AI4E.Utils.Async;
using AI4E.Utils.Processing;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;

namespace AI4E.Utils.Proxying
{
    /// <summary>
    /// Represents a proxy host.
    /// </summary>
    public sealed class ProxyHost : IAsyncDisposable, IProxyHost
    {
        #region Fields

        private readonly Stream _stream;
        private readonly IServiceProvider _serviceProvider;
        private readonly AsyncLock _sendLock = new AsyncLock();
        private readonly IAsyncProcess _receiveProcess;
        private readonly ConcurrentDictionary<int, (Action<MessageType, object> callback, Type resultType)> _responseTable
            = new ConcurrentDictionary<int, (Action<MessageType, object> callback, Type resultType)>();
        private readonly Dictionary<object, IProxyInternal> _proxyLookup = new Dictionary<object, IProxyInternal>();
        private readonly Dictionary<int, IProxyInternal> _proxies = new Dictionary<int, IProxyInternal>();
        private readonly object _proxyLock = new object();
        private readonly AsyncDisposeHelper _disposeHelper;

        private int _nextSeqNum = 0;
        private int _nextProxyId = 0;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of the <see cref="ProxyHost"/> type.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> that is used to communicate with the remote end-point.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> that is used to resolve services.</param>
        public ProxyHost(Stream stream, IServiceProvider serviceProvider)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            _stream = stream;
            _serviceProvider = serviceProvider;

            _receiveProcess = new AsyncProcess(ReceiveProcess, start: true);
            _disposeHelper = new AsyncDisposeHelper(DisposeInternalAsync);
        }

        #endregion

        #region Activation

        /// <inheritdoc />
        public Task<IProxy<TRemote>> CreateAsync<TRemote>(object[] parameter, CancellationToken cancellation)
            where TRemote : class
        {
            return ActivateAsync<TRemote>(ActivationMode.Create, parameter ?? new object[0], cancellation);
        }

        /// <inheritdoc />
        public Task<IProxy<TRemote>> CreateAsync<TRemote>(CancellationToken cancellation)
            where TRemote : class
        {
            return ActivateAsync<TRemote>(ActivationMode.Create, new object[0], cancellation);
        }

        /// <inheritdoc />
        public Task<IProxy<TRemote>> LoadAsync<TRemote>(CancellationToken cancellation)
            where TRemote : class
        {
            return ActivateAsync<TRemote>(ActivationMode.Load, parameter: null, cancellation);
        }

        private async Task<IProxy<TRemote>> ActivateAsync<TRemote>(ActivationMode mode, object[] parameter, CancellationToken cancellation)
            where TRemote : class
        {
            int seqNum;
            Task<IProxy<TRemote>> result;

            using (var stream = new MemoryStream())
            {
                do
                {
                    seqNum = Interlocked.Increment(ref _nextSeqNum);

                    stream.Position = 0;
                    stream.SetLength(0);

                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.Write((byte)MessageType.Activation);
                        writer.Write(seqNum);

                        writer.Write((byte)mode);
                        WriteType(writer, typeof(TRemote));
                        Serialize(writer, parameter?.Select(p => (p, p?.GetType())));
                    }
                }
                while (!TryGetResultTask(seqNum, out result));

                stream.Position = 0;
                await SendAsync(stream, cancellation);
            }
            return await result;
        }

        internal async Task Deactivate(int proxyId, CancellationToken cancellation)
        {
            var seqNum = Interlocked.Increment(ref _nextSeqNum);

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write((byte)MessageType.Deactivation);
                    writer.Write(seqNum);
                    writer.Write(proxyId);
                }

                stream.Position = 0;
                await SendAsync(stream, cancellation);
            }
        }

        private enum ActivationMode : byte
        {
            Create,
            Load
        }

        #endregion

        #region Disposal

        /// <inheritdoc />
        public Task Disposal => _disposeHelper.Disposal;

        /// <inheritdoc />
        public void Dispose()
        {
            _disposeHelper.Dispose();
        }

        /// <inheritdoc />
        public Task DisposeAsync()
        {
            return _disposeHelper.DisposeAsync();
        }

        private async Task DisposeInternalAsync()
        {
            await _receiveProcess.TerminateAsync().HandleExceptionsAsync();

            IEnumerable<IProxyInternal> proxies;

            lock (_proxyLock)
            {
                proxies = _proxies.Values.ToList();
            }

            await Task.WhenAll(proxies.Select(p => p.DisposeAsync())).HandleExceptionsAsync();
        }

        #endregion

        #region Proxies

        private IProxyInternal RegisterLocalProxy(IProxyInternal proxy)
        {
            lock (_proxyLock)
            {
                if (_proxyLookup.TryGetValue(proxy.LocalInstance, out var existing))
                {
                    return existing;
                }

                var id = Interlocked.Increment(ref _nextProxyId);

                proxy.Register(this, id, () => UnregisterLocalProxy(proxy));

                _proxyLookup.Add(proxy.LocalInstance, proxy);
                _proxies.Add(id, proxy);
            }

            return proxy;
        }

        private void UnregisterLocalProxy(IProxyInternal proxy)
        {
            lock (_proxyLock)
            {
                _proxyLookup.Remove(proxy.LocalInstance);
                _proxies.Remove(proxy.Id);
            }
        }

        private bool TryGetProxyById(int proxyId, out IProxyInternal proxy)
        {
            lock (_proxyLock)
            {
                return _proxies.TryGetValue(proxyId, out proxy);
            }
        }

        /// <summary>
        /// Gets a collection of registered local proxies.
        /// FOR TEST AND DEBUGGING PUPOSES ONLY.
        /// </summary>
        internal IReadOnlyCollection<IProxyInternal> LocalProxies
        {
            get
            {
                lock (_proxyLock)
                {
                    return _proxies.Values.ToImmutableList();
                }
            }
        }

        /// <summary>
        /// Creates a new proxy from the specified object instance.
        /// </summary>
        /// <typeparam name="TRemote">The type of object that a proxy is created for.</typeparam>
        /// <param name="instance">The instance a proxy is created for.</param>
        /// <param name="ownsInstance">A boolean value indicating whether the proxy host owns the instance.</param>
        /// <returns>The create proxy.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is <c>null</c>.</exception>
        public static IProxy<TRemote> CreateProxy<TRemote>(TRemote instance, bool ownsInstance = false)
            where TRemote : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return new Proxy<TRemote>(instance, ownsInstance);
        }

        private static readonly MethodInfo _createProxyMethodDefinition =
            typeof(ProxyHost)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .SingleOrDefault(p => p.Name == nameof(CreateProxy) && p.IsGenericMethodDefinition && p.GetGenericArguments().Length == 1);

        internal static IProxyInternal CreateProxy(Type remoteType, object instance, bool ownsInstance)
        {
            var createProxyMethod = _createProxyMethodDefinition.MakeGenericMethod(remoteType);
            return (IProxyInternal)createProxyMethod.Invoke(obj: null, new object[] { instance, ownsInstance });
        }

        internal interface IProxyInternal : IAsyncDisposable
        {
            object LocalInstance { get; }

            Type RemoteType { get; }
            Type ObjectType { get; }
            int Id { get; }

            void Register(ProxyHost host, int proxyId, Action unregisterAction);

            Task<object> ExecuteAsync(MethodInfo method, object[] args);
        }

        internal sealed class Proxy<TRemote> : IProxy<TRemote>, IProxyInternal
            where TRemote : class
        {
            private ProxyHost _host;
            private Action _unregisterAction;
            private readonly Type _remoteType;
            private readonly bool _ownsInstance;
            private readonly AsyncDisposeHelper _disposeHelper;

            #region C'tor

            internal Proxy(TRemote instance, bool ownsInstance)
            {
                LocalInstance = instance;

                _disposeHelper = new AsyncDisposeHelper(DisposeInternalAsync);
                _ownsInstance = ownsInstance;
            }

            internal Proxy(ProxyHost host, int id, Type remoteType)
            {
                _host = host;
                Id = id;
                _remoteType = remoteType;
                _disposeHelper = new AsyncDisposeHelper(DisposeInternalAsync);
            }

            #endregion

            public TRemote LocalInstance { get; }

            public Type ObjectType => IsRemoteProxy ? _remoteType : LocalInstance.GetType();

            public Type RemoteType => typeof(TRemote);

            public int Id { get; private set; }

            object IProxyInternal.LocalInstance => LocalInstance;
            object IProxy.LocalInstance => LocalInstance;

            internal bool IsRemoteProxy => LocalInstance == null;

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

                if (IsRemoteProxy)
                {
                    return TransparentProxy<TRemote, TCast>.Create(this);
                }
                else
                {
                    return (TCast)(object)LocalInstance;
                }
            }
        }

        internal sealed class CastProxy<TRemote, TCast> : IProxy<TCast>
            where TRemote : class
            where TCast : class
        {
            public CastProxy(Proxy<TRemote> original)
            {
                Original = original;
            }

            public TCast LocalInstance => Original.IsRemoteProxy ? null : (TCast)(object)Original.LocalInstance;

            object IProxy.LocalInstance => LocalInstance;

            public Type ObjectType => Original.ObjectType;

            public Type RemoteType => typeof(TCast);

            public int Id => Original.Id;

            internal Proxy<TRemote> Original { get; }

            private Expression<TDelegate> ConvertExpression<TDelegate>(LambdaExpression expression)
                where TDelegate : Delegate
            {
                var parameter = expression.Parameters.First();
                var body = expression.Body;

                var newParameter = Expression.Parameter(typeof(TRemote));
                var newBody = ParameterExpressionReplacer.ReplaceParameter(body, parameter, newParameter);
                return Expression.Lambda<TDelegate>(newBody, newParameter);
            }

            public Task ExecuteAsync(Expression<Action<TCast>> expression)
            {
                return Original.ExecuteAsync(ConvertExpression<Action<TRemote>>(expression));
            }

            public Task ExecuteAsync(Expression<Func<TCast, Task>> expression)
            {
                return Original.ExecuteAsync(ConvertExpression<Func<TRemote, Task>>(expression));
            }

            public Task<TResult> ExecuteAsync<TResult>(Expression<Func<TCast, TResult>> expression)
            {
                return Original.ExecuteAsync(ConvertExpression<Func<TRemote, TResult>>(expression));
            }

            public Task<TResult> ExecuteAsync<TResult>(Expression<Func<TCast, Task<TResult>>> expression)
            {
                return Original.ExecuteAsync(ConvertExpression<Func<TRemote, Task<TResult>>>(expression));
            }

            public IProxy<T> Cast<T>() where T : class
            {
                return Original.Cast<T>();
            }

            public void Dispose()
            {
                Original.Dispose();
            }

            public Task DisposeAsync()
            {
                return Original.DisposeAsync();
            }

            public Task Disposal => Original.Disposal;

            public TCast AsTransparentProxy()
            {
                return Original.AsTransparentProxy<TCast>();
            }
        }

        #endregion

        #region Receive

        private async Task ReceiveProcess(CancellationToken cancellation)
        {
            var messageLengthBytes = new byte[4];
            while (cancellation.ThrowOrContinue())
            {
                try
                {
                    try
                    {
                        await _stream.ReadExactAsync(messageLengthBytes, offset: 0, count: messageLengthBytes.Length, cancellation);
                        var messageLength = BinaryPrimitives.ReadInt32LittleEndian(messageLengthBytes.AsSpan());

                        var buffer = ArrayPool<byte>.Shared.Rent(messageLength);
                        try
                        {
                            await _stream.ReadExactAsync(buffer, offset: 0, count: messageLength, cancellation);

                            // We do not want the process to be disturbed/blocked/deadlocked
                            Task.Run(async () =>
                            {
                                try
                                {
                                    using (var messageStream = new MemoryStream(buffer, index: 0, count: messageLength, writable: false))
                                    {
                                        await HandleMessageAsync(messageStream, cancellation);
                                    }
                                }
                                finally
                                {
                                    ArrayPool<byte>.Shared.Return(buffer);
                                }

                            }).HandleExceptions();
                        }
                        catch
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                            throw;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Do not call DisposeAsync. This will result in a deadlock.
                        Dispose();
                        return;
                    }
                    catch (IOException)
                    {
                        // Do not call DisposeAsync. This will result in a deadlock.
                        Dispose();
                        return;
                    }
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
                catch (Exception)
                {
                    // TODO: Log exception
                }
            }
        }

        private async Task HandleMessageAsync(Stream stream, CancellationToken cancellation)
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false))
            {
                var messageType = (MessageType)reader.ReadByte();
                var seqNum = reader.ReadInt32();

                switch (messageType)
                {
                    case MessageType.ReturnValue:
                    case MessageType.ReturnException:
                        ReceiveResult(messageType, reader);
                        break;

                    case MessageType.MethodCall:
                        await ReceiveMethodCallAsync(reader, seqNum, cancellation);
                        break;

                    case MessageType.Activation:
                        await ReceiveActivationAsync(reader, seqNum, cancellation);
                        break;

                    case MessageType.Deactivation:
                        ReceiveDeactivation(reader);
                        break;
                }
            }
        }

        private void ReceiveDeactivation(BinaryReader reader)
        {
            var proxyId = reader.ReadInt32();

            if (TryGetProxyById(proxyId, out var proxy))
            {
                proxy.Dispose();
            }
        }

        private async Task ReceiveActivationAsync(BinaryReader reader, int seqNum, CancellationToken cancellation)
        {
            var result = default(object);
            var exception = default(Exception);

            try
            {
                var mode = (ActivationMode)reader.ReadByte();
                var type = ReadType(reader);
                var parameter = Deserialize(reader, (ParameterInfo[])null).ToArray();
                var instance = default(object);
                var ownsInstance = false;

                if (mode == ActivationMode.Create)
                {
                    instance = ActivatorUtilities.CreateInstance(_serviceProvider, type, parameter);
                    ownsInstance = true;
                }
                else if (mode == ActivationMode.Load)
                {
                    instance = _serviceProvider.GetRequiredService(type);
                }

                var proxy = (IProxyInternal)Activator.CreateInstance(typeof(Proxy<>).MakeGenericType(type), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { instance, ownsInstance }, null);
                result = RegisterLocalProxy(proxy);
            }
            catch (TargetInvocationException exc)
            {
                exception = exc.InnerException;
            }
            catch (Exception exc)
            {
                exception = exc;
            }

            await SendResult(seqNum, result, result.GetType(), exception, waitTask: false, cancellation);
        }

        private async Task ReceiveMethodCallAsync(BinaryReader reader, int seqNum, CancellationToken cancellation)
        {
            var result = default(object);
            var exception = default(Exception);
            var waitTask = false;

            MethodInfo method;
            Type returnType = null;

            try
            {
                var proxyId = reader.ReadInt32();
                waitTask = reader.ReadBoolean();
                method = DeserializeMethod(reader);
                var arguments = Deserialize(reader, method.GetParameters()).ToArray();

                if (!TryGetProxyById(proxyId, out var proxy))
                {
                    throw new Exception("Proxy not found."); // TODO
                }

                var instance = proxy.LocalInstance;

                if (instance == null)
                {
                    throw new Exception("Proxy not found."); // TODO
                }

                result = method.Invoke(instance, arguments);
                returnType = method.ReturnType;
            }
            catch (TargetInvocationException exc)
            {
                exception = exc.InnerException;
            }
            catch (Exception exc)
            {
                exception = exc;
            }

            await SendResult(seqNum, result, returnType, exception, waitTask, cancellation);
        }

        private void ReceiveResult(MessageType messageType, BinaryReader reader)
        {
            var corr = reader.ReadInt32();

            if (_responseTable.TryRemove(corr, out var entry))
            {
                var value = Deserialize(reader, expectedType: entry.resultType);
                entry.callback(messageType, value);
            }
        }

        #endregion

        #region Send

        private readonly byte[] _sendMessageLengthBuffer = new byte[4];

        private async Task SendAsync(Stream stream, CancellationToken cancellation)
        {
            using (await _sendLock.LockAsync())
            {
                var messageLength = checked((int)stream.Length);
                BinaryPrimitives.WriteInt32LittleEndian(_sendMessageLengthBuffer.AsSpan(), messageLength);

                await _stream.WriteAsync(_sendMessageLengthBuffer, offset: 0, count: _sendMessageLengthBuffer.Length);

                await stream.CopyToAsync(_stream, bufferSize: 81920, cancellation);
            }
        }

        private async Task SendResult(int corrNum, object result, Type resultType, Exception exception, bool waitTask, CancellationToken cancellation)
        {
            if (exception == null && waitTask)
            {
                try
                {
                    var t = (Task)result;
                    await t;
                    result = t.GetResult();

                    if (!t.GetType().IsGenericType || t.GetType().GetGenericArguments()[0] == Type.GetType("System.Threading.Tasks.VoidTaskResult"))
                    {
                        resultType = typeof(void);
                    }
                    else
                    {
                        resultType = t.GetType().GetGenericArguments()[0];
                    }
                }
                catch (Exception exc)
                {
                    exception = exc;
                }
            }

            var seqNum = Interlocked.Increment(ref _nextSeqNum);

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    writer.Write(exception == null ? (byte)MessageType.ReturnValue : (byte)MessageType.ReturnException);
                    writer.Write(seqNum);
                    writer.Write(corrNum);

                    if (exception != null)
                    {
                        Serialize(writer, exception, exception.GetType());
                    }
                    else
                    {
                        Serialize(writer, result, resultType);
                    }
                }

                stream.Position = 0;
                await SendAsync(stream, cancellation);
            }
        }

        internal Task<TResult> SendMethodCallAsync<TResult>(Expression expression, int proxyId, bool waitTask)
        {
            var method = default(MethodInfo);
            var parameters = Array.Empty<object>();

            if (expression is MethodCallExpression methodCallExpression)
            {
                method = methodCallExpression.Method;
                parameters = methodCallExpression.Arguments.Select(p => p.GetExpressionValue()).ToArray();
            }
            else if (expression is MemberExpression memberExpression && memberExpression.Member is PropertyInfo property)
            {
                method = property.GetGetMethod();
            }
            else
            {
                throw new InvalidOperationException(); // TODO: What about Property writes? What about indexed properties?
            }

            return SendMethodCallAsync<TResult>(method, parameters, proxyId, waitTask);
        }

        internal async Task<TResult> SendMethodCallAsync<TResult>(MethodInfo method, object[] args, int proxyId, bool waitTask)
        {
            // TODO: Add sanity checks.

            var seqNum = default(int);
            var task = default(Task<TResult>);

            using (var stream = new MemoryStream())
            {
                do
                {
                    seqNum = Interlocked.Increment(ref _nextSeqNum);

                    stream.Position = 0;
                    stream.SetLength(0);

                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.Write((byte)MessageType.MethodCall);
                        writer.Write(seqNum);

                        writer.Write(proxyId);
                        writer.Write(waitTask);
                        SerializeMethod(writer, method);
                        Serialize(writer, args.ElementWiseMerge(method.GetParameters(), (arg, param) => (arg, param.ParameterType)));
                        writer.Flush();
                    }
                }
                while (!TryGetResultTask(seqNum, out task));

                stream.Position = 0;
                await SendAsync(stream, cancellation: default);
            }

            return await task;
        }

        private bool TryGetResultTask<TResult>(int seqNum, out Task<TResult> task)
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>();

            void Callback(MessageType msgType, object value)
            {
                if (msgType == MessageType.ReturnValue)
                {
                    taskCompletionSource.SetResult((TResult)value);
                }
                else
                {

                    if (value is Exception exc)
                    {
                        var preserveStackTrace = typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (preserveStackTrace != null)
                            preserveStackTrace.Invoke(exc, null);
                    }
                    else
                    {
                        exc = new Exception();
                    }

                    taskCompletionSource.SetException(exc);
                }
            }

            if (!_responseTable.TryAdd(seqNum, (Callback, typeof(TResult))))
            {
                task = default;
                return false;
            }

            task = taskCompletionSource.Task;
            return true;
        }

        #endregion

        #region Serialization

        private void SerializeMethod(BinaryWriter writer, MethodInfo method)
        {
            writer.Write(method.IsGenericMethod);
            writer.Write(method.DeclaringType.AssemblyQualifiedName);
            writer.Write(method.Name);

            var arguments = method.GetParameters();

            writer.Write(arguments.Length);

            foreach (var argument in arguments)
            {
                writer.Write(argument.ParameterType.AssemblyQualifiedName);
            }

            if (method.IsGenericMethod)
            {
                var genericArguments = method.GetGenericArguments();

                writer.Write(genericArguments.Length);

                foreach (var genericArgument in genericArguments)
                {
                    writer.Write(genericArgument.AssemblyQualifiedName);
                }
            }
        }

        private MethodInfo DeserializeMethod(BinaryReader reader)
        {
            var isGenericMethod = reader.ReadBoolean();

            var declaringType = LoadTypeIgnoringVersion(reader.ReadString());
            var methodName = reader.ReadString();

            var argumentsLengh = reader.ReadInt32();
            var arguments = new Type[argumentsLengh];

            for (var i = 0; i < arguments.Length; i++)
            {
                arguments[i] = LoadTypeIgnoringVersion(reader.ReadString());
            }

            var candidates = declaringType.GetMethods().Where(p => p.Name == methodName);

            if (isGenericMethod)
            {
                var genericArgumentsLength = reader.ReadInt32();
                var genericArguments = new Type[genericArgumentsLength];

                for (var i = 0; i < genericArguments.Length; i++)
                {
                    genericArguments[i] = LoadTypeIgnoringVersion(reader.ReadString());
                }

                candidates = candidates.Where(p => p.IsGenericMethodDefinition && p.GetGenericArguments().Length == genericArgumentsLength)
                                       .Select(p => p.MakeGenericMethod(genericArguments));
            }

            candidates = candidates.Where(p => p.GetParameters().Select(q => q.ParameterType).SequenceEqual(arguments));

            if (candidates.Count() != 1)
            {
                if (candidates.Count() > 1)
                {
                    throw new Exception("Possible method missmatch.");
                }

                throw new Exception("Method not found");
            }

            var result = candidates.First();

            if (result.IsGenericMethodDefinition)
            {
                throw new Exception("Specified method contains unresolved generic arguments");
            }

            return result;
        }

        private void Serialize(BinaryWriter writer, IEnumerable<(object obj, Type objType)> objs)
        {
            writer.Write(objs?.Count() ?? 0);

            if (objs != null)
            {
                foreach (var (obj, objType) in objs)
                {
                    Serialize(writer, obj, objType);
                }
            }
        }

        private IEnumerable<object> Deserialize(BinaryReader reader, ParameterInfo[] parameterInfos)
        {
            var objectCount = reader.ReadInt32();
            for (var i = 0; i < objectCount; i++)
            {
                if (parameterInfos != null && i >= parameterInfos.Length)
                {
                    yield break;// TODO
                }

                yield return Deserialize(reader, parameterInfos?[i].ParameterType);
            }
        }

        private void Serialize(BinaryWriter writer, object obj, Type objType)
        {
            Debug.Assert(obj == null && (objType.CanContainNull() || objType == typeof(void)) || obj != null && objType.IsAssignableFrom(obj.GetType()));

            switch (obj)
            {
                case null:
                    writer.Write((byte)TypeCode.Null);
                    break;

                case bool value:
                    writer.Write(value ? (byte)TypeCode.True : (byte)TypeCode.False);
                    break;

                case byte value:
                    writer.Write((byte)TypeCode.Byte);
                    writer.Write(value);
                    break;

                case sbyte value:
                    writer.Write((byte)TypeCode.SByte);
                    writer.Write(value);
                    break;

                case short value:
                    writer.Write((byte)TypeCode.Int16);
                    writer.Write(value);
                    break;

                case ushort value:
                    writer.Write((byte)TypeCode.UInt16);
                    writer.Write(value);
                    break;

                case char value:
                    writer.Write((byte)TypeCode.Char);
                    writer.Write(value);
                    break;

                case int value:
                    writer.Write((byte)TypeCode.Int32);
                    writer.Write(value);
                    break;

                case uint value:
                    writer.Write((byte)TypeCode.UInt32);
                    writer.Write(value);
                    break;

                case long value:
                    writer.Write((byte)TypeCode.Int64);
                    writer.Write(value);
                    break;

                case ulong value:
                    writer.Write((byte)TypeCode.UInt64);
                    writer.Write(value);
                    break;

                case float value:
                    writer.Write((byte)TypeCode.Single);
                    writer.Write(value);
                    break;

                case double value:
                    writer.Write((byte)TypeCode.Double);
                    writer.Write(value);
                    break;

                case decimal value:
                    writer.Write((byte)TypeCode.Decimal);
                    writer.Write(value);
                    break;

                case string value:
                    writer.Write((byte)TypeCode.String);
                    writer.Write(value);
                    break;

                case Type type:
                    writer.Write((byte)TypeCode.Type);
                    WriteType(writer, type);
                    break;

                case byte[] value:
                    writer.Write((byte)TypeCode.ByteArray);
                    writer.Write(value.Length);
                    writer.Write(value);
                    break;

                case IProxyInternal proxy:
                    writer.Write((byte)TypeCode.Proxy);
                    SerializeProxy(writer, proxy);
                    break;

                case CancellationToken cancellationToken:
                    writer.Write((byte)TypeCode.CancellationToken);
                    break;

                case object _ when (objType.IsInterface && !objType.IsSerializable):
                    var createdProxy = CreateProxy(obj.GetType(), obj, ownsInstance: false);
                    writer.Write((byte)TypeCode.Proxy);
                    SerializeProxy(writer, createdProxy);
                    break;

                default:
                    writer.Write((byte)TypeCode.Other);
                    writer.Flush();
                    GetBinaryFormatter().Serialize(writer.BaseStream, obj);
                    break;
            }
        }

        private object Deserialize(BinaryReader reader, Type expectedType)
        {
            var typeCode = (TypeCode)reader.ReadByte();

            switch (typeCode)
            {
                case TypeCode.Null:
                    return null;

                case TypeCode.False:
                    return false;

                case TypeCode.True:
                    return true;

                case TypeCode.Byte:
                    return reader.ReadByte();

                case TypeCode.SByte:
                    return reader.ReadSByte();

                case TypeCode.Int16:
                    return reader.ReadInt16();

                case TypeCode.UInt16:
                    return reader.ReadUInt16();

                case TypeCode.Char:
                    return reader.ReadChar();

                case TypeCode.Int32:
                    return reader.ReadInt32();

                case TypeCode.UInt32:
                    return reader.ReadUInt32();

                case TypeCode.Int64:
                    return reader.ReadInt64();

                case TypeCode.UInt64:
                    return reader.ReadUInt64();

                case TypeCode.Single:
                    return reader.ReadSingle();

                case TypeCode.Double:
                    return reader.ReadDouble();

                case TypeCode.Decimal:
                    return reader.ReadDecimal();

                case TypeCode.String:
                    return reader.ReadString();

                case TypeCode.Type:
                    return ReadType(reader);

                case TypeCode.ByteArray:
                    var length = reader.ReadInt32();
                    return reader.ReadBytes(length);

                case TypeCode.Proxy:
                    return DeserializeProxy(reader, expectedType);

                case TypeCode.CancellationToken:
                    return CancellationToken.None; // TODO: Cancellation token support

                case TypeCode.Other:
                    return GetBinaryFormatter().Deserialize(reader.BaseStream);

                default:
                    throw new FormatException("Unknown type code.");
            }
        }

        private void SerializeProxy(BinaryWriter writer, IProxyInternal proxy)
        {
            if (proxy.LocalInstance != null)
            {
                proxy = RegisterLocalProxy(proxy);

                writer.Write((byte)ProxyOwner.Remote); // We own the proxy, but for the remote end, this is a remote proxy.
            }
            else
            {
                writer.Write((byte)ProxyOwner.Local);
            }

            var proxyType = proxy.RemoteType;
            writer.Write(proxyType.AssemblyQualifiedName);
            writer.Write((proxy.LocalInstance?.GetType() ?? proxyType).AssemblyQualifiedName);
            writer.Write(proxy.Id);
        }

        private object DeserializeProxy(BinaryReader reader, Type expectedType)
        {
            var proxyOwner = (ProxyOwner)reader.ReadByte();
            var proxyType = LoadTypeIgnoringVersion(reader.ReadString());
            var actualType = LoadTypeIgnoringVersion(reader.ReadString());
            var proxyId = reader.ReadInt32();

            if (proxyOwner == ProxyOwner.Remote)
            {
                var type = typeof(Proxy<>).MakeGenericType(proxyType);
                var proxy = (IProxyInternal)Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, new object[]
                {
                            this,
                            proxyId,
                            actualType
                }, null);

                if (expectedType == null || expectedType.GetInterfaces().Contains(typeof(IProxy)))
                {
                    return proxy;
                }

                return CreateTransparentProxy(expectedType, proxy);
            }
            else
            {
                if (!TryGetProxyById(proxyId, out var proxy))
                {
                    throw new Exception("Proxy not found.");
                }

                if (expectedType != null && expectedType.IsInstanceOfType(proxy.LocalInstance))
                    return proxy.LocalInstance;

                return proxy;
            }
        }

        private static IProxyInternal CreateTransparentProxy(Type expectedType, IProxyInternal proxy)
        {
            var transparentProxyTypeDefinition = typeof(TransparentProxy<,>);
            var transparentProxyType = transparentProxyTypeDefinition.MakeGenericType(proxy.RemoteType, expectedType);

            var createMethod = transparentProxyType.GetMethod(
                nameof(TransparentProxy<object, object>.Create),
                BindingFlags.Static | BindingFlags.NonPublic,
                Type.DefaultBinder,
                new[] { typeof(IProxyInternal) },
                modifiers: null);

            Debug.Assert(createMethod.ReturnType == expectedType);

            return (IProxyInternal)createMethod.Invoke(obj: null, new[] { proxy });
        }

        private void WriteType(BinaryWriter writer, Type type)
        {
            writer.Write(type.AssemblyQualifiedName);
        }

        private Type ReadType(BinaryReader reader)
        {
            var assemblyQualifiedName = reader.ReadString();
            return LoadTypeIgnoringVersion(assemblyQualifiedName);
        }

        private BinaryFormatter GetBinaryFormatter()
        {
            var selector = new SurrogateSelector();

            return new BinaryFormatter(selector, context: default);
        }

        private static Type LoadTypeIgnoringVersion(string assemblyQualifiedName)
        {
            return Type.GetType(assemblyQualifiedName, assemblyName => { assemblyName.Version = null; return Assembly.Load(assemblyName); }, null);
        }

        private enum TypeCode : byte
        {
            Other,
            Null,
            False,
            True,
            Byte,
            SByte,
            Int16,
            UInt16,
            Char,
            Int32,
            UInt32,
            Int64,
            UInt64,
            Single,
            Double,
            Decimal,
            String,
            Type,
            ByteArray,
            CancellationToken,
            Proxy
        }

        private enum ProxyOwner : byte
        {
            Local,
            Remote
        }

        #endregion

        private enum MessageType : byte
        {
            MethodCall,
            ReturnValue,
            ReturnException,
            Activation,
            Deactivation
        }
    }
}
