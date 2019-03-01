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
* Scrutor (https://github.com/khellang/Scrutor)
* The MIT License
* 
* Copyright (c) 2015 Kristian Hellang
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
* --------------------------------------------------------------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AI4E.Utils
{
    public static class TypeExtension
    {
        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }

        public static string GetUnqualifiedTypeName(this Type type)
        {
            return type.ToString();
        }

        public static bool IsDelegate(this Type type)
        {
            if (type.IsValueType)
                return false;

            if (type.IsInterface)
                return false;

            if (type == typeof(Delegate) || type == typeof(MulticastDelegate))
                return true;

            return type.IsSubclassOf(typeof(Delegate));
        }

        /// <summary>
        /// Checks whether the type is an ordinary class and specially not any of:
        /// - A value type
        /// - A delegate
        /// - A generic type definition
        /// - The type <see cref="object"/>, <see cref="Enum"/> or <see cref="ValueType"/>
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if <paramref name="type"/> is an ordinary class, false otherwise.</returns>
        public static bool IsOrdinaryClass(this Type type)
        {
            if (type.IsValueType)
                return false;

            if (type.IsDelegate())
                return false;

            if (type.IsGenericTypeDefinition)
                return false;

            // TODO: Do we have to check for System.Void? It is defined as a value type, that we alread check for.
            if (type == typeof(Enum) || type == typeof(ValueType) || type == typeof(void))
                return false;

            return true;
        }

        // Based on: https://github.com/khellang/Scrutor/blob/master/src/Scrutor/ReflectionExtensions.cs
        public static bool IsAssignableTo(this Type type, Type otherType)
        {
            var typeInfo = type.GetTypeInfo();
            var otherTypeInfo = otherType.GetTypeInfo();

            if (otherTypeInfo.IsGenericTypeDefinition)
            {
                return typeInfo.IsAssignableToGenericTypeDefinition(otherTypeInfo);
            }

            return otherTypeInfo.IsAssignableFrom(typeInfo);
        }

        // Based on: https://github.com/khellang/Scrutor/blob/master/src/Scrutor/ReflectionExtensions.cs
        private static bool IsAssignableToGenericTypeDefinition(this TypeInfo typeInfo, TypeInfo genericTypeInfo)
        {
            var interfaceTypes = typeInfo.ImplementedInterfaces.Select(t => t.GetTypeInfo());

            foreach (var interfaceType in interfaceTypes)
            {
                if (interfaceType.IsGenericType)
                {
                    var typeDefinitionTypeInfo = interfaceType
                        .GetGenericTypeDefinition()
                        .GetTypeInfo();

                    if (typeDefinitionTypeInfo.Equals(genericTypeInfo))
                    {
                        return true;
                    }
                }
            }

            if (typeInfo.IsGenericType)
            {
                var typeDefinitionTypeInfo = typeInfo
                    .GetGenericTypeDefinition()
                    .GetTypeInfo();

                if (typeDefinitionTypeInfo.Equals(genericTypeInfo))
                {
                    return true;
                }
            }

            var baseTypeInfo = typeInfo.BaseType?.GetTypeInfo();

            if (baseTypeInfo == null)
            {
                return false;
            }

            return baseTypeInfo.IsAssignableToGenericTypeDefinition(genericTypeInfo);
        }

        // Based on: https://github.com/khellang/Scrutor/blob/master/src/Scrutor/MissingTypeRegistrationException.cs
        public static string GetFriendlyName(this Type type)
        {
            if (type == typeof(int))
                return "int";

            if (type == typeof(short))
                return "short";

            if (type == typeof(byte))
                return "byte";

            if (type == typeof(bool))
                return "bool";

            if (type == typeof(char))
                return "char";

            if (type == typeof(long))
                return "long";

            if (type == typeof(float))
                return "float";

            if (type == typeof(double))
                return "double";

            if (type == typeof(decimal))
                return "decimal";

            if (type == typeof(string))
                return "string";

            if (type == typeof(object))
                return "object";

            if (type.IsGenericType)
                return GetGenericFriendlyName(type);

            return type.Name;
        }

        private static string GetGenericFriendlyName(Type type)
        {
            var argumentNames = type.GenericTypeArguments.Select(GetFriendlyName).ToArray();

            var baseName = type.Name.Split('`').First();

            return $"{baseName}<{string.Join(", ", argumentNames)}>";
        }
    }
}
