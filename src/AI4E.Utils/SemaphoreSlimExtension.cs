using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils
{
    public static class SemaphoreSlimExtension
    {
        // True if the lock could be taken immediately, false otherwise.
        public static ValueTask<bool> LockOrWaitAsync(this SemaphoreSlim semaphore, CancellationToken cancellation)
        {
            if (semaphore.Wait(0))
            {
                Debug.Assert(semaphore.CurrentCount == 0);
                return new ValueTask<bool>(true);
            }

            return WaitAsync(semaphore, cancellation).AsValueTask();
        }

        private static async Task<bool> WaitAsync(SemaphoreSlim semaphore, CancellationToken cancellation)
        {
            await semaphore.WaitAsync(cancellation);
            Debug.Assert(semaphore.CurrentCount == 0);

            return false;
        }
    }
}
