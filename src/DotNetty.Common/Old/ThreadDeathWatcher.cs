using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal;
using DotNetty.Common.Internal.Logging;

namespace DotNetty.Common
{
    public static class ThreadDeathWatcher
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance(typeof(ThreadDeathWatcher));

        private static readonly IQueue<Entry> PendingEntries = PlatformDependent.NewMpscQueue<Entry>();
        private static readonly Watcher watcher = new Watcher();
        private static int started;
        private static volatile Thread watcherThread;

        static ThreadDeathWatcher()
        {
            string poolName = "threadDeathWatcher";
            string serviceThreadPrefix = SystemPropertyUtil.Get("io.netty.serviceThreadPrefix");
            if (!string.IsNullOrEmpty(serviceThreadPrefix))
            {
                poolName = serviceThreadPrefix + poolName;
            }
        }

        /// <summary>
        /// Schedules the specified <see cref="Action"/> to run when the specified <see cref="Thread"/> dies.
        /// </summary>
        public static void Watch(Thread thread, Action task)
        {
            Contract.Requires(thread != null);
            Contract.Requires(task != null);
            Contract.Requires(thread.IsAlive);

            Schedule(thread, task, true);
        }

        /// <summary>
        /// Cancels the task scheduled via <see cref="Watch"/>.
        /// </summary>
        public static void Unwatch(Thread thread, Action task)
        {
            Contract.Requires(thread != null);
            Contract.Requires(task != null);

            Schedule(thread, task, false);
        }

        private static void Schedule(Thread thread, Action task, bool isWatch)
        {
            PendingEntries.TryEnqueue(new Entry(thread, task, isWatch));

            if (Interlocked.CompareExchange(ref started, 1, 0) == 0)
            {
                var watcherThread = new Thread(s => ((IRunnable)s).Run());
                watcherThread.IsBackground = true;
                watcherThread.Start(watcher);
                ThreadDeathWatcher.watcherThread = watcherThread;
            }
        }

        /// <summary>
        /// Waits until the thread of this watcher has no threads to watch and terminates itself.
        /// Because a new watcher thread will be started again on <see cref="Watch"/>,
        /// this operation is only useful when you want to ensure that the watcher thread is terminated
        /// <strong>after</strong> your application is shut down and there's no chance of calling <see cref="Watch"/>
        /// afterwards.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns><c>true</c> if and only if the watcher thread has been terminated.</returns>
        public static bool AwaitInactivity(TimeSpan timeout)
        {
            Thread watcherThread = ThreadDeathWatcher.watcherThread;
            if (watcherThread != null)
            {
                watcherThread.Join(timeout);
                return !watcherThread.IsAlive;
            }
            else
            {
                return true;
            }
        }

        private sealed class Watcher : IRunnable
        {
            private readonly List<Entry> watchers = new List<Entry>();

            public void Run()
            {
                for (;;)
                {
                    this.FetchWatchers();
                    this.NotifyWatchers();

                    // Try once again just in case notifyWatchees() triggered watch() or unwatch().
                    this.FetchWatchers();
                    this.NotifyWatchers();

                    Thread.Sleep(1000);

                    if (this.watchers.Count == 0 && PendingEntries.IsEmpty)
                    {
                        // Mark the current worker thread as stopped.
                        // The following CAS must always success and must be uncontended,
                        // because only one watcher thread should be running at the same time.
                        bool stopped = Interlocked.CompareExchange(ref started, 0, 1) == 1;
                        Contract.Assert(stopped);

                        // Check if there are pending entries added by watch() while we do CAS above.
                        if (PendingEntries.IsEmpty)
                        {
                            // A) watch() was not invoked and thus there's nothing to handle
                            //    -> safe to terminate because there's nothing left to do
                            // B) a new watcher thread started and handled them all
                            //    -> safe to terminate the new watcher thread will take care the rest
                            break;
                        }

                        // There are pending entries again, added by watch()
                        if (Interlocked.CompareExchange(ref started, 1, 0) != 0)
                        {
                            // watch() started a new watcher thread and set 'started' to true.
                            // -> terminate this thread so that the new watcher reads from pendingEntries exclusively.
                            break;
                        }

                        // watch() added an entry, but this worker was faster to set 'started' to true.
                        // i.e. a new watcher thread was not started
                        // -> keep this thread alive to handle the newly added entries.
                    }
                }
            }

            private void FetchWatchers()
            {
                for (;;)
                {
                    if (!PendingEntries.TryDequeue(out var entry))
                    {
                        break;
                    }

                    if (entry.IsWatch)
                    {
                        this.watchers.Add(entry);
                    }
                    else
                    {
                        this.watchers.Remove(entry);
                    }
                }
            }

            private void NotifyWatchers()
            {
                var watchers = this.watchers;
                for (int i = 0; i < watchers.Count;)
                {
                    Entry e = watchers[i];
                    if (!e.Thread.IsAlive)
                    {
                        watchers.RemoveAt(i);
                        try
                        {
                            e.Task();
                        }
                        catch (Exception t)
                        {
                            Logger.Warn("Thread death watcher task raised an exception:", t);
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        private sealed class Entry
        {
            internal readonly Thread Thread;
            internal readonly Action Task;
            internal readonly bool IsWatch;

            public Entry(Thread thread, Action task, bool isWatch)
            {
                this.Thread = thread;
                this.Task = task;
                this.IsWatch = isWatch;
            }

            public override int GetHashCode() => this.Thread.GetHashCode() ^ this.Task.GetHashCode();

            public override bool Equals(object obj)
            {
                if (obj == this)
                {
                    return true;
                }

                if (!(obj is Entry entry))
                {
                    return false;
                }

                return this.Thread == entry.Thread && this.Task == entry.Task;
            }
        }
    }
}