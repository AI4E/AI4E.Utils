using System;
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying.Test.TestTypes
{
    public sealed class Foo : IDisposable
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public Task<int> AddAsync(int a, int b)
        {
            return Task.FromResult(a + b);
        }

        public T Get<T>()
        {
            return default;
        }

        public Task<int> ReadValueAsync(Proxy<Value> proxy)
        {
            return proxy.ExecuteAsync(value => value.GetValue());
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
    }
}
