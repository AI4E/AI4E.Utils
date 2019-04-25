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

// Based on: https://stackoverflow.com/questions/4106862/how-to-sort-depended-objects-by-dependency#answer-11027096

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>
    /// Contains extension methods for the <see cref="IEnumerable{T}"/> type.
    /// </summary>
    public static class TopologicalSortEnumerableExtension
    {
        /// <summary>
        /// Topologically sorts the source elements.
        /// </summary>
        /// <typeparam name="T">The type of element.</typeparam>
        /// <param name="source">The enumerable of source elements.</param>
        /// <param name="dependencies">
        /// A func that returns the dependencies of the specified elements.
        /// </param>
        /// <param name="throwOnCycle">
        /// A boolean value that indicates whether an exception shall be thrown when cycles are detected.
        /// </param>
        /// <returns>The topologically sorted source elements.</returns>
        /// <remarks>
        /// The sort is stable. Elements that are on the same level in the topology are guaranteed to be in the same
        /// order than they were in the source collection.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dependencies"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if a cycle is detected and <paramref name="throwOnCycle"/> is true.</exception>
        public static IEnumerable<T> TopologicalSort<T>(
            this IEnumerable<T> source, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle = false)
        {
            var sorted = new List<T>();
            var visited = new HashSet<T>();

            foreach (var item in source)
            {
                Visit(item, visited, sorted, dependencies, throwOnCycle);
            }

            return sorted;
        }

        private static void Visit<T>(
            T item, HashSet<T> visited, List<T> sorted, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle)
        {
            if (!visited.Contains(item))
            {
                visited.Add(item);

                foreach (var dep in dependencies(item))
                {
                    Visit(dep, visited, sorted, dependencies, throwOnCycle);
                }

                sorted.Add(item);
            }
            else
            {
                if (throwOnCycle && !sorted.Contains(item))
                {
                    throw new InvalidOperationException("Cyclic dependency found.");
                }
            }
        }
    }
}

