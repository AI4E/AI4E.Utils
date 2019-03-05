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

    public class ComplexTypeStub
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

    }
}
