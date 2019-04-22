using System;

namespace AI4E.Utils.TestTypes
{
    public readonly struct RAIIDisposable : IDisposable
    {
        private readonly Action _action;

        public RAIIDisposable(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _action = action;
        }

        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}
