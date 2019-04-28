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
using System.Linq;
using System.Threading;

#if DEBUG
using System.Diagnostics;
#endif

namespace AI4E.Utils
{
    public static class EnumerableExtension
    {
        private const int _sequenceHashCodeSeedValue = 0x2D2816FE;
        private const int _sequenceHashCodePrimeNumber = 397;

        [ThreadStatic]
        private static Random _rnd;
        private static int _count = 0;

        // https://stackoverflow.com/questions/6165379/quickest-way-to-randomly-re-order-a-linq-collection
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var list = source.ToArray();

            if (list.Length < 2)
                return list;

            for (var i = list.Length; i > 1; i--)
            {
                var k = Rnd.Next(i);

                if (k == (i - 1))
                    continue;

                Swap(ref list[k], ref list[i - 1]);
            }

#if DEBUG

            Debug.Assert(source.Count() == list.Length);

            foreach (var element in source)
            {
                Debug.Assert(list.Contains(element));
            }

#endif

            return list;
        }

        private static void Swap<T>(ref T left, ref T right)
        {
            var t = left;

            left = right;
            right = t;
        }

        private static Random Rnd
        {
            get
            {
                if (_rnd == null)
                {
                    var seed = GetNextSeed();

                    _rnd = new Random(seed);
                }

                return _rnd;
            }
        }

        private static int GetNextSeed()
        {
            var count = Interlocked.Increment(ref _count);

            unchecked
            {
                return count + Environment.TickCount;
            }
        }

        public static T FirstOrDefault<T>(this IEnumerable<T> collection, Func<T, bool> predicate, T defaultValue)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var entry in collection)
            {
                if (predicate(entry))
                    return entry;
            }

            return defaultValue;
        }

        public static T FirstOrDefault<T>(this IEnumerable<T> collection, T defaultValue)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            using (var enumerator = collection.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }
            }

            return defaultValue;
        }

        // https://stackoverflow.com/questions/1779129/how-to-take-all-but-the-last-element-in-a-sequence-using-linq
        public static IEnumerable<T> TakeAllButLast<T>(this IEnumerable<T> source)
        {
            var enumerator = source.GetEnumerator();
            var hasRemainingItems = false;
            var isFirst = true;
            var item = default(T);

            try
            {
                do
                {
                    hasRemainingItems = enumerator.MoveNext();
                    if (hasRemainingItems)
                    {
                        if (!isFirst)
                            yield return item;
                        item = enumerator.Current;
                        isFirst = false;
                    }
                } while (hasRemainingItems);
            }
            finally
            {
                enumerator?.Dispose();
            }
        }

#if NETSTANDARD
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source is HashSet<T> hashSet)
            {
                return hashSet;
            }

            return new HashSet<T>(source);
        }
#endif

        // Adapted from: https://stackoverflow.com/questions/8094867/good-gethashcode-override-for-list-of-foo-objects-respecting-the-order#answer-48192420
        public static int GetSequenceHashCode<TItem>(this IEnumerable<TItem> list)
        {
            if (list == null)
                return 0;

            return list.Aggregate(_sequenceHashCodeSeedValue, (current, item) => (current * _sequenceHashCodePrimeNumber) + (Equals(item, default(TItem)) ? 0 : item.GetHashCode()));
        }

        public static IEnumerable<TResult> ElementWiseMerge<TFirst, TSecond, TResult>(
            this IEnumerable<TFirst> enumerable1,
            IEnumerable<TSecond> enumerable2,
            Func<TFirst, TSecond, TResult> mergeOperation)
        {
            var enumerator1 = enumerable1.GetEnumerator();

            try
            {
                var enumerator2 = enumerable2.GetEnumerator();

                try
                {
                    while (enumerator1.MoveNext() && enumerator2.MoveNext())
                    {
                        yield return mergeOperation(enumerator1.Current, enumerator2.Current);
                    }
                }
                finally
                {
                    enumerator2.Dispose();
                }
            }
            finally
            {
                enumerator1.Dispose();
            }
        }

        public static bool All(this IEnumerable<bool> enumerable)
        {
            return enumerable.All(_ => _);
        }
    }
}
