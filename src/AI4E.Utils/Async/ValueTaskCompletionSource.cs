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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace AI4E.Utils.Async
{
    public readonly struct ValueTaskCompletionSource<T> : IEquatable<ValueTaskCompletionSource<T>>
    {
        private readonly ValueTaskSource<T> _source;
        private readonly short _token;

        private ValueTaskCompletionSource(ValueTaskSource<T> source)
        {
            Debug.Assert(source != null);
            Debug.Assert(!source.Exhausted);

            var token = source.Token;

            _source = source;
            _token = token;
            Task = new ValueTask<T>(source, token);
        }

        public ValueTask<T> Task { get; }

        public bool TrySetCanceled()
        {
            return TrySetCanceled(cancellation: default);
        }

        public bool TrySetCanceled(CancellationToken cancellation)
        {
            return _source?.TryNotifyCompletion(cancellation, _token) ?? false;
        }

        public bool TrySetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return _source?.TryNotifyCompletion(exception, _token) ?? false;
        }

        public bool TrySetException(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null)
                throw new ArgumentNullException(nameof(exceptions));

            var exception = exceptions.FirstOrDefault();

            if (exception == null)
            {
                if (!exceptions.Any())
                    throw new ArgumentException("The collection must not be empty.", nameof(exceptions));

                throw new ArgumentException("The collection must not contain null entries.", nameof(exceptions));
            }

            return TrySetException(exception);
        }

        public bool TrySetResult(T result)
        {
            return _source?.TryNotifyCompletion(result, _token) ?? false;
        }

        public void SetCanceled()
        {
            if (!TrySetCanceled())
            {
                ThrowAlreadyCompleted();
            }
        }

        public void SetCanceled(CancellationToken cancellation)
        {
            if (!TrySetCanceled(cancellation))
            {
                ThrowAlreadyCompleted();
            }
        }

        public void SetException(Exception exception)
        {
            if (!TrySetException(exception))
            {
                ThrowAlreadyCompleted();
            }
        }

        public void SetException(IEnumerable<Exception> exceptions)
        {
            if (!TrySetException(exceptions))
            {
                ThrowAlreadyCompleted();
            }
        }

        public void SetResult(T result)
        {
            if (!TrySetResult(result))
            {
                ThrowAlreadyCompleted();
            }
        }

        private static void ThrowAlreadyCompleted()
        {
            throw new InvalidOperationException("An attempt was made to transition a value task to a final state when it had already completed");
        }

        public static ValueTaskCompletionSource<T> Create()
        {
            var source = ValueTaskSource<T>.Allocate();
            return new ValueTaskCompletionSource<T>(source);
        }

        public bool Equals(ValueTaskCompletionSource<T> other)
        {
            return _source == other._source && _token == other._token;
        }

        public override bool Equals(object obj)
        {
            return obj is ValueTaskCompletionSource<T> valueTaskCompletionSource && Equals(valueTaskCompletionSource);
        }

        public override int GetHashCode()
        {
            return (_source?.GetHashCode() ?? 0) * 34556421 + _token.GetHashCode();
        }

        public static bool operator ==(in ValueTaskCompletionSource<T> left, in ValueTaskCompletionSource<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in ValueTaskCompletionSource<T> left, in ValueTaskCompletionSource<T> right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct ValueTaskCompletionSource : IEquatable<ValueTaskCompletionSource>
    {
        private readonly ValueTaskSource<byte> _source;
        private readonly short _token;

        private ValueTaskCompletionSource(ValueTaskSource<byte> source)
        {
            Debug.Assert(source != null);
            Debug.Assert(!source.Exhausted);

            var token = source.Token;

            _source = source;
            _token = token;
            Task = new ValueTask(source, token);
        }

        public ValueTask Task { get; }

        public bool TrySetCanceled()
        {
            return TrySetCanceled(cancellation: default);
        }

        public bool TrySetCanceled(CancellationToken cancellation)
        {
            return _source?.TryNotifyCompletion(cancellation, _token) ?? false;
        }

        public bool TrySetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return _source?.TryNotifyCompletion(exception, _token) ?? false;
        }

        public bool TrySetException(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null)
                throw new ArgumentNullException(nameof(exceptions));

            var exception = exceptions.FirstOrDefault();

            if (exception == null)
            {
                if (!exceptions.Any())
                    throw new ArgumentException("The collection must not be empty.", nameof(exceptions));

                throw new ArgumentException("The collection must not contain null entries.", nameof(exceptions));
            }

            return TrySetException(exception);
        }

        public bool TrySetResult()
        {
            return _source?.TryNotifyCompletion(0, _token) ?? false;
        }

        public void SetCanceled()
        {
            if (!TrySetCanceled())
            {
                ThrowAlreadyCompleted();
            }
        }

        public void SetCanceled(CancellationToken cancellation)
        {
            if (!TrySetCanceled(cancellation))
            {
                ThrowAlreadyCompleted();
            }
        }

        public void SetException(Exception exception)
        {
            if (!TrySetException(exception))
            {
                ThrowAlreadyCompleted();
            }
        }

        public void SetException(IEnumerable<Exception> exceptions)
        {
            if (!TrySetException(exceptions))
            {
                ThrowAlreadyCompleted();
            }
        }

        public void SetResult()
        {
            if (!TrySetResult())
            {
                ThrowAlreadyCompleted();
            }
        }

        private static void ThrowAlreadyCompleted()
        {
            throw new InvalidOperationException("An attempt was made to transition a value task to a final state when it had already completed");
        }

        public static ValueTaskCompletionSource Create()
        {
            var source = ValueTaskSource<byte>.Allocate();
            return new ValueTaskCompletionSource(source);
        }

        public bool Equals(ValueTaskCompletionSource other)
        {
            return _source == other._source && _token == other._token;
        }

        public override bool Equals(object obj)
        {
            return obj is ValueTaskCompletionSource valueTaskCompletionSource && Equals(valueTaskCompletionSource);
        }

        public override int GetHashCode()
        {
            return (_source?.GetHashCode() ?? 0) * 34556421 + _token.GetHashCode();
        }

        public static bool operator ==(in ValueTaskCompletionSource left, in ValueTaskCompletionSource right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in ValueTaskCompletionSource left, in ValueTaskCompletionSource right)
        {
            return !left.Equals(right);
        }
    }

    // Based on: http://tooslowexception.com/implementing-custom-ivaluetasksource-async-without-allocations/
    internal sealed class ValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        #region Pooling

        private static readonly ObjectPool<ValueTaskSource<T>> _pool = CreatePool();
        private static readonly ObjectPool<SynchronizationContextPostState> _synchronizationContextPostStatePool = ObjectPool.Create<SynchronizationContextPostState>();
        private static readonly ObjectPool<ExecutionContextRunState> _executionContextRunStatePool = ObjectPool.Create<ExecutionContextRunState>();

        private static ObjectPool<ValueTaskSource<T>> CreatePool()
        {
            return ObjectPool.Create<ValueTaskSource<T>>(isReusable: p => !p.Exhausted, size: ObjectPool.DefaultSize);
        }

        #endregion

        /// Sentinel object used to indicate that the operation has completed prior to OnCompleted being called.
        private static readonly Action<object> _callbackCompleted = _ => { Debug.Assert(false, "Should not be invoked"); };

        private State _state;

        internal bool Exhausted { get; private set; }
        internal short Token { get; private set; }

        internal static ValueTaskSource<T> Allocate()
        {
            var result = _pool.Rent();
            Debug.Assert(!result.Exhausted);
            Debug.Assert(result._state._continuation == default);
            Debug.Assert(result._state._result == default);
            Debug.Assert(result._state._completing == default);
            Debug.Assert(result._state._completed == default);
            Debug.Assert(result._state._exception == default);
            Debug.Assert(result._state._continuationState == default);
            Debug.Assert(result._state._executionContext == default);
            Debug.Assert(result._state._scheduler == default);
            return result;
        }

        internal bool TryNotifyCompletion(T result, short token)
        {
            if (token != Token || !TrySetCompleted(exception: null, result))
            {
                return false;
            }

            ExecuteContinuation();

            return true;
        }

        internal bool TryNotifyCompletion(CancellationToken cancellation, short token)
        {
            if (token != Token || !TrySetCompleted(new OperationCanceledException(cancellation), result: default))
            {
                return false;
            }

            ExecuteContinuation();
            return true;
        }

        internal bool TryNotifyCompletion(Exception exception, short token)
        {
            Debug.Assert(exception != null);

            if (token != Token || !TrySetCompleted(exception, result: default))
            {
                return false;
            }

            ExecuteContinuation();

            return true;
        }

        private bool TrySetCompleted(Exception exception, T result)
        {
            if (_state._completing != 0)
            {
                return false;
            }

            var completing = Interlocked.Exchange(ref _state._completing, 1);

            if (completing != 0)
            {
                return false;
            }

            _state._exception = exception;
            _state._result = result;
            Volatile.Write(ref _state._completed, true);

            return true;
        }

        private void ExecuteContinuation()
        {
            // Mark operation as completed
            var previousContinuation = Interlocked.CompareExchange(ref _state._continuation, _callbackCompleted, null);
            if (previousContinuation != null)
            {
                // Async work completed, continue with... continuation
                var executionContext = _state._executionContext;
                if (executionContext == null)
                {
                    InvokeContinuation(previousContinuation, _state._continuationState, forceAsync: false);
                }
                else
                {
                    // This case should be relatively rare, as the async Task/ValueTask method builders
                    // use the awaiter's UnsafeOnCompleted, so this will only happen with code that
                    // explicitly uses the awaiter's OnCompleted instead.
                    _state._executionContext = null;

                    var executionContextRunState = _executionContextRunStatePool.Rent();
                    executionContextRunState.ValueTaskSource = this;
                    executionContextRunState.PreviousContinuation = previousContinuation;
                    executionContextRunState.State = _state._continuationState;

                    void ExecutionContextCallback(object runState)
                    {
                        var t = (ExecutionContextRunState)runState;
                        try
                        {
                            t.ValueTaskSource.InvokeContinuation(t.PreviousContinuation, t.State, forceAsync: false);
                        }
                        finally
                        {
                            _executionContextRunStatePool.Return(t);
                        }
                    }

                    ExecutionContext.Run(executionContext, ExecutionContextCallback, executionContextRunState);
                }
            }
        }

        #region IValueTaskSource

        public ValueTaskSourceStatus GetStatus(short token)
        {
            if (token != Token)
            {
                ThrowMultipleContinuations();
            }

            if (!Volatile.Read(ref _state._completed))
            {
                return ValueTaskSourceStatus.Pending;
            }

            return _state._exception != null ? ValueTaskSourceStatus.Succeeded : ValueTaskSourceStatus.Faulted;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (token != Token)
            {
                ThrowMultipleContinuations();
            }

            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _state._executionContext = ExecutionContext.Capture();
            }

            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                var sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _state._scheduler = sc;
                }
                else
                {
                    var ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        _state._scheduler = ts;
                    }
                }
            }

            // Remember current state
            _state._continuationState = state;
            // Remember continuation to be executed on completed (if not already completed, in case of which
            // continuation will be set to CallbackCompleted)
            var previousContinuation = Interlocked.CompareExchange(ref continuation, continuation, null);
            if (previousContinuation != null)
            {
                if (!ReferenceEquals(previousContinuation, _callbackCompleted))
                    ThrowMultipleContinuations();

                // Lost the race condition and the operation has now already completed.
                // We need to invoke the continuation, but it must be asynchronously to
                // avoid a stack dive.  However, since all of the queueing mechanisms flow
                // ExecutionContext, and since we're still in the same context where we
                // captured it, we can just ignore the one we captured.
                _state._executionContext = null;
                _state._continuationState = null; // we have the state in "state"; no need for the one in UserToken
                InvokeContinuation(continuation, state, forceAsync: true);
            }
        }

        public T GetResult(short token)
        {
            if (token != Token)
            {
                ThrowMultipleContinuations();
            }

            var exception = _state._exception;
            var result = ResetAndReleaseOperation();

            if (exception != null)
            {
                var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);
                exceptionDispatchInfo.Throw();

                Debug.Assert(false);
                throw exception;
            }

            return result;
        }

        void IValueTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        #endregion

        private static void ThrowMultipleContinuations()
        {
            throw new InvalidOperationException("Multiple awaiters are not allowed");
        }

        private T ResetAndReleaseOperation()
        {
            var result = _state._result;
            if (Token == short.MaxValue)
            {
                Exhausted = true;
            }
            else
            {
                Token++;
                _state = new State();
            }
            _pool.Return(this);
            return result;
        }

        private void InvokeContinuation(Action<object> continuation, object state, bool forceAsync)
        {
            if (continuation == null)
                return;

            if (_state._scheduler != null)
            {
                if (_state._scheduler is SynchronizationContext synchronizationContext)
                {
                    var synchronizationContextPostState = _synchronizationContextPostStatePool.Rent();
                    synchronizationContextPostState.Continuation = continuation;
                    synchronizationContextPostState.State = state;

                    void PostCallback(object s)
                    {
                        var t = (SynchronizationContextPostState)s;
                        try
                        {
                            t.Continuation(t.State);
                        }
                        finally
                        {
                            _synchronizationContextPostStatePool.Return(t);
                        }
                    }

                    synchronizationContext.Post(PostCallback, synchronizationContextPostState);
                }
                else
                {
                    Debug.Assert(_state._scheduler is TaskScheduler, $"Expected TaskScheduler, got {_state._scheduler}");
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, (TaskScheduler)_state._scheduler);
                }
            }
            else if (forceAsync)
            {
                var threadPoolWorkItem = (WaitCallback)Delegate.CreateDelegate(typeof(WaitCallback), continuation.Target, continuation.Method);

                ThreadPool.QueueUserWorkItem(threadPoolWorkItem, state);
            }
            else
            {
                continuation(state);
            }
        }

        private sealed class SynchronizationContextPostState
        {
            public Action<object> Continuation { get; set; }
            public object State { get; set; }
        }

        private sealed class ExecutionContextRunState
        {
            public ValueTaskSource<T> ValueTaskSource { get; set; }
            public Action<object> PreviousContinuation { get; set; }
            public object State { get; set; }
        }

        private struct State
        {
            public Action<object> _continuation;
            public T _result;
            public int _completing;
            public bool _completed;
            public Exception _exception;
            public object _continuationState;
            public ExecutionContext _executionContext;
            public object _scheduler;
        }
    }
}
