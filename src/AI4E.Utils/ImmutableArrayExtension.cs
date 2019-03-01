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
using System.Collections.Immutable;

namespace AI4E.Utils
{
    public static class ImmutableArrayExtension
    {
        public static T? FirstOrDefaultEx<T>(this ImmutableArray<T> collection, Func<T, bool> predicate)
           where T : struct
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (var i = 0; i < collection.Length; i++)
            {
                var t = collection[i];

                if (predicate(t))
                    return t;
            }

            return default;
        }

        public static int FindIndex<T>(this ImmutableArray<T> collection, Func<T, bool> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            for (var i = 0; i < collection.Length; i++)
            {
                if (predicate(collection[i]))
                    return i;
            }

            return -1;
        }
    }
}
