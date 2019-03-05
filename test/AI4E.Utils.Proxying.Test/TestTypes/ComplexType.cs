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
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying.Test.TestTypes
{
    [Serializable]
    public class ComplexType
    {
        public string Str { get; set; }
        public int Int { get; set; }
    }

    [Serializable]
    public class ComplexTypeWithProxy
    {
        public string ProxyName { get; set; }
        public IProxy<Value> Proxy { get; set; }
    }

    [Serializable]
    public class ComplexTypeWithCancellationToken
    {
        public string Str { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    [Serializable]
    public class ComplexTypeWithTransparentProxy
    {
        public string ProxyName { get; set; }
        public IValue Proxy { get; set; }
    }

    [Serializable]
    public class ComplexTypeStubBackReference
    {
        public string ABC { get; set; }
        public IProxy<ComplexTypeStub> Proxy { get; set; }
    }

    [Serializable]
    public class ComplexTypeStubTransparentBackReference
    {
        public string ABC { get; set; }
        public IComplexTypeStub Proxy { get; set; }
    }

    public interface IComplexTypeStub
    {
        CancellationToken Cancellation { get; }
        TaskCompletionSource<object> TaskCompletionSource { get; set; }

        ComplexType Echo(ComplexType complexType);
        ComplexTypeWithProxy GetComplexTypeWithProxy();
        ComplexTypeWithTransparentProxy GetComplexTypeWithTransparentProxy();
        int GetValue(ComplexTypeWithTransparentProxy complexType);
        Task<int> GetValueAsync(ComplexTypeWithProxy complexType);
        Task OperateAsync(ComplexTypeWithCancellationToken complexType);
        ComplexTypeStubBackReference GetComplexObjectWithBackReference();
        ComplexTypeStubTransparentBackReference GetComplexObjectWithTransparentBackReference();
    }

    public class ComplexTypeStub : IComplexTypeStub
    {
        public ComplexType Echo(ComplexType complexType)
        {
            return complexType;
        }

        public Task<int> GetValueAsync(ComplexTypeWithProxy complexType)
        {
            return complexType.Proxy.ExecuteAsync(p => p.GetValue());
        }

        public int GetValue(ComplexTypeWithTransparentProxy complexType)
        {
            return complexType.Proxy.GetValue();
        }

        public CancellationToken Cancellation { get; private set; }
        public TaskCompletionSource<object> TaskCompletionSource { get; set; }

        public async Task OperateAsync(ComplexTypeWithCancellationToken complexType)
        {
            Cancellation = complexType.CancellationToken;

            using (Cancellation.Register(() => TaskCompletionSource.TrySetCanceled(Cancellation)))
            {
                await (TaskCompletionSource?.Task ?? Task.CompletedTask);
            }
        }

        public ComplexTypeWithProxy GetComplexTypeWithProxy()
        {
            return new ComplexTypeWithProxy
            {
                ProxyName = "MyProxy",
                Proxy = ProxyHost.CreateProxy(new Value(23))
            };
        }

        public ComplexTypeWithTransparentProxy GetComplexTypeWithTransparentProxy()
        {
            return new ComplexTypeWithTransparentProxy
            {
                ProxyName = "MyProxy",
                Proxy = ProxyHost.CreateProxy(new Value(23)).Cast<IValue>().AsTransparentProxy()
            };
        }

        public ComplexTypeStubBackReference GetComplexObjectWithBackReference()
        {
            return new ComplexTypeStubBackReference
            {
                ABC = "DEF",
                Proxy = ProxyHost.CreateProxy(this)
            };
        }

        public ComplexTypeStubTransparentBackReference GetComplexObjectWithTransparentBackReference()
        {
            return new ComplexTypeStubTransparentBackReference
            {
                ABC = "DEF",
                Proxy = this // Do NOT wrap this in a transparent proxy. As the current instance is alread registered in the ProxyHost, this must be done automatically.
            };
        }
    }
}
