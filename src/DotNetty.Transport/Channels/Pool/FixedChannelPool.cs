﻿using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Common;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal;
using DotNetty.Transport.Bootstrapping;

namespace DotNetty.Transport.Channels.Pool
{
    public class FixedChannelPool : SimpleChannelPool
    {
        private static readonly InvalidOperationException FullException = new InvalidOperationException("Too many outstanding acquire operations");
        private static readonly TimeoutException TimeoutException = new TimeoutException("Acquire operation took longer then configured maximum time");
        internal static readonly InvalidOperationException PoolClosedOnReleaseException = new InvalidOperationException("FixedChannelPooled was closed");
        private static readonly InvalidOperationException PoolClosedOnAcquireException = new InvalidOperationException("FixedChannelPooled was closed");

        public enum AcquireTimeoutAction
        {
            None,
            New,
            Fail
        }

        private readonly IEventExecutor executor;
        private readonly TimeSpan acquireTimeout;
        private readonly IRunnable timeoutTask;

        private readonly IQueue<AcquireTask> pendingAcquireQueue = PlatformDependent.NewMpscQueue<AcquireTask>();

        private readonly int maxConnections;
        private readonly int maxPendingAcquires;
        private int acquiredChannelCount;
        private int pendingAcquireCount;
        private bool closed;

        public FixedChannelPool(Bootstrap bootstrap, IChannelPoolHandler handler, int maxConnections, int maxPendingAcquires = int.MaxValue)
            : this(bootstrap, handler, ChannelActiveHealthChecker.Instance, AcquireTimeoutAction.None, Timeout.InfiniteTimeSpan, maxConnections, maxPendingAcquires)
        {
        }

        public FixedChannelPool(Bootstrap bootstrap, IChannelPoolHandler handler, IChannelHealthChecker healthChecker, AcquireTimeoutAction action, TimeSpan acquireTimeout, int maxConnections, int maxPendingAcquires)
            : this(bootstrap, handler, healthChecker, action, acquireTimeout, maxConnections, maxPendingAcquires, true)
        {
        }

        public FixedChannelPool(Bootstrap bootstrap, IChannelPoolHandler handler, IChannelHealthChecker healthChecker, AcquireTimeoutAction action, TimeSpan acquireTimeout, int maxConnections, int maxPendingAcquires, bool releaseHealthCheck)
            : this(bootstrap, handler, healthChecker, action, acquireTimeout, maxConnections, maxPendingAcquires, releaseHealthCheck, true)
        {
        }

        public FixedChannelPool(Bootstrap bootstrap, IChannelPoolHandler handler, IChannelHealthChecker healthChecker, AcquireTimeoutAction action, TimeSpan acquireTimeout, int maxConnections, int maxPendingAcquires, bool releaseHealthCheck, bool lastRecentUsed)
            : base(bootstrap, handler, healthChecker, releaseHealthCheck, lastRecentUsed)
        {
            if (maxConnections < 1)
            {
                throw new ArgumentException($"maxConnections: {maxConnections} (expected: >= 1)");
            }

            if (maxPendingAcquires < 1)
            {
                throw new ArgumentException($"maxPendingAcquires: {maxPendingAcquires} (expected: >= 1)");
            }

            this.acquireTimeout = acquireTimeout;
            if (action == AcquireTimeoutAction.None && acquireTimeout == Timeout.InfiniteTimeSpan)
            {
                this.timeoutTask = null;
            }
            else if (action == AcquireTimeoutAction.None && acquireTimeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentException("action");
            }
            else if (action != AcquireTimeoutAction.None && acquireTimeout < TimeSpan.Zero)
            {
                throw new ArgumentException($"acquireTimeoutMillis: {acquireTimeout} (expected: >= 1)");
            }
            else
            {
                switch (action)
                {
                    case AcquireTimeoutAction.Fail:
                        this.timeoutTask = new TimeoutTask(this, OnTimeoutFail);
                        break;
                    case AcquireTimeoutAction.New:
                        this.timeoutTask = new TimeoutTask(this, OnTimeoutNew);
                        break;
                    default:
                        throw new ArgumentException("action");
                }
            }

            this.executor = bootstrap.Group.GetNext();
            this.maxConnections = maxConnections;
            this.maxPendingAcquires = maxPendingAcquires;
        }

        public override ValueTask<IChannel> AcquireAsync()
        {
            if (this.executor.InEventLoop)
            {
                return this.DoAcquireAsync(null);
            }

            var promise = new TaskCompletionSource<IChannel>();
            this.executor.Execute(this.Acquire0, promise);
            return new ValueTask<IChannel>(promise.Task);
        }

        private async void Acquire0(object state)
        {
            var promise = (TaskCompletionSource<IChannel>)state;
            try
            {
                var result = await this.DoAcquireAsync(promise);
                promise.TrySetResult(result);
            }
            catch (Exception ex)
            {
                promise.TrySetException(ex);
            }
        }

        private ValueTask<IChannel> DoAcquireAsync(TaskCompletionSource<IChannel> promise)
        {
            Contract.Assert(this.executor.InEventLoop);

            if (this.closed)
            {
                throw PoolClosedOnAcquireException;
            }

            if (this.acquiredChannelCount < this.maxConnections)
            {
                Contract.Assert(this.acquiredChannelCount >= 0);
                return new AcquireTask(this, promise).AcquireAsync();
            }
            else
            {
                if (this.pendingAcquireCount >= this.maxPendingAcquires)
                {
                    throw FullException;
                }
                else
                {
                    promise = promise ?? new TaskCompletionSource<IChannel>();
                    var task = new AcquireTask(this, promise);
                    if (this.pendingAcquireQueue.TryEnqueue(task))
                    {
                        ++this.pendingAcquireCount;

                        if (this.timeoutTask != null)
                        {
                            task.TimeoutTask = this.executor.Schedule(this.timeoutTask, this.acquireTimeout);
                        }
                    }
                    else
                    {
                        throw FullException;
                    }

                    return new ValueTask<IChannel>(promise.Task);
                }
            }
        }

        private ValueTask<IChannel> DoAcquireAsync() => base.AcquireAsync();
        
        public override async ValueTask<bool> ReleaseAsync(IChannel channel)
        {
            Contract.Requires(channel != null);
            
            if (this.executor.InEventLoop)
            {
                return await this.DoReleaseAsync(channel);
            }
            else
            {
                var promise = new TaskCompletionSource<bool>();
                this.executor.Schedule(this.Release0, channel, promise, TimeSpan.Zero);
                return await promise.Task;
            }
        }

        private async void Release0(object channel, object promise)
        {
            var tsc = promise as TaskCompletionSource<bool>;
            try
            {
                var result = await this.DoReleaseAsync((IChannel)channel);
                tsc.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tsc.TrySetException(ex);
            }
        }

        private async ValueTask<bool> DoReleaseAsync(IChannel channel)
        {
            Contract.Assert(this.executor.InEventLoop);
            
            try
            {
                await base.ReleaseAsync(channel);
                FailIfClosed(channel);
                
                this.DecrementAndRunTaskQueue();
                return true;
            }
            catch (Exception ex)
            {
                FailIfClosed(channel);
                if (!(ex is ArgumentException))
                {
                    this.DecrementAndRunTaskQueue();
                }

                throw;
            }

            void FailIfClosed(IChannel ch)
            {
                if (this.closed)
                {
                    ch.CloseAsync();
                    throw PoolClosedOnReleaseException;
                }
            }
        }


        private void DecrementAndRunTaskQueue()
        {
            --this.acquiredChannelCount;

            // We should never have a negative value.
            Contract.Assert(this.acquiredChannelCount >= 0);

            // Run the pending acquire tasks before notify the original promise so if the user would
            // try to acquire again from the ChannelFutureListener and the pendingAcquireCount is >=
            // maxPendingAcquires we may be able to run some pending tasks first and so allow to add
            // more.
            this.RunTaskQueue();
        }

        private void RunTaskQueue()
        {
            while (this.acquiredChannelCount < this.maxConnections)
            {
                if (!this.pendingAcquireQueue.TryDequeue(out AcquireTask task))
                {
                    break;
                }

                // Cancel the timeout if one was scheduled
                task.TimeoutTask?.Cancel();

                --this.pendingAcquireCount;

                task.AcquireAsync();
            }

            // We should never have a negative value.
            Contract.Assert(this.pendingAcquireCount >= 0);
            Contract.Assert(this.acquiredChannelCount >= 0);
        }

        public override void Dispose() => this.executor.Execute(this.Close);

        private void Close()
        {
            if (this.closed)
            {
                return;
            }

            this.closed = true;
            
            while(this.pendingAcquireQueue.TryDequeue(out AcquireTask task))
            {
                task.TimeoutTask?.Cancel();
                task.Promise.TrySetException(new ClosedChannelException());
            }

            this.acquiredChannelCount = 0;
            this.pendingAcquireCount = 0;
            base.Dispose();
        }

        private static void OnTimeoutNew(AcquireTask task) => task.AcquireAsync();

        private static void OnTimeoutFail(AcquireTask task) => task.Promise.TrySetException(TimeoutException);

        private class TimeoutTask : IRunnable
        {
            private readonly FixedChannelPool pool;
            private readonly Action<AcquireTask> onTimeout;

            public TimeoutTask(FixedChannelPool pool, Action<AcquireTask> onTimeout)
            {
                this.pool = pool;
                this.onTimeout = onTimeout;
            }

            public void Run()
            {
                Contract.Assert(this.pool.executor.InEventLoop);
                while (true)
                {
                    if (!this.pool.pendingAcquireQueue.TryPeek(out AcquireTask task) || PreciseTimeSpan.FromTicks(Stopwatch.GetTimestamp()) < task.ExpireTime)
                    {
                        break;
                    }

                    this.pool.pendingAcquireQueue.TryDequeue(out _);

                    --this.pool.pendingAcquireCount;
                    this.onTimeout(task);
                }
            }
        }

        private class AcquireTask
        {
            private readonly FixedChannelPool pool;
            
            public readonly TaskCompletionSource<IChannel> Promise;
            public readonly PreciseTimeSpan ExpireTime;
            public IScheduledTask TimeoutTask;

            private bool acquired;

            public AcquireTask(FixedChannelPool pool, TaskCompletionSource<IChannel> promise)
            {
                this.pool = pool;
                this.Promise = promise;
                this.ExpireTime = PreciseTimeSpan.FromTicks(Stopwatch.GetTimestamp()) + pool.acquireTimeout;
            }
            
            // Increment the acquire count and delegate to super to actually acquire a Channel which will
            // create a new connection.
            public ValueTask<IChannel> AcquireAsync()
            {
                var promise = this.Promise;
                
                if (this.pool.closed)
                {
                    if (promise != null)
                    {
                        promise.TrySetException(PoolClosedOnAcquireException);
                        return new ValueTask<IChannel>(promise.Task);
                    }
                    else
                    {
                        throw PoolClosedOnAcquireException;
                    }
                }

                this.Acquired();
                
                ValueTask<IChannel> future;
                
                try
                {
                    future = this.pool.DoAcquireAsync();
                    if (future.IsCompletedSuccessfully)
                    {
                        //pool never closed here
                        var channel = future.Result;
                        if (promise != null)
                        {
                            promise.TrySetResult(channel);
                            return new ValueTask<IChannel>(promise.Task);
                        }
                        else
                        {
                            return future;    
                        }
                    }
                }
                catch (Exception ex)
                {
                    //pool never closed here
                    ResumeQueue();

                    if (promise != null)
                    {
                        promise.TrySetException(ex);
                        return new ValueTask<IChannel>(promise.Task);
                    }
                    else
                    {
                        throw;
                    }
                }

                //at this point 'future' is a real Task
                promise = promise ?? new TaskCompletionSource<IChannel>();
                future.AsTask().ContinueWith(
                    t =>
                    {
                        Contract.Assert(this.pool.executor.InEventLoop);

                        if (this.pool.closed) 
                        {
                            if (t.Status == TaskStatus.RanToCompletion) 
                            {
                                // Since the pool is closed, we have no choice but to close the channel
                                t.Result.CloseAsync();
                            }
                            promise.TrySetException(PoolClosedOnAcquireException);
                        }
                        else if (t.Status == TaskStatus.RanToCompletion)
                        {
                            promise.TrySetResult(future.Result);
                        }
                        else 
                        {
                            ResumeQueue();
                            promise.TrySetException(t.Exception);
                        }
                    });

                return new ValueTask<IChannel>(promise.Task);
                
                void ResumeQueue()
                {
                    if (this.acquired)
                    {
                        this.pool.DecrementAndRunTaskQueue();
                    }
                    else
                    {
                        this.pool.RunTaskQueue();
                    }
                }
            }

            private void Acquired()
            {
                if (this.acquired)
                {
                    return;
                }

                this.pool.acquiredChannelCount++;
                this.acquired = true;
            }
        }
    }
}