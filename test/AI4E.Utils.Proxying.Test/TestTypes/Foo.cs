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
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying.Test.TestTypes
{
    public interface IFoo
    {
        bool IsDisposed { get; }

        int Add(int a, int b);
        Task<int> AddAsync(int a, int b);
        void Dispose();
        int Get();
        void Set(int i);
        Task SetAsync(int i);
        IProxy<Value> GetBackProxy(IProxy<Value> proxy);
        Task<int> ReadValueAsync(IProxy<Value> proxy);
    }

    public sealed class Foo : IDisposable, IFoo
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public Task<int> AddAsync(int a, int b)
        {
            return Task.FromResult(a + b);
        }

        private int _i;

        public int Get()
        {
            return _i;
        }

        public void Set(int i)
        {
            _i = i;
        }

        public Task SetAsync(int i)
        {
            _i = i;
            return Task.CompletedTask;
        }

        public Task<int> ReadValueAsync(IValue transparentProxy)
        {
            return Task.FromResult(transparentProxy.GetValue());
        }

        public Task<int> ReadValueAsync(IProxy<Value> proxy)
        {
            return proxy.ExecuteAsync(value => value.GetValue());
        }

        public IProxy<Value> GetBackProxy(IProxy<Value> proxy)
        {
            return proxy;
        }

        public IValue GetBackTransparentProxy(IProxy<Value> proxy)
        {
            return proxy.Cast<IValue>().AsTransparentProxy();
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
    }
}
