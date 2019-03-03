using System;

namespace AI4E.Utils.Proxying.Test.TestTypes
{
    public sealed class Value : IDisposable
    {
        private readonly int _value;

        public Value(int value)
        {
            _value = value;
        }

        public int GetValue()
        {
            return _value;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
    }
}
