using System.Threading;

namespace AI4E.Utils.TestTypes
{
    public sealed class TestSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            using (Use(this))
            {
                d(state);
            }

        }

        public override void Send(SendOrPostCallback d, object state)
        {
            using (Use(this))
            {
                d(state);
            }
        }

        public static RAIIDisposable Use()
        {
            return Use(new TestSynchronizationContext());
        }

        public static RAIIDisposable Use(TestSynchronizationContext synchronizationContext)
        {
            var currentContext = Current;
            SetSynchronizationContext(synchronizationContext);
            return new RAIIDisposable(() =>
            {
                if (Current == synchronizationContext)
                {
                    SetSynchronizationContext(currentContext);
                }
            });
        }
    }
}
