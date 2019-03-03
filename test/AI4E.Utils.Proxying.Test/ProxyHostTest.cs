using System;
using System.Threading.Tasks;
using AI4E.Utils.Proxying.Test.TestTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI4E.Utils.Proxying.Test
{
    [TestClass]
    public class ProxyHostTest
    {
        [TestMethod]
        public async Task BasicCallTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildEmptyServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildEmptyServiceProvider());

                var fooProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                var result = await fooProxy.ExecuteAsync(foo => foo.Add(5, 3));

                Assert.AreEqual(8, result);

                await fooProxy.DisposeAsync();
            }
        }

        private IServiceProvider BuildEmptyServiceProvider()
        {
            return new ServiceCollection().BuildServiceProvider();
        }
    }
}
