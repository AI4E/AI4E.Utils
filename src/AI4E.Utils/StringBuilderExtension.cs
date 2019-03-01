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

using System.Text;

namespace AI4E.Utils
{
    // Adapted from: https://stackoverflow.com/questions/1359948/why-doesnt-stringbuilder-have-indexof-method
    public static class StringBuilderExtension
    {
        public static int IndexOf(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
        {
            var length = value.Length;
            var maxSearchLength = (sb.Length - length) + 1;

            for (var i = startIndex; i < maxSearchLength; ++i)
            {
                if (AreEqual(sb[i], value[0], ignoreCase))
                {
                    var index = 1;
                    for (; index < length && AreEqual(sb[i + index], value[index], ignoreCase); index++) ;

                    if (index == length)
                        return i;
                }
            }

            return -1;
        }


        public static int IndexOf(this StringBuilder sb, char value, int startIndex, bool ignoreCase)
        {
            for (var i = startIndex; i < sb.Length; ++i)
            {
                if (AreEqual(sb[i], value, ignoreCase))
                    return i;
            }

            return -1;
        }

        public static int LastIndexOf(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
        {
            var length = value.Length;
            var maxSearchLength = sb.Length - length;

            for (var i = maxSearchLength; i >= startIndex; --i)
            {
                if (AreEqual(sb[i], value[0], ignoreCase))
                {
                    var index = 1;
                    for (; index < length && AreEqual(sb[i + index], value[index], ignoreCase); index++) ;

                    if (index == length)
                        return i;
                }
            }

            return -1;
        }

        public static int LastIndexOf(this StringBuilder sb, char value, int startIndex, bool ignoreCase)
        {
            for (var i = sb.Length - 1; i >= startIndex; --i)
            {
                if (AreEqual(sb[i], value, ignoreCase))
                    return i;
            }

            return -1;
        }

        private static bool AreEqual(char c1, char c2, bool ignoreCase)
        {
            if (ignoreCase)
            {
                return char.ToLower(c1) == char.ToLower(c2);
            }

            return c1 == c2;
        }
    }
}
