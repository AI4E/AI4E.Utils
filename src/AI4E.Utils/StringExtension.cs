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

using System.Linq;

namespace System
{
    public static class AI4EUtilsStringExtension
    {
        public static bool ContainsWhitespace(this string str)
        {
#pragma warning disable CA1062
            return str.Length == 0 ? false : str.Any(c => char.IsWhiteSpace(c));
#pragma warning restore CA1062
        }

#if NETSTD20
        public static int IndexOf(this string str, char value, StringComparison comparisonType)
        {
#pragma warning disable CA1062
            return str.IndexOf(new string(value, count: 1), comparisonType);
#pragma warning restore CA1062
        }

        public static bool Contains(this string str, string value, StringComparison comparisonType)
        {
#pragma warning disable CA1062
            return str.IndexOf(value, comparisonType) >= 0;
#pragma warning restore CA1062
        }

        public static string Replace(this string str, string oldValue, string? newValue, StringComparison comparisonType)
        {
            if (oldValue is null)
                throw new ArgumentNullException(nameof(oldValue));

            if (oldValue.Length == 0)
                throw new ArgumentException("The argument must not be an empty string.", nameof(oldValue));

            if (newValue is null)
                throw new ArgumentNullException(nameof(newValue));

#pragma warning disable CA1062
            var index = str.IndexOf(oldValue, comparisonType);
#pragma warning restore CA1062

            while (index > 0)
            {
                var newStr = string.Empty;

                if (index > 0)
                {
                    newStr = str.Substring(0, index);
                }

                if (!string.IsNullOrEmpty(newValue))
                {
                    newStr += newValue;
                }

                if (index + oldValue.Length < str.Length)
                {
                    newStr += str.Substring(index + oldValue.Length);
                }

                str = newStr;
                index = str.IndexOf(oldValue, comparisonType);
            }

            return str;
        }

#endif
    }
}
