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
using System.Runtime.Serialization.Formatters;
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
        private static readonly MethodInfo _createProxyMethodDefinition =
            typeof(ProxyHost)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .SingleOrDefault(p => p.Name == nameof(CreateProxy) && p.IsGenericMethodDefinition && p.GetGenericArguments().Length == 1);

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
        private readonly Dictionary<int, IProxyInternal> _remoteProxies = new Dictionary<int, IProxyInternal>();
        private readonly object _remoteProxiesMutex = new object();
        private readonly object _cancellationTokenSourcesMutex = new object();
        private readonly Dictionary<int, Dictionary<int, CancellationTokenSource>> _cancellationTokenSources
            = new Dictionary<int, Dictionary<int, CancellationTokenSource>>();
        private static volatile ImmutableList<Type> _loadedRemoteTypes = ImmutableList<Type>.Empty;
        private static readonly HashSet<Type> _loadedTransparentProxyTypes = new HashSet<Type>();
        private static readonly object _loadedTransparentProxyTypesMutex = new object();
        private readonly AsyncDisposeHelper _disposeHelper;

        private int _nextSeqNum = 0;
        private int _nextLocalProxyId = 0;
        private int _nextRemoteProxyId = 0;

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

        internal async Task<IProxy<TRemote>> ActivateAsync<TRemote>(ActivationMode mode, object[] parameter, CancellationToken cancellation)
            where TRemote : class
        {
            int seqNum;
            Task<IProxy<TRemote>> result;

            var id = Interlocked.Increment(ref _nextRemoteProxyId);

            // Ids for remote proxies we create must carry a one in the MSB to prevent id conflicts.
            id = unchecked(id |-1);

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

                        writer.Write(id);
                        writer.Write((byte)mode);
                        WriteType(writer, typeof(TRemote));
                        Serialize(writer, parameter?.Select(p => (p, p?.GetType())), null);
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
            try
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
            catch (ObjectDisposedException) { }
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

            List<IProxyInternal> proxies;

            lock (_proxyLock)
            {
                proxies = _proxies.Values.ToList();
            }

            lock (_remoteProxiesMutex)
            {
                proxies.AddRange(_remoteProxies.Values);
            }

            var objectDisposedException = new ObjectDisposedException(GetType().FullName);

            // TODO: Parallelize this.
            foreach (var callback in _responseTable.Values.Select(p => p.callback))
            {
                callback.Invoke(MessageType.ReturnException, objectDisposedException);
            }

            await Task.WhenAll(proxies.Select(p => p.DisposeAsync())).HandleExceptionsAsync();

            _stream.Dispose();
        }

        #endregion

        #region Proxies

        private IProxyInternal RegisterLocalProxy(IProxyInternal proxy, int id)
        {
            try
            {
                using (_disposeHelper.GuardDisposal())
                {
                    lock (_proxyLock)
                    {
                        if (_proxyLookup.TryGetValue(proxy.LocalInstance, out var existing))
                        {
                            return existing;
                        }

                        proxy.Register(this, id, () => UnregisterLocalProxy(proxy));

                        _proxyLookup.Add(proxy.LocalInstance, proxy);
                        _proxies.Add(id, proxy);
                    }
                }
            }
            catch (OperationCanceledException) when (_disposeHelper.IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            return proxy;
        }

        private IProxyInternal RegisterLocalProxy(IProxyInternal proxy)
        {
            var id = Interlocked.Increment(ref _nextLocalProxyId);

            return RegisterLocalProxy(proxy, id);
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

        internal static IProxyInternal CreateProxy(Type remoteType, object instance, bool ownsInstance)
        {
            var createProxyMethod = _createProxyMethodDefinition.MakeGenericMethod(remoteType);
            return (IProxyInternal)createProxyMethod.Invoke(obj: null, new object[] { instance, ownsInstance });
        }

        internal static void AddTransparentProxyType(Type type)
        {
            lock (_loadedTransparentProxyTypesMutex)
            {
                _loadedTransparentProxyTypes.Add(type);
            }
        }

        internal static void AddLoadedRemoteType(Type remoteType)
        {
            _loadedRemoteTypes = _loadedRemoteTypes.Add(remoteType); // Volatile write op.
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

                    case MessageType.CancellationRequest:
                        ReceiveCancellationRequest(reader);
                        break;
                }
            }
        }

        private void ReceiveCancellationRequest(BinaryReader reader)
        {
            var corr = reader.ReadInt32();
            var cancellationTokenId = reader.ReadInt32();

            lock (_cancellationTokenSourcesMutex)
            {
                if (_cancellationTokenSources.TryGetValue(corr, out var cancellationTokenSources) &&
                   cancellationTokenSources.TryGetValue(cancellationTokenId, out var cancellationTokenSource))
                {
                    cancellationTokenSource.Cancel();
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
                var id = reader.ReadInt32();
                var mode = (ActivationMode)reader.ReadByte();
                var type = ReadType(reader);
                var parameter = Deserialize(reader, (ParameterInfo[])null, null).ToArray();
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

            var cancellationTokenSources = new Dictionary<int, CancellationTokenSource>();

            lock (_cancellationTokenSourcesMutex)
            {
                _cancellationTokenSources[seqNum] = cancellationTokenSources;
            }

            try
            {
                try
                {
                    var proxyId = reader.ReadInt32();
                    waitTask = reader.ReadBoolean();
                    method = DeserializeMethod(reader);

                    var arguments = Deserialize(reader, method.GetParameters(), cancellationTokenSources).ToArray();

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
            finally
            {
                lock (_cancellationTokenSourcesMutex)
                {
                    _cancellationTokenSources.Remove(seqNum);
                }
            }
        }

        private void ReceiveResult(MessageType messageType, BinaryReader reader)
        {
            var corr = reader.ReadInt32();

            if (_responseTable.TryRemove(corr, out var entry))
            {
                var value = Deserialize(reader, expectedType: entry.resultType, null);
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

                try
                {
                    await _stream.WriteAsync(_sendMessageLengthBuffer, offset: 0, count: _sendMessageLengthBuffer.Length);

                    await stream.CopyToAsync(_stream, bufferSize: 81920, cancellation);
                }
                catch (ObjectDisposedException)
                {
                    Dispose();
                    throw;
                }
            }
        }

        private async Task SendResult(
            int corrNum,
            object result,
            Type resultType,
            Exception exception,
            bool waitTask,
            CancellationToken cancellation)
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
                        Serialize(writer, exception, exception.GetType(), null);
                    }
                    else
                    {
                        Serialize(writer, result, resultType, null);
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
                parameters = methodCallExpression.Arguments.Select(p => p.Evaluate()).ToArray();
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
                var cancellationTokens = new List<CancellationToken>();

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
                        Serialize(writer, args.ElementWiseMerge(method.GetParameters(), (arg, param) => (arg, param.ParameterType)), cancellationTokens);
                        writer.Flush();
                    }
                }
                while (!TryGetResultTask(seqNum, out task));

                stream.Position = 0;

                var registrations = new List<CancellationTokenRegistration>(capacity: cancellationTokens.Count);
                var cancellations = new List<Task>();

                using (var cancellationOperationCancellation = new CancellationTokenSource())
                {
                    try
                    {
                        for (var id = 0; id < cancellationTokens.Count; id++)
                        {
                            var cancellationToken = cancellationTokens[id];
                            if (!cancellationToken.CanBeCanceled)
                            {
                                continue;
                            }

                            if (cancellationToken.IsCancellationRequested)
                            {
                                cancellations.Add(CancelMethodCallAsync(seqNum, id, cancellationOperationCancellation.Token));
                                continue;
                            }

                            var idCopy = id;
                            var registration = cancellationToken.Register(() => cancellations.Add(CancelMethodCallAsync(seqNum, idCopy, cancellationOperationCancellation.Token)));
                            try
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    cancellations.Add(CancelMethodCallAsync(seqNum, id, cancellationOperationCancellation.Token));
                                    registration.Dispose();
                                    continue;
                                }

                                registrations.Add(registration);
                            }
                            catch
                            {
                                registration.Dispose();
                                throw;
                            }
                        }

                        await SendAsync(stream, cancellation: default);

                        return await task;
                    }
                    finally
                    {
                        List<Exception> exceptions = null;

                        foreach (var registration in registrations)
                        {
                            try
                            {
                                registration.Dispose();
                            }
                            catch (Exception exc)
                            {
                                if (exceptions == null)
                                    exceptions = new List<Exception>();

                                exceptions.Add(exc);
                            }
                        }

                        cancellationOperationCancellation.Cancel();

                        if (cancellations.Any())
                        {
                            try
                            {
                                await Task.WhenAll(cancellations);
                            }
                            catch (Exception exc)
                            {
                                if (exceptions == null)
                                    exceptions = new List<Exception>();

                                exceptions.Add(exc);
                            }
                        }

                        if (exceptions != null)
                        {
                            if (exceptions.Count == 1)
                            {
                                throw exceptions[0];
                            }

                            throw new AggregateException(exceptions);
                        }
                    }
                }
            }
        }

        private async Task CancelMethodCallAsync(int corrNum, int cancellationTokenId, CancellationToken cancellation)
        {
            var delay = TimeSpan.FromMilliseconds(200);
            var maxDelay = TimeSpan.FromMilliseconds(1000);

            while (!cancellation.IsCancellationRequested)
            {
                var seqNum = Interlocked.Increment(ref _nextSeqNum);

                using (var stream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.Write((byte)MessageType.CancellationRequest);
                        writer.Write(seqNum);
                        writer.Write(corrNum);
                        writer.Write(cancellationTokenId);
                    }

                    stream.Position = 0;
                    await SendAsync(stream, cancellation: default);
                }

                try
                {
                    await Task.Delay(delay, cancellation);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    return;
                }

                delay += delay;

                if (delay > maxDelay)
                    delay = maxDelay;
            }
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

            try
            {
                using (_disposeHelper.GuardDisposal())
                {

                    if (!_responseTable.TryAdd(seqNum, (Callback, typeof(TResult))))
                    {
                        task = default;
                        return false;
                    }

                }
            }
            catch (OperationCanceledException) when (_disposeHelper.IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
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

        private void Serialize(BinaryWriter writer, IEnumerable<(object obj, Type objType)> objs, List<CancellationToken> cancellationTokens)
        {
            writer.Write(objs?.Count() ?? 0);

            if (objs != null)
            {
                foreach (var (obj, objType) in objs)
                {
                    Serialize(writer, obj, objType, cancellationTokens);
                }
            }
        }

        private IEnumerable<object> Deserialize(BinaryReader reader, ParameterInfo[] parameterInfos, Dictionary<int, CancellationTokenSource> cancellationTokenSources)
        {
            var objectCount = reader.ReadInt32();
            for (var i = 0; i < objectCount; i++)
            {
                if (parameterInfos != null && i >= parameterInfos.Length)
                {
                    yield break;// TODO
                }

                yield return Deserialize(reader, parameterInfos?[i].ParameterType, cancellationTokenSources);
            }
        }

        private void Serialize(BinaryWriter writer, object obj, Type objType, List<CancellationToken> cancellationTokens)
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
                    writer.Write(SerializeCancellationToken(cancellationTokens, cancellationToken));

                    break;

                case object _ when objType.IsInterface && !obj.GetType().IsSerializable && LocalProxyRegistered(obj, out var existingProxy):
                    writer.Write((byte)TypeCode.Proxy);
                    SerializeProxy(writer, existingProxy);
                    break;

                default:
                    writer.Write((byte)TypeCode.Other);
                    writer.Flush();
                    GetBinaryFormatter(null, cancellationTokens).Serialize(writer.BaseStream, obj);
                    break;
            }
        }

        private bool LocalProxyRegistered(object obj, out IProxyInternal proxy)
        {
            lock (_proxyLock)
            {
                return _proxyLookup.TryGetValue(obj, out proxy);
            }
        }

        private static int SerializeCancellationToken(List<CancellationToken> cancellationTokens, CancellationToken cancellationToken)
        {
            if (cancellationTokens == null || !cancellationToken.CanBeCanceled)
            {
                return -1;
            }

            cancellationTokens.Add(cancellationToken);
            return cancellationTokens.Count - 1;
        }

        private object Deserialize(BinaryReader reader, Type expectedType, Dictionary<int, CancellationTokenSource> cancellationTokenSources)
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
                    var id = reader.ReadInt32();
                    return DeserializeCancellationToken(id, cancellationTokenSources);

                case TypeCode.Other:
                    return GetBinaryFormatter(cancellationTokenSources, null).Deserialize(reader.BaseStream);

                default:
                    throw new FormatException("Unknown type code.");
            }
        }

        private static object DeserializeCancellationToken(int id, Dictionary<int, CancellationTokenSource> cancellationTokenSources)
        {
            if (id < 0 || cancellationTokenSources == null)
            {
                return CancellationToken.None;
            }

            var cancellationToken = cancellationTokenSources.GetOrAdd(id, _ => new CancellationTokenSource()).Token;
            return cancellationToken;
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

            return DeserializeProxy(expectedType, proxyOwner, proxyType, actualType, proxyId);
        }

        private object DeserializeProxy(
            Type expectedType,
            ProxyOwner proxyOwner,
            Type proxyType,
            Type actualType,
            int proxyId)
        {
            if (proxyOwner == ProxyOwner.Remote)
            {
                IProxyInternal proxy;

                lock (_remoteProxiesMutex)
                {
                    if (!_remoteProxies.TryGetValue(proxyId, out proxy))
                    {
                        proxy = null;
                    }
                }

                if (proxy == null)
                {
                    var type = typeof(Proxy<>).MakeGenericType(proxyType);
                    proxy = (IProxyInternal)Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, new object[]
                    {
                            this,
                            proxyId,
                            actualType
                    }, null);
                }

                try
                {
                    using (_disposeHelper.GuardDisposal())
                    {
                        lock (_remoteProxiesMutex)
                        {
                            if (_remoteProxies.TryGetValue(proxy.Id, out var p))
                            {
                                proxy = p;
                            }
                            else
                            {
                                _remoteProxies.Add(proxy.Id, proxy);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (_disposeHelper.IsDisposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }

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
            var transparentProxyTypeDefinition = typeof(TransparentProxy<>);
            var transparentProxyType = transparentProxyTypeDefinition.MakeGenericType(expectedType);

            var createMethod = transparentProxyType.GetMethod(
                nameof(TransparentProxy<object>.Create),
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

        private BinaryFormatter GetBinaryFormatter(
                Dictionary<int, CancellationTokenSource> cancellationTokenSources,
                List<CancellationToken> cancellationTokens)
        {
            var selector = new FallbackSurrogateSelector(this);

            HashSet<Type> transparentProxyTypes;

            lock (_loadedTransparentProxyTypesMutex)
            {
                transparentProxyTypes = _loadedTransparentProxyTypes.ToHashSet();
            }

            var proxySurrogate = new ProxySurrogate(this);
            foreach (var remoteType in _loadedRemoteTypes) // Volatile read op.
            {
                selector.AddSurrogate(typeof(Proxy<>).MakeGenericType(remoteType), new StreamingContext(), proxySurrogate);

                var interfaces = remoteType.GetInterfaces();

                foreach (var @interface in interfaces)
                {
                    transparentProxyTypes.Add(@interface);
                }
            }

            foreach (var transparentProxyType in transparentProxyTypes) // Volatile read op.
            {
                selector.AddSurrogate(transparentProxyType, new StreamingContext(), proxySurrogate);
            }

            var cancellationTokenSurrogate = new CancellationTokenSurrogate(this, cancellationTokenSources, cancellationTokens);

            selector.AddSurrogate(typeof(CancellationToken), new StreamingContext(), cancellationTokenSurrogate);

            return new BinaryFormatter(selector, context: default) { AssemblyFormat = FormatterAssemblyStyle.Simple };
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
            Deactivation,
            CancellationRequest
        }

        private sealed class ProxySurrogate : ISerializationSurrogate
        {
            private readonly ProxyHost _proxyHost;

            public ProxySurrogate(ProxyHost proxyHost)
            {
                Debug.Assert(proxyHost != null);
                _proxyHost = proxyHost;
            }

            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                if (!(obj is IProxyInternal proxy) && !_proxyHost.LocalProxyRegistered(obj, out proxy))
                {
                    throw new SerializationException("Type " + obj.GetType() + " cannot be serialized.");
                }

                var remoteType = proxy.RemoteType;
                byte proxyOwner;

                if (proxy.LocalInstance != null)
                {
                    proxy = _proxyHost.RegisterLocalProxy(proxy);

                    proxyOwner = (byte)ProxyOwner.Remote; // We own the proxy, but for the remote end, this is a remote proxy.
                }
                else
                {
                    proxyOwner = (byte)ProxyOwner.Local;
                }

                var expectedType = obj is IProxyInternal ? typeof(Proxy<>).MakeGenericType(remoteType) : remoteType;

                for (var current = obj.GetType(); current != typeof(object); current = current.BaseType)
                {
                    if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(TransparentProxy<>))
                    {
                        expectedType = current.GetGenericArguments()[0];
                    }
                }

                info.AddValue("proxyOwner", proxyOwner);
                info.AddValue("id", proxy.Id);
                info.AddValue("objectType", (proxy.LocalInstance?.GetType() ?? remoteType).AssemblyQualifiedName);
                info.AddValue("expectedType", expectedType.AssemblyQualifiedName);
                info.SetType(typeof(Proxy<>).MakeGenericType(remoteType));
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var remoteType = info.ObjectType.GetGenericArguments()[0];
                var proxyOwner = (ProxyOwner)info.GetByte("proxyOwner");
                var id = info.GetInt32("id");
                var objectType = LoadTypeIgnoringVersion(info.GetString("objectType"));
                var expectedType = LoadTypeIgnoringVersion(info.GetString("expectedType"));

                return _proxyHost.DeserializeProxy(expectedType, proxyOwner, remoteType, objectType, id);
            }
        }

        private sealed class CancellationTokenSurrogate : ISerializationSurrogate
        {
            private readonly ProxyHost _proxyHost;
            private readonly Dictionary<int, CancellationTokenSource> _cancellationTokenSources;
            private readonly List<CancellationToken> _cancellationTokens;

            public CancellationTokenSurrogate(
                ProxyHost proxyHost,
                Dictionary<int, CancellationTokenSource> cancellationTokenSources,
                List<CancellationToken> cancellationTokens)
            {
                Debug.Assert(proxyHost != null);

                _proxyHost = proxyHost;
                _cancellationTokenSources = cancellationTokenSources;
                _cancellationTokens = cancellationTokens;
            }

            public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                Debug.Assert(obj is CancellationToken);
                var id = SerializeCancellationToken(_cancellationTokens, (CancellationToken)obj);
                info.AddValue("id", id);
            }

            public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                var id = info.GetInt32("id");
                return DeserializeCancellationToken(id, _cancellationTokenSources);
            }
        }

        private sealed class FallbackSurrogateSelector : SurrogateSelector
        {
            private readonly ProxyHost _proxyHost;

            public FallbackSurrogateSelector(ProxyHost proxyHost)
            {
                Debug.Assert(proxyHost != null);
                _proxyHost = proxyHost;
            }

            public override ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
            {
                var surrogate = base.GetSurrogate(type, context, out selector);

                if (surrogate == null && !type.IsSerializable)
                {
                    return new ProxySurrogate(_proxyHost);
                }

                return surrogate;
            }
        }
    }

    internal enum ActivationMode : byte
    {
        Create,
        Load
    }
}
