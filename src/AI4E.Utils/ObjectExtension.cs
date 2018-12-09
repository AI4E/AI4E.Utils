using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AI4E.Utils.Async;

namespace AI4E.Utils
{
    public static class ObjectExtension
    {
        public static void DisposeIfDisposable(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public static Task DisposeIfDisposableAsync(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (obj is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return Task.CompletedTask;
        }

        public static IEnumerable<object> Yield(this object obj)
        {
            yield return obj;
        }

        public static IEnumerable<T> Yield<T>(this T t)
        {
            yield return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Assert<T>(this T t, Func<T, bool> assertion)
        {
            Debug.Assert(assertion(t));

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Assert<T>(this T t, Func<T, bool> precondition, Func<T, bool> assertion)
        {
            if (precondition(t))
            {
                Debug.Assert(assertion(t));
            }

            return t;
        }

        public static async Task<T> AssertAsync<T>(this Task<T> task, Func<T, bool> assertion)
        {
            return (await task).Assert(assertion);
        }

        public static async Task<T> AssertAsync<T>(this Task<T> task, Func<T, bool> precondition, Func<T, bool> assertion)
        {
            return (await task).Assert(precondition, assertion);
        }
    }
}