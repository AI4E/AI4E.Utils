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
using System.Reflection;
using static System.Diagnostics.Debug;

using System.Diagnostics.CodeAnalysis;

namespace AI4E.Utils
{
    public sealed class TypeLoadHelper
    {
        public static bool TryLoadTypeFromUnqualifiedName(string unqualifiedTypeName, [NotNullWhen(true)] out Type? type)
        {
#pragma warning disable CA1062
            if (unqualifiedTypeName.IndexOf('`', StringComparison.Ordinal) >= 0)
            {
                type = LoadGenericType(unqualifiedTypeName);
#pragma warning restore CA1062
            }
            else
            {
                type = LoadNonGenericOrTypeDefinition(unqualifiedTypeName);
            }

            return type != null;
        }

        public static Type LoadTypeFromUnqualifiedName(string unqualifiedTypeName)
        {
            if (!TryLoadTypeFromUnqualifiedName(unqualifiedTypeName, out var type))
            {
                throw new ArgumentException($"Type '{unqualifiedTypeName}' could not be loaded.");
            }

            return type!;
        }

        private static Type? LoadNonGenericOrTypeDefinition(string unqualifiedTypeName)
        {
            Assert(!unqualifiedTypeName.Contains(",", StringComparison.Ordinal));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var type = TryLoad(assembly, unqualifiedTypeName);

                if (type != null)
                    return type;
            }

            return null;
        }

        private static Type? TryLoad(Assembly assembly, string unqualifiedTypeName)
        {
            return assembly.GetType(unqualifiedTypeName, false);
        }

        private static Type? LoadGenericType(string unqualifiedTypeName)
        {
            Type? type = null;
            var openBracketIndex = unqualifiedTypeName.IndexOf('[', StringComparison.Ordinal);
            if (openBracketIndex >= 0)
            {
                var genericTypeDefName = unqualifiedTypeName.Substring(0, openBracketIndex);
                var genericTypeDef = LoadNonGenericOrTypeDefinition(genericTypeDefName);

                if (genericTypeDef == null)
                {
                    return null;
                }

                if (genericTypeDef != null)
                {
                    var genericTypeArguments = new List<Type>();
                    var scope = 0;
                    var typeArgStartIndex = openBracketIndex + 1;
                    var endIndex = unqualifiedTypeName.Length - 1;

                    var i = openBracketIndex;
                    for (; i <= endIndex; ++i)
                    {
                        var current = unqualifiedTypeName[i];
                        switch (current)
                        {
                            case '[':
                                ++scope;
                                break;
                            case ',':
                                if (scope == 1)
                                {
                                    var typeArgName = unqualifiedTypeName.Substring(typeArgStartIndex, i - typeArgStartIndex);
                                    genericTypeArguments.Add(LoadTypeFromUnqualifiedName(typeArgName));

                                    typeArgStartIndex = i + 1;
                                }
                                break;

                            case ']':
                                --scope;
                                if (scope == 0)
                                {
                                    var typeArgName = unqualifiedTypeName.Substring(typeArgStartIndex, i - typeArgStartIndex);
                                    genericTypeArguments.Add(LoadTypeFromUnqualifiedName(typeArgName));

                                    goto X;
                                }
                                break;
                        }
                    }

X:

                    type = genericTypeDef.MakeGenericType(genericTypeArguments.ToArray());

                    // https://github.com/AI4E/AI4E.Utils/issues/50
                    if (i < endIndex)
                    {
                        if (unqualifiedTypeName[i + 1] == '[' && unqualifiedTypeName[i + 2] == ']')
                        {
                            type = type.MakeArrayType();
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
            }

            return type;
        }
    }
}
