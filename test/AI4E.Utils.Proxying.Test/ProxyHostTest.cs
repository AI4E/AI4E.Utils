/* License
 * --------------------------------------------------------------------------------------------------------------------
 * This file is part of the AI4E distribution.
 *   (https://github.com/AI4E/AI4E.Utils)
 * Copyright (c) 2018-2019 - 2019-2019 Andreas Truetschel and contributors.
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
using System.Linq;
using System.Threading;
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
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var fooProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                var result = await fooProxy.ExecuteAsync(foo => foo.Add(5, 3));

                Assert.IsNull(fooProxy.LocalInstance);
                Assert.AreEqual(8, result);
            }
        }

        [TestMethod]
        public async Task RemoteProxyComplianceTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var localProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                var remoteProxy = remoteProxyHost.LocalProxies.FirstOrDefault();

                Assert.IsNotNull(remoteProxy);
                Assert.IsInstanceOfType(remoteProxy, typeof(IProxy<Foo>));
                Assert.IsNotNull(remoteProxy.LocalInstance);
                Assert.IsInstanceOfType(remoteProxy.LocalInstance, typeof(Foo));
                Assert.AreEqual(localProxy.Id, remoteProxy.Id);
            }
        }

        [TestMethod]
        public async Task ProxyDisposalTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var localProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                var remoteProxy = (IProxy<Foo>)remoteProxyHost.LocalProxies.First();

                await localProxy.DisposeAsync();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                // The remote proxy must be unregistered.
                Assert.IsFalse(remoteProxyHost.LocalProxies.Contains((IProxy)remoteProxy));

                // The remote proxy must be disposed.
                Assert.IsTrue(remoteProxy.Disposal.Status == TaskStatus.RanToCompletion);

                // The remote proxy value must be disposed.
                Assert.IsTrue(remoteProxy.LocalInstance.IsDisposed);

                // The local proxy must be disposed.
                Assert.IsTrue(localProxy.Disposal.Status == TaskStatus.RanToCompletion);
            }
        }

        [TestMethod]
        public async Task LocalProxyHostDisposalTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var localProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                var remoteProxy = (IProxy<Foo>)remoteProxyHost.LocalProxies.First();

                await localProxyHost.DisposeAsync();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                // The remote proxy must be unregistered.
                Assert.IsFalse(remoteProxyHost.LocalProxies.Contains((IProxy)remoteProxy));

                // The remote proxy must be disposed.
                Assert.IsTrue(remoteProxy.Disposal.Status == TaskStatus.RanToCompletion);

                // The remote proxy value must be disposed.
                Assert.IsTrue(remoteProxy.LocalInstance.IsDisposed);

                // The local proxy must be disposed.
                Assert.IsTrue(localProxy.Disposal.Status == TaskStatus.RanToCompletion);
            }
        }

        [TestMethod]
        public async Task RemoteProxyHostDisposalTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var localProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                var remoteProxy = (IProxy<Foo>)remoteProxyHost.LocalProxies.First();

                await remoteProxyHost.DisposeAsync();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                // The remote proxy must be unregistered.
                Assert.IsFalse(remoteProxyHost.LocalProxies.Contains((IProxy)remoteProxy));

                // The remote proxy must be disposed.
                Assert.IsTrue(remoteProxy.Disposal.Status == TaskStatus.RanToCompletion);

                // The remote proxy value must be disposed.
                Assert.IsTrue(remoteProxy.LocalInstance.IsDisposed);

                // The local proxy must be disposed.
                Assert.IsTrue(localProxy.Disposal.Status == TaskStatus.RanToCompletion);
            }
        }

        [TestMethod]
        public async Task ConnectionBreakdownTest()
        {
            ProxyHost localProxyHost, remoteProxyHost;
            Proxy<Foo> localProxy, remoteProxy;

            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                localProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                remoteProxy = (Proxy<Foo>)remoteProxyHost.LocalProxies.First();
            }

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
            {
                await localProxy.ExecuteAsync(foo => foo.Add(1, 1));
            });

            // The remote proxy must be unregistered.
            Assert.IsFalse(remoteProxyHost.LocalProxies.Contains((IProxy)remoteProxy));

            // The remote proxy must be disposed.
            Assert.IsTrue(remoteProxy.Disposal.Status == TaskStatus.RanToCompletion);

            // The remote proxy value must be disposed.
            Assert.IsTrue(remoteProxy.LocalInstance.IsDisposed);

            // The local proxy must be disposed.
            Assert.IsTrue(localProxy.Disposal.Status == TaskStatus.RanToCompletion);
        }

        // TODO: Do we need to test this for all of the 4 ExecuteAsync methods? (YES)
        [TestMethod]
        public async Task MethodExecutionWhenDisposedTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var localProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);

                await localProxy.DisposeAsync();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () =>
                {
                    await localProxy.ExecuteAsync(foo => foo.Add(1, 1));
                });
            }
        }

        [TestMethod]
        public async Task ResolveFromServicesTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider(services =>
                {
                    services.AddSingleton<Value>(_ => new Value(10));
                }));

                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var valueProxy = await localProxyHost.LoadAsync<Value>(cancellation: default);
                var result = await valueProxy.ExecuteAsync(value => value.GetValue());

                Assert.AreEqual(10, result);

                await valueProxy.DisposeAsync();
            }
        }

        // TODO: Do we have a better name for this? This test a remote proxy creating another remote proxy and passing this to us.
        [TestMethod]
        public async Task RecursionTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var barProxy = await localProxyHost.CreateAsync<Bar>(cancellation: default);
                var fooProxy = await barProxy.ExecuteAsync(bar => bar.GetFoo());

                Assert.IsNull(fooProxy.LocalInstance);

                var result = await fooProxy.ExecuteAsync(value => value.Add(10, 5));

                Assert.AreEqual(15, result);

                await fooProxy.DisposeAsync();
                await barProxy.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task LocalProxyRoundtripTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var fooProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                var value = new Value(5);
                var valueLocalProxy = new Proxy<Value>(value, ownsInstance: true);

                var resultProxy = await fooProxy.ExecuteAsync(foo => foo.GetBackProxy(valueLocalProxy));

                Assert.IsNotNull(resultProxy.LocalInstance);
                Assert.IsInstanceOfType(resultProxy, typeof(IProxy<Value>));
                Assert.IsNotNull(resultProxy.LocalInstance);
                Assert.IsInstanceOfType(resultProxy.LocalInstance, typeof(Value));
                Assert.AreSame(value, resultProxy.LocalInstance);
                Assert.AreEqual(valueLocalProxy.Id, resultProxy.Id);

                // TODO: Do we really need to enforce this?
                Assert.AreSame(valueLocalProxy, resultProxy);
            }
        }

        // TODO: Do we have a better name? We test, whether a local proxy correctly materialize on the remote side, when we pass it as argument.
        [TestMethod]
        public async Task LocalProxyToRemoteTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider());
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var fooProxy = await localProxyHost.CreateAsync<Foo>(cancellation: default);
                var value = new Value(5);
                var valueLocalProxy = new Proxy<Value>(value, ownsInstance: true);

                var result = await fooProxy.ExecuteAsync(foo => foo.ReadValueAsync(valueLocalProxy));

                Assert.AreEqual(5, result);
            }
        }

        [TestMethod]
        public async Task CancellationTest()
        {
            using (var fs1 = new FloatingStream())
            using (var fs2 = new FloatingStream())
            using (var mux1 = new MultiplexStream(fs1, fs2))
            using (var mux2 = new MultiplexStream(fs2, fs1))
            {
                var tcs = new TaskCompletionSource<object>();
                var cancellationTestType = new CancellationTestType { TaskCompletionSource = tcs };

                var remoteProxyHost = new ProxyHost(mux1, BuildServiceProvider(services =>
                {
                    services.AddSingleton(cancellationTestType);
                }));
                var localProxyHost = new ProxyHost(mux2, BuildServiceProvider());

                var proxy = await localProxyHost.LoadAsync<CancellationTestType>(cancellation: default);

                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var task = proxy.ExecuteAsync(t => t.OperateAsync(26, cancellationTokenSource.Token));
                    await Task.Delay(50); // As we do not have a task to wait for, we have to delay for some time, in good hope that the message arrived at remote in that time.

                    Assert.IsTrue(cancellationTestType.Cancellation.CanBeCanceled);
                    Assert.IsFalse(cancellationTestType.Cancellation.IsCancellationRequested);

                    cancellationTokenSource.Cancel();
                    await Task.Delay(50); // As we do not have a task to wait for, we have to delay for some time, in good hope that the message arrived at remote in that time.

                    Assert.IsTrue(cancellationTestType.Cancellation.CanBeCanceled);
                    Assert.IsTrue(cancellationTestType.Cancellation.IsCancellationRequested);
                }

                tcs.SetResult(null);
            }
        }

        // TODO: Add test for various primitive types, casts, and Serialization of complex objects.

        private IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection().BuildServiceProvider();
        }

        private IServiceProvider BuildServiceProvider(Action<IServiceCollection> servicesBuilder)
        {
            var serviceCollection = new ServiceCollection();
            servicesBuilder(serviceCollection);
            return serviceCollection.BuildServiceProvider();
        }
    }
}
