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
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace AI4E.Utils.ApplicationParts.Test
{
    public class RelatedAssemblyPartTest
    {
        private static readonly string AssemblyDirectory = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

        [Fact]
        public void GetRelatedAssemblies_Noops_ForDynamicAssemblies()
        {
            // Arrange
            var name = new AssemblyName($"DynamicAssembly-{Guid.NewGuid()}");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);

            // Act
            var result = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: true);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetRelatedAssemblies_ThrowsIfRelatedAttributeReferencesSelf()
        {
            // Arrange
            var expected = "RelatedAssemblyAttribute specified on MyAssembly cannot be self referential.";
            var assembly = new TestAssembly { AttributeAssembly = "MyAssembly" };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: true));
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void GetRelatedAssemblies_ThrowsIfAssemblyCannotBeFound()
        {
            // Arrange
            var expected = $"Related assembly 'DoesNotExist' specified by assembly 'MyAssembly' could not be found in the directory {AssemblyDirectory}. Related assemblies must be co-located with the specifying assemblies.";
            var assemblyPath = Path.Combine(AssemblyDirectory, "MyAssembly.dll");
            var assembly = new TestAssembly
            {
                AttributeAssembly = "DoesNotExist"
            };

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() => RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: true));
            Assert.Equal(expected, ex.Message);
            Assert.Equal(Path.Combine(AssemblyDirectory, "DoesNotExist.dll"), ex.FileName);
        }

        [Fact]
        public void GetRelatedAssemblies_LoadsRelatedAssembly()
        {
            // Arrange
            var destination = Path.Combine(AssemblyDirectory, "RelatedAssembly.dll");
            var assembly = new TestAssembly
            {
                AttributeAssembly = "RelatedAssembly",
            };
            var relatedAssembly = typeof(RelatedAssemblyPartTest).Assembly;

            var result = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: true, file => true, file =>
            {
                Assert.Equal(file, destination);
                return relatedAssembly;
            });
            Assert.Equal(new[] { relatedAssembly }, result);
        }

        [Fact]
        public void GetAssemblyLocation_UsesCodeBase()
        {
            // Arrange
            var destination = Path.Combine(AssemblyDirectory, "RelatedAssembly.dll");
            var codeBase = "file://x:/file/Assembly.dll";
            var expected = new Uri(codeBase).LocalPath;
            var assembly = new TestAssembly
            {
                CodeBaseSettable = codeBase,
            };

            // Act
            var actual = RelatedAssemblyAttribute.GetAssemblyLocation(assembly);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetAssemblyLocation_UsesLocation_IfCodeBaseIsNotLocal()
        {
            // Arrange
            var destination = Path.Combine(AssemblyDirectory, "RelatedAssembly.dll");
            var expected = Path.Combine(AssemblyDirectory, "Some-Dir", "Assembly.dll");
            var assembly = new TestAssembly
            {
                CodeBaseSettable = "https://www.microsoft.com/test.dll",
                LocationSettable = expected,
            };

            // Act
            var actual = RelatedAssemblyAttribute.GetAssemblyLocation(assembly);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetAssemblyLocation_CodeBase_HasPoundCharacterUnixPath()
        {
            var destination = Path.Combine(AssemblyDirectory, "RelatedAssembly.dll");
            var expected = @"/etc/#NIN/dotnetcore/tryx/try1.dll";
            var assembly = new TestAssembly
            {
                CodeBaseSettable = "file:///etc/#NIN/dotnetcore/tryx/try1.dll",
                LocationSettable = expected,
            };

            // Act
            var actual = RelatedAssemblyAttribute.GetAssemblyLocation(assembly);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetAssemblyLocation_CodeBase_HasPoundCharacterUNCPath()
        {
            var destination = Path.Combine(AssemblyDirectory, "RelatedAssembly.dll");
            var expected = @"\\server\#NIN\dotnetcore\tryx\try1.dll";
            var assembly = new TestAssembly
            {
                CodeBaseSettable = "file://server/#NIN/dotnetcore/tryx/try1.dll",
                LocationSettable = expected,
            };

            // Act
            var actual = RelatedAssemblyAttribute.GetAssemblyLocation(assembly);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetAssemblyLocation_CodeBase_HasPoundCharacterDOSPath()
        {
            var destination = Path.Combine(AssemblyDirectory, "RelatedAssembly.dll");
            var expected = @"C:\#NIN\dotnetcore\tryx\try1.dll";
            var assembly = new TestAssembly
            {
                CodeBaseSettable = "file:///C:/#NIN/dotnetcore/tryx/try1.dll",
                LocationSettable = expected,
            };

            // Act
            var actual = RelatedAssemblyAttribute.GetAssemblyLocation(assembly);
            Assert.Equal(expected, actual);
        }

        private class TestAssembly : Assembly
        {
            public override AssemblyName GetName()
            {
                return new AssemblyName("MyAssembly");
            }

            public string AttributeAssembly { get; set; }

            public string CodeBaseSettable { get; set; } = Path.Combine(AssemblyDirectory, "MyAssembly.dll");

            public override string CodeBase => CodeBaseSettable;

            public string LocationSettable { get; set; } = Path.Combine(AssemblyDirectory, "MyAssembly.dll");

            public override string Location => LocationSettable;

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                var attribute = new RelatedAssemblyAttribute(AttributeAssembly);
                return new[] { attribute };
            }
        }
    }
}
