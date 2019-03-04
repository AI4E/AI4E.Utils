using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Proxying.Test.TestTypes
{
    public sealed class CancellationTestType
    {
        public CancellationToken Cancellation { get; private set; }
        public TaskCompletionSource<object> TaskCompletionSource { get; set; }

        public Task OperateAsync(int someArgument, CancellationToken cancellation)
        {
            Cancellation = cancellation;

            return TaskCompletionSource?.Task ?? Task.CompletedTask;
        }
    }
}
