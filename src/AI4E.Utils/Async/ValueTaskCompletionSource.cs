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
using Microsoft.Extensions.ObjectPool;

namespace AI4E.Utils.Async
{
    /// <summary>
    /// Represents the producer side of of a <see cref="ValueTask{TResult}"/>
    /// providing access to the consumer side with through the <see cref="Task"/> property.
    /// </summary>
    /// <typeparam name="T">The type of result value.</typeparam>
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

        /// <summary>
        /// Gets a <see cref="ValueTask{TResult}"/> created by the <see cref="ValueTaskCompletionSource{T}"/>.
        /// </summary>
        public ValueTask<T> Task { get; }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetCanceled()
        {
            return TrySetCanceled(cancellation: default);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <param name="cancellation">A <see cref="CancellationToken"/>.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetCanceled(CancellationToken cancellation)
        {
            return _source?.TryNotifyCompletion(cancellation, _token) ?? false;
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> that caused the task to fail.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return _source?.TryNotifyCompletion(exception, _token) ?? false;
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exceptions">The collection of<see cref="Exception"/>s that caused the task to fail.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
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

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> to the <c>CompletedSuccessfully</c> state.
        /// </summary>
        /// <param name="result">The result value to bind to the <see cref="ValueTask{TResult}"/>.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetResult(T result)
        {
            return _source?.TryNotifyCompletion(result, _token) ?? false;
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetCanceled()
        {
            if (!TrySetCanceled())
            {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <param name="cancellation">The <see cref="CancellationToken"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetCanceled(CancellationToken cancellation)
        {
            if (!TrySetCanceled(cancellation))
            {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> that caused the task to fail.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetException(Exception exception)
        {
            if (!TrySetException(exception))
            {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exceptions">The collection of<see cref="Exception"/>s that caused the task to fail.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
        public void SetException(IEnumerable<Exception> exceptions)
        {
            if (!TrySetException(exceptions))
            {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> to the <c>CompletedSuccessfully</c> state.
        /// </summary>
        /// <param name="result">The result value to bind to the <see cref="ValueTask{TResult}"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask{TResult}"/> is already completed.
        /// </exception>
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

        /// <summary>
        /// Creates a new <see cref="ValueTaskCompletionSource{T}"/>.
        /// </summary>
        /// <returns>The created <see cref="ValueTaskCompletionSource{T}"/>.</returns>
        public static ValueTaskCompletionSource<T> Create()
        {
            var source = ValueTaskSource<T>.Allocate();
            return new ValueTaskCompletionSource<T>(source);
        }

        /// <inheritdoc/>
        public bool Equals(ValueTaskCompletionSource<T> other)
        {
            return _source == other._source && _token == other._token;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ValueTaskCompletionSource<T> valueTaskCompletionSource && Equals(valueTaskCompletionSource);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (_source, _token).GetHashCode();
        }

        /// <summary>
        /// Gets a boolean value indicating whether two <see cref="ValueTaskCompletionSource{T}"/> are equal.
        /// </summary>
        /// <param name="left">The first <see cref="ValueTaskCompletionSource{T}"/>.</param>
        /// <param name="right">The second <see cref="ValueTaskCompletionSource{T}"/>.</param>
        /// <returns>True if <paramref name="left"/> equals <paramref name="right"/>, false otherwise.</returns>
        public static bool operator ==(in ValueTaskCompletionSource<T> left, in ValueTaskCompletionSource<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Gets a boolean value indicating whether two <see cref="ValueTaskCompletionSource{T}"/> are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="ValueTaskCompletionSource{T}"/>.</param>
        /// <param name="right">The second <see cref="ValueTaskCompletionSource{T}"/>.</param>
        /// <returns>True if <paramref name="left"/> does not equal <paramref name="right"/>, false otherwise.</returns>
        public static bool operator !=(in ValueTaskCompletionSource<T> left, in ValueTaskCompletionSource<T> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Represents the producer side of of a <see cref="ValueTask"/>
    /// providing access to the consumer side with through the <see cref="Task"/> property.
    /// </summary>
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

        /// <summary>
        /// Gets a <see cref="ValueTask"/> created by the <see cref="ValueTaskCompletionSource"/>.
        /// </summary>
        public ValueTask Task { get; }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetCanceled()
        {
            return TrySetCanceled(cancellation: default);
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <param name="cancellation">A <see cref="CancellationToken"/>.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetCanceled(CancellationToken cancellation)
        {
            return _source?.TryNotifyCompletion(cancellation, _token) ?? false;
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> that caused the task to fail.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return _source?.TryNotifyCompletion(exception, _token) ?? false;
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exceptions">The collection of<see cref="Exception"/>s that caused the task to fail.</param>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
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

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask"/> to the <c>CompletedSuccessfully</c> state.
        /// </summary>
        /// <returns>A boolean value indicating whether the operation was successful.</returns>
        public bool TrySetResult()
        {
            return _source?.TryNotifyCompletion(0, _token) ?? false;
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask"/> is already completed.
        /// </exception>
        public void SetCanceled()
        {
            if (!TrySetCanceled())
            {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask"/> to the <c>Canceled</c> state.
        /// </summary>
        /// <param name="cancellation">The <see cref="CancellationToken"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask"/> is already completed.
        /// </exception>
        public void SetCanceled(CancellationToken cancellation)
        {
            if (!TrySetCanceled(cancellation))
            {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> that caused the task to fail.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask"/> is already completed.
        /// </exception>
        public void SetException(Exception exception)
        {
            if (!TrySetException(exception))
            {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask"/> to the <c>Faulted</c> state.
        /// </summary>
        /// <param name="exceptions">The collection of<see cref="Exception"/>s that caused the task to fail.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask"/> is already completed.
        /// </exception>
        public void SetException(IEnumerable<Exception> exceptions)
        {
            if (!TrySetException(exceptions))
            {
                ThrowAlreadyCompleted();
            }
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask"/> to the <c>CompletedSuccessfully</c> state.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ValueTask"/> is already completed.
        /// </exception>
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

        /// <summary>
        /// Creates a new <see cref="ValueTaskCompletionSource"/>.
        /// </summary>
        /// <returns>The created <see cref="ValueTaskCompletionSource"/>.</returns>
        public static ValueTaskCompletionSource Create()
        {
            var source = ValueTaskSource<byte>.Allocate();
            return new ValueTaskCompletionSource(source);
        }

        /// <inheritdoc/>
        public bool Equals(ValueTaskCompletionSource other)
        {
            return _source == other._source && _token == other._token;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is ValueTaskCompletionSource valueTaskCompletionSource && Equals(valueTaskCompletionSource);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (_source, _token).GetHashCode();
        }

        /// <summary>
        /// Gets a boolean value indicating whether two <see cref="ValueTaskCompletionSource"/> are equal.
        /// </summary>
        /// <param name="left">The first <see cref="ValueTaskCompletionSource"/>.</param>
        /// <param name="right">The second <see cref="ValueTaskCompletionSource"/>.</param>
        /// <returns>True if <paramref name="left"/> equals <paramref name="right"/>, false otherwise.</returns>
        public static bool operator ==(in ValueTaskCompletionSource left, in ValueTaskCompletionSource right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Gets a boolean value indicating whether two <see cref="ValueTaskCompletionSource"/> are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="ValueTaskCompletionSource"/>.</param>
        /// <param name="right">The second <see cref="ValueTaskCompletionSource"/>.</param>
        /// <returns>True if <paramref name="left"/> does not equal <paramref name="right"/>, false otherwise.</returns>
        public static bool operator !=(in ValueTaskCompletionSource left, in ValueTaskCompletionSource right)
        {
            return !left.Equals(right);
        }
    }

    // Based on: http://tooslowexception.com/implementing-custom-ivaluetasksource-async-without-allocations/
    internal sealed class ValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        /// Sentinel object used to indicate that the operation has completed prior to OnCompleted being called.
        private static readonly Action<object> _callbackCompleted = _ => { Debug.Assert(false, "Should not be invoked"); };

        #region Pooling

        static ValueTaskSource()
        {
            _pool = new DefaultObjectPool<ValueTaskSource<T>>(ValueTaskSourcePooledObjectPolicy.Instance);
            _synchronizationContextPostStatePool = new DefaultObjectPool<SynchronizationContextPostState>(new DefaultPooledObjectPolicy<SynchronizationContextPostState>());
            _executionContextRunStatePool = new DefaultObjectPool<ExecutionContextRunState>(new DefaultPooledObjectPolicy<ExecutionContextRunState>());
        }

        private static readonly ObjectPool<ValueTaskSource<T>> _pool;
        private static readonly ObjectPool<SynchronizationContextPostState> _synchronizationContextPostStatePool;
        private static readonly ObjectPool<ExecutionContextRunState> _executionContextRunStatePool;

        private sealed class ValueTaskSourcePooledObjectPolicy : PooledObjectPolicy<ValueTaskSource<T>>
        {
            public static ValueTaskSourcePooledObjectPolicy Instance { get; } = new ValueTaskSourcePooledObjectPolicy();

            private ValueTaskSourcePooledObjectPolicy() { }

            public override ValueTaskSource<T> Create()
            {
                return new ValueTaskSource<T>();
            }

            public override bool Return(ValueTaskSource<T> obj)
            {
                return !obj.Exhausted;
            }
        }

        #endregion

        private State _state;

        internal bool Exhausted { get; private set; }
        internal short Token { get; private set; }

        internal static ValueTaskSource<T> Allocate()
        {
            var result = _pool.Get();
            Debug.Assert(!result.Exhausted);
            Debug.Assert(result._state._continuation == default);
            Debug.Assert(EqualityComparer<T>.Default.Equals(result._state._result, default));
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

                    var executionContextRunState = _executionContextRunStatePool.Get();
                    executionContextRunState.ValueTaskSource = this;
                    executionContextRunState.PreviousContinuation = previousContinuation;
                    executionContextRunState.State = _state._continuationState;

                    static void ExecutionContextCallback(object runState)
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

            var exception = _state._exception;

            if (exception == null)
            {
                return ValueTaskSourceStatus.Succeeded;
            }

            if (exception is OperationCanceledException)
            {
                return ValueTaskSourceStatus.Canceled;
            }

            return ValueTaskSourceStatus.Faulted;
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
            var previousContinuation = Interlocked.CompareExchange(ref _state._continuation, continuation, null);
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

            // TODO: Should this block until the result is available?

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
                    var synchronizationContextPostState = _synchronizationContextPostStatePool.Get();
                    synchronizationContextPostState.Continuation = continuation;
                    synchronizationContextPostState.State = state;

                    static void PostCallback(object s)
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
