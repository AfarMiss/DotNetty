using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetty.Common.Concurrency
{
    public sealed class ExecutorTaskScheduler : TaskScheduler
    {
        private readonly IEventExecutor executor;
        private bool started;

        public ExecutorTaskScheduler(IEventExecutor executor) => this.executor = executor;

        protected override void QueueTask(Task task)
        {
            if (this.started)
            {
                this.executor.Execute(new TaskQueueNode(this, task));
            }
            else
            {
                this.started = true;
                this.TryExecuteTask(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (taskWasPreviouslyQueued || !this.executor.InEventLoop)
            {
                return false;
            }

            return this.TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks() => null;

        protected override bool TryDequeue(Task task) => false;

        private sealed class TaskQueueNode : IRunnable
        {
            private readonly ExecutorTaskScheduler scheduler;
            private readonly Task task;

            public TaskQueueNode(ExecutorTaskScheduler scheduler, Task task)
            {
                this.scheduler = scheduler;
                this.task = task;
            }

            public void Run() => this.scheduler.TryExecuteTask(this.task);
        }
    }
}