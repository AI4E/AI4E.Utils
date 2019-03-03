using System;

namespace AI4E.Utils.Proxying.Test.TestTypes
{
    public sealed class Bar : IDisposable
    {
        public Proxy<Foo> GetFoo()
        {
            return new Proxy<Foo>(new Foo(), ownsInstance: true);
        }

        public void Dispose()
        {
            Console.WriteLine("Destroying bar");
        }
    }
}
