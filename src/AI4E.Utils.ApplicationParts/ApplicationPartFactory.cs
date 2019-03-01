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
    /// Specifies a contract for synthesizing one or more <see cref="ApplicationPart"/> instances
    /// from an <see cref="Assembly"/>.
    /// <para>
    /// By default, Mvc registers each application assembly that it discovers as an <see cref="AssemblyPart"/>.
    /// Assemblies can optionally specify an <see cref="ApplicationPartFactory"/> to configure parts for the assembly
    /// by using <see cref="ProvideApplicationPartFactoryAttribute"/>.
    /// </para>
    /// </summary>
    public abstract class ApplicationPartFactory
    {
        /// <summary>
        /// Gets one or more <see cref="ApplicationPart"/> instances for the specified <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        public abstract IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly);

        /// <summary>
        /// Gets the <see cref="ApplicationPartFactory"/> for the specified assembly.
        /// <para>
        /// An assembly may specify an <see cref="ApplicationPartFactory"/> using <see cref="ProvideApplicationPartFactoryAttribute"/>.
        /// Otherwise, <see cref="DefaultApplicationPartFactory"/> is used.
        /// </para>
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <returns>An instance of <see cref="ApplicationPartFactory"/>.</returns>
        public static ApplicationPartFactory GetApplicationPartFactory(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var provideAttribute = assembly.GetCustomAttribute<ProvideApplicationPartFactoryAttribute>();
            if (provideAttribute == null)
            {
                return DefaultApplicationPartFactory.Instance;
            }

            var type = provideAttribute.GetFactoryType();
            if (!typeof(ApplicationPartFactory).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(string.Format(
                    "Type {0} specified by {1} is invalid. Type specified by {1} must derive from {2}.",
                    type,
                    nameof(ProvideApplicationPartFactoryAttribute),
                    typeof(ApplicationPartFactory)));
            }

            return (ApplicationPartFactory)Activator.CreateInstance(type);
        }
    }
}
