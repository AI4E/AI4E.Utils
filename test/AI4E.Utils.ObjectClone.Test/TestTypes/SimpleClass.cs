/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * 
 * MIT License
 * 
 * Copyright (c) 2018-2019 Andreas Truetschel and contributors.
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
* Fast Deep Copy by Expression Trees 
* https://www.codeproject.com/articles/1111658/fast-deep-copy-by-expression-trees-c-sharp
* 
* MIT License
* 
* Copyright (c) 2014 - 2016 Frantisek Konopecky
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

namespace AI4E.Utils.ObjectClone.Test.TestTypes
{
    [Serializable]
    public class SimpleClass : ISimpleClass
    {
        public string PropertyPublic { get; set; }

        protected bool PropertyProtected { get; set; }

        private int PropertyPrivate { get; set; }

        public int _fieldPublic;

#pragma warning disable IDE0044
        private string _fieldPrivate;
#pragma warning restore IDE0044

        public readonly string _readOnlyField;

        public SimpleClass(int propertyPrivate, bool propertyProtected, string fieldPrivate)
        {
            PropertyPrivate = propertyPrivate;
            PropertyProtected = propertyProtected;
            _fieldPrivate = fieldPrivate + "_" + typeof(SimpleClass);
            _readOnlyField = _fieldPrivate + "_readonly";
        }

        public static SimpleClass CreateForTests(int seed)
        {
            return new SimpleClass(seed, seed % 2 == 1, "seed_" + seed)
                {
                    _fieldPublic = -seed,
                    PropertyPublic = "seed_" + seed + "_public"
                };
        }

        public int GetPrivateProperty()
        {
            return PropertyPrivate;
        }

        public string GetPrivateField()
        {
            return _fieldPrivate;
        }
    }
}
