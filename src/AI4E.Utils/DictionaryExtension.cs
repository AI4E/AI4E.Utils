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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    public static class AI4EUtilsDictionaryExtension
    {
        // https://blogs.msdn.microsoft.com/pfxteam/2011/04/02/little-known-gems-atomic-conditional-removals-from-concurrentdictionary/
        public static bool Remove<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key, TValue comparison)
            where TKey : notnull
        {
#pragma warning disable CA1062
            return dictionary.Remove(new KeyValuePair<TKey, TValue>(key, comparison));
#pragma warning restore CA1062
        }

#if NETSTD20
        public static bool Remove<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)]out TValue value)
            where TKey : notnull
        {
            if (dictionary is ConcurrentDictionary<TKey, TValue> concurrentDictionary)
                return concurrentDictionary.TryRemove(key, out value);

#pragma warning disable CA1062
            if (dictionary.ContainsKey(key))
#pragma warning restore CA1062
            {
                value = dictionary[key];
                dictionary.Remove(key);
                return true;
            }

            value = default!;
            return false;
        }


        public static bool TryAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
            where TKey : notnull
        {
#pragma warning disable CA1062
            if (dictionary.ContainsKey(key))
#pragma warning restore CA1062
            {
                return false;
            }

            dictionary.Add(key, value);
            return true;
        }
#endif

        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
            where TKey : notnull
        {
#pragma warning disable CA1062
            if (dictionary.TryGetValue(key, out var result))
#pragma warning restore CA1062
                return result;

            dictionary.Add(key, value);
            return value;
        }

        public static TValue GetOrAdd<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
            where TKey : notnull
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

#pragma warning disable CA1062
            if (dictionary.TryGetValue(key, out var result))
#pragma warning restore CA1062
                return result;

            result = factory(key);
            dictionary.Add(key, result);
            return result;
        }
    }
}
