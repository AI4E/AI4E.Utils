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
using System.Threading.Tasks;
using AI4E.Utils.Async;

namespace AI4E.Utils
{
    public static class ValueTaskCompletionSourceExtension
    {
        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask{TResult}"/> object
        /// into the <see cref="TaskStatus.Faulted"/> or <see cref="TaskStatus.Canceled"/> state,
        /// depending on the type of exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result value associated with the <see cref="ValueTaskCompletionSource{TResult}"/>.</typeparam>
        /// <param name="taskCompletionSource">The task completion source.</param>
        /// <param name="exception">The exception to bind to the <see cref="ValueTask{TResult}"/>.</param>
        /// <returns>True if the operation was succesful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any of <paramref name="taskCompletionSource"/> or <paramref name="exception"/> is <c>null</c>.
        /// </exception>
        public static bool TrySetExceptionOrCanceled<TResult>(this ValueTaskCompletionSource<TResult> taskCompletionSource, Exception exception)
        {
            if (taskCompletionSource == null)
                throw new ArgumentNullException(nameof(taskCompletionSource));

            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (exception is OperationCanceledException operationCanceledException)
            {
                var cancellationToken = operationCanceledException.CancellationToken;

                if (cancellationToken != default)
                {
                    return taskCompletionSource.TrySetCanceled(cancellationToken);
                }

                return taskCompletionSource.TrySetCanceled();
            }

            return taskCompletionSource.TrySetException(exception);
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask{TResult}"/> object
        /// into the <see cref="TaskStatus.Faulted"/> or <see cref="TaskStatus.Canceled"/> state,
        /// depending on the type of exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result value associated with the <see cref="ValueTaskCompletionSource{TResult}"/>.</typeparam>
        /// <param name="taskCompletionSource">The task completion source.</param>
        /// <param name="exception">The exception to bind to the <see cref="ValueTask{TResult}"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any of <paramref name="taskCompletionSource"/> or <paramref name="exception"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="ValueTask{TResult}"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>, <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        public static void SetExceptionOrCanceled<TResult>(this ValueTaskCompletionSource<TResult> taskCompletionSource, Exception exception)
        {
            if (!TrySetExceptionOrCanceled(taskCompletionSource, exception))
            {
                throw new InvalidOperationException("The underlying Task<TResult> is already in one of the three final states: RanToCompletion, Faulted, or Canceled.");
            }
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="ValueTask"/> object
        /// into the <see cref="TaskStatus.Faulted"/> or <see cref="TaskStatus.Canceled"/> state,
        /// depending on the type of exception.
        /// </summary>
        /// <param name="taskCompletionSource">The task completion source.</param>
        /// <param name="exception">The exception to bind to the <see cref="ValueTask"/>.</param>
        /// <returns>True if the operation was succesful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any of <paramref name="taskCompletionSource"/> or <paramref name="exception"/> is <c>null</c>.
        /// </exception>
        public static bool TrySetExceptionOrCanceled(this ValueTaskCompletionSource taskCompletionSource, Exception exception)
        {
            if (taskCompletionSource == null)
                throw new ArgumentNullException(nameof(taskCompletionSource));

            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (exception is OperationCanceledException operationCanceledException)
            {
                var cancellationToken = operationCanceledException.CancellationToken;

                if (cancellationToken != default)
                {
                    return taskCompletionSource.TrySetCanceled(cancellationToken);
                }

                return taskCompletionSource.TrySetCanceled();
            }

            return taskCompletionSource.TrySetException(exception);
        }

        /// <summary>
        /// Transitions the underlying <see cref="ValueTask"/> object
        /// into the <see cref="TaskStatus.Faulted"/> or <see cref="TaskStatus.Canceled"/> state,
        /// depending on the type of exception.
        /// </summary>
        /// <param name="taskCompletionSource">The task completion source.</param>
        /// <param name="exception">The exception to bind to the <see cref="ValueTask"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if any of <paramref name="taskCompletionSource"/> or <paramref name="exception"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="ValueTask"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>, <see cref="TaskStatus.Faulted"/>, or <see cref="TaskStatus.Canceled"/>.
        /// </exception>
        public static void SetExceptionOrCanceled(this ValueTaskCompletionSource taskCompletionSource, Exception exception)
        {
            if (!TrySetExceptionOrCanceled(taskCompletionSource, exception))
            {
                throw new InvalidOperationException("The underlying Task<TResult> is already in one of the three final states: RanToCompletion, Faulted, or Canceled.");
            }
        }
    }
}
