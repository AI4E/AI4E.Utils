using System;

namespace AI4E.Utils
{
    // Null object for the IDisposable interface
    public sealed class NoOpDisposable : IDisposable
    {
        public static NoOpDisposable Instance { get; } = new NoOpDisposable();

        private NoOpDisposable() { }

        void IDisposable.Dispose() { }
    }
}
