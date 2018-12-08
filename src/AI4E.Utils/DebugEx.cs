using System.Diagnostics;

#if ASYNC_ENUMERABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
#endif

namespace AI4E.Utils
{
    public sealed class DebugEx
    {
        // condition is only checked if precondition mets.
        public static void Assert(bool precondition, bool condition)
        {
            // precondition => condition
            if (precondition)
            {
                Debug.Assert(condition);
            }
        }

#if ASYNC_ENUMERABLE

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IAsyncEnumerable<T> AssertEach<T>(this IAsyncEnumerable<T> asyncEnumerable, Func<T, bool> assertion)
        {
#if !DEBUG
            return asyncEnumerable;
#endif

            return asyncEnumerable.Select(p =>
            {
                Debug.Assert(assertion(p));

                return p;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IAsyncEnumerable<T> AssertEach<T>(this IAsyncEnumerable<T> asyncEnumerable, Func<T, bool> precondition, Func<T, bool> assertion)
        {
#if !DEBUG
            return asyncEnumerable;
#endif

            return asyncEnumerable.Select(p =>
            {
                if (precondition(p))
                {
                    Debug.Assert(assertion(p));
                }

                return p;
            });
        }

#endif
    }
}
