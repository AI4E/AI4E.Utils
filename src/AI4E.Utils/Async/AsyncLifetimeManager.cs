using System;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.Async
{
    public sealed class AsyncLifetimeManager : IAsyncInitialization, IAsyncDisposable
    {
        private readonly DisposableAsyncLazy<byte> _underlyingManager;

        #region C'tor

        public AsyncLifetimeManager(Func<CancellationToken, Task> initialization, Func<Task> disposal, bool executeOnCallingThread = true)
        {
            var options = GetOptions(executeOnCallingThread);
            _underlyingManager = new DisposableAsyncLazy<byte>(
                factory: AsFactory(initialization),
                disposal: AsDisposal(disposal),
                options);
        }

        public AsyncLifetimeManager(Func<Task> initialization, Func<Task> disposal, bool executeOnCallingThread = true)
        {
            var options = GetOptions(executeOnCallingThread);
            _underlyingManager = new DisposableAsyncLazy<byte>(
                factory: AsFactory(initialization),
                disposal: AsDisposal(disposal),
                options);
        }

        public AsyncLifetimeManager(Func<CancellationToken, Task> initialization, bool executeOnCallingThread = true)
        {
            var options = GetOptions(executeOnCallingThread);
            _underlyingManager = new DisposableAsyncLazy<byte>(
                factory: AsFactory(initialization),
                options);
        }

        public AsyncLifetimeManager(Func<Task> initialization, bool executeOnCallingThread = true)
        {
            var options = GetOptions(executeOnCallingThread);
            _underlyingManager = new DisposableAsyncLazy<byte>(
                factory: AsFactory(initialization),
                options);
        }

        #endregion

        #region Helpers

        private static DisposableAsyncLazyOptions GetOptions(bool executeOnCallingThread)
        {
            var options = DisposableAsyncLazyOptions.Autostart;

            if (executeOnCallingThread)
            {
                options |= DisposableAsyncLazyOptions.ExecuteOnCallingThread;
            }

            return options;
        }

        private static Func<CancellationToken, Task<byte>> AsFactory(Func<Task> initialization)
        {
            return async cancellation =>
            {
                await initialization();
                return 0;
            };
        }

        private static Func<CancellationToken, Task<byte>> AsFactory(Func<CancellationToken, Task> initialization)
        {
            return async cancellation =>
            {
                await initialization(cancellation);
                return 0;
            };
        }

        private static Func<byte, Task> AsDisposal(Func<Task> disposal)
        {
            return _ => disposal();
        }

        #endregion

        public Task Initialization => _underlyingManager.Task;

        #region Disposal

        public Task Disposal => _underlyingManager.Disposal;

        public void Dispose()
        {
            _underlyingManager.Dispose();
        }

        public Task DisposeAsync()
        {
            return _underlyingManager.DisposeAsync();
        }

        #endregion
    }
}
