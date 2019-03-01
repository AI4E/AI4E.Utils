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

/* Based on
 * --------------------------------------------------------------------------------------------------------------------
 * Asp.Net Core MVC
 * Copyright (c) .NET Foundation. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use
 * these files except in compliance with the License. You may obtain a copy of the
 * License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed
 * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
 * CONDITIONS OF ANY KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations under the License.
 * --------------------------------------------------------------------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Reflection;

namespace AI4E.Utils.ApplicationParts
{
    /// <summary>
    /// Default <see cref="ApplicationPartFactory"/>.
    /// </summary>
    public class DefaultApplicationPartFactory : ApplicationPartFactory
    {
        /// <summary>
        /// Gets an instance of <see cref="DefaultApplicationPartFactory"/>.
        /// </summary>
        public static DefaultApplicationPartFactory Instance { get; } = new DefaultApplicationPartFactory();

        /// <summary>
        /// Gets the sequence of <see cref="ApplicationPart"/> instances that are created by this instance of <see cref="DefaultApplicationPartFactory"/>.
        /// <para>
        /// Applications may use this method to get the same behavior as this factory produces during MVC's default part discovery.
        /// </para>
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <returns>The sequence of <see cref="ApplicationPart"/> instances.</returns>
        public static IEnumerable<ApplicationPart> GetDefaultApplicationParts(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            yield return new AssemblyPart(assembly);
        }

        /// <inheritdoc />
        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
        {
            return GetDefaultApplicationParts(assembly);
        }
    }
}
