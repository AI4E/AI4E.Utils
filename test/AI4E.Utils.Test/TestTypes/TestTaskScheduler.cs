using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AI4E.Utils.TestTypes
{
    public sealed class TestTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        private readonly Thread _mainThread = null;

        public TestTaskScheduler()
        {
            _mainThread = new Thread(new ThreadStart(Execute));

            if (!_mainThread.IsAlive)
            {
                _mainThread.Start();
            }
        }

        private void Execute()
        {
            foreach (var task in _tasks.GetConsumingEnumerable())
            {
                TryExecuteTask(task);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        public void Dispose()
        {
            _tasks.CompleteAdding();
            _tasks.Dispose();
        }
    }
}
