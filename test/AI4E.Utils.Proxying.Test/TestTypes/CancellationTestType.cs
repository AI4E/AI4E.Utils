using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying.Test.TestTypes
{
    public sealed class CancellationTestType
    {
        public CancellationToken Cancellation { get; private set; }
        public TaskCompletionSource<object> TaskCompletionSource { get; set; }

        public async Task OperateAsync(int someArgument, CancellationToken cancellation)
        {
            Cancellation = cancellation;

            using (cancellation.Register(() => TaskCompletionSource.TrySetCanceled(cancellation)))
            {
                await (TaskCompletionSource?.Task ?? Task.CompletedTask);
            }
        }
    }
}
