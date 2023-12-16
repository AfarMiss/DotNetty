using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using DotNetty.Common;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Channels
{
    public sealed class BatchingPendingWriteQueue
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<PendingWriteQueue>();

        private readonly IChannelHandlerContext ctx;
        private readonly int maxSize;
        private readonly ChannelOutboundBuffer buffer;
        private readonly IMessageSizeEstimatorHandle estimatorHandle;

        private PendingWrite head;
        private PendingWrite tail;
        private int size;

        public BatchingPendingWriteQueue(IChannelHandlerContext ctx, int maxSize)
        {
            this.ctx = ctx;
            this.maxSize = maxSize;
            this.buffer = ctx.Channel.Unsafe.OutboundBuffer;
            this.estimatorHandle = ctx.Channel.Configuration.MessageSizeEstimator.NewHandle();
        }

        /// <summary>Returns <c>true</c> if there are no pending write operations left in this queue.</summary>
        public bool IsEmpty
        {
            get
            {
                Contract.Assert(this.ctx.Executor.InEventLoop);

                return this.head == null;
            }
        }

        /// <summary>Returns the number of pending write operations.</summary>
        public int Size
        {
            get
            {
                Contract.Assert(this.ctx.Executor.InEventLoop);

                return this.size;
            }
        }

        public Task Add(object msg)
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);
            Contract.Requires(msg != null);

            int messageSize = this.estimatorHandle.Size(msg);
            if (messageSize < 0) messageSize = 0;
            
            var currentTail = this.tail;
            if (currentTail != null)
            {
                if (this.CanBatch(messageSize, currentTail.Size))
                {
                    currentTail.Add(msg, messageSize);
                    return currentTail.Promise.Task;
                }
            }

            var promise = new TaskCompletionSource();
            var write = PendingWrite.Acquire(msg, messageSize, promise);
            if (currentTail == null)
            {
                this.tail = this.head = write;
            }
            else
            {
                currentTail.Next = write;
                this.tail = write;
            }
            this.size++;
            // 预防Channel关闭导致OutboundBuffer null
            this.buffer?.IncrementPendingOutboundBytes(messageSize);
            return promise.Task;
        }

        /// <summary>
        /// 移除并释放所有等待写入操作且抛出异常
        /// </summary>
        public void RemoveAndFailAll(Exception cause)
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);
            Contract.Requires(cause != null);

            // 重置 防止重新写入
            var write = this.head;
            this.Reset();

            while (write != null)
            {
                var next = write.Next;
                ReleaseMessages(write.Messages);
                var promise = write.Promise;
                this.Recycle(write, false);
                Util.SafeSetFailure(promise, cause, Logger);
                write = next;
            }
            this.AssertEmpty();
        }

        public void RemoveAndFail(Exception cause)
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);
            Contract.Requires(cause != null);

            var write = this.head;

            if (write == null) return;
            ReleaseMessages(write.Messages);
            var promise = write.Promise;
            Util.SafeSetFailure(promise, cause, Logger);
            this.Recycle(write, true);
        }

        /// <summary>
        ///     Remove all pending write operation and performs them via
        ///     <see cref="IChannelHandlerContext.WriteAsync(object)" />.
        /// </summary>
        /// <returns>
        ///     <see cref="Task" /> if something was written and <c>null</c> if the <see cref="BatchingPendingWriteQueue" />
        ///     is empty.
        /// </returns>
        public Task RemoveAndWriteAllAsync()
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);

            // 单条操作
            if (this.size == 1) return this.RemoveAndWriteAsync();
            
            var write = this.head;
            if (write == null) return null;

            // 重置 防止重新写入
            int currentSize = this.size;
            this.Reset();

            var tasks = new List<Task>(currentSize);
            while (write != null)
            {
                var next = write.Next;
                object msg = write.Messages;
                var promise = write.Promise;
                this.Recycle(write, false);
                this.ctx.WriteAsync(msg).LinkOutcome(promise);
                tasks.Add(promise.Task);
                write = next;
            }
            this.AssertEmpty();
            return Task.WhenAll(tasks);
        }

        private void AssertEmpty() => Contract.Assert(this.tail == null && this.head == null && this.size == 0);

        /// <summary>
        ///     Removes a pending write operation and performs it via
        ///     <see cref="IChannelHandlerContext.WriteAsync(object)"/>.
        /// </summary>
        /// <returns>
        ///     <see cref="Task" /> if something was written and <c>null</c> if the <see cref="BatchingPendingWriteQueue" />
        ///     is empty.
        /// </returns>
        public Task RemoveAndWriteAsync()
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);

            var write = this.head;
            if (write == null) return null;
            
            object msg = write.Messages;
            var promise = write.Promise;
            this.Recycle(write, true);
            this.ctx.WriteAsync(msg).LinkOutcome(promise);
            return promise.Task;
        }

        /// <summary>
        ///     Removes a pending write operation and release it's message via <see cref="ReferenceCountUtil.SafeRelease(object)"/>.
        /// </summary>
        /// <returns><see cref="TaskCompletionSource" /> of the pending write or <c>null</c> if the queue is empty.</returns>
        public TaskCompletionSource Remove()
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);

            var write = this.head;
            if (write == null) return null;
            
            var promise = write.Promise;
            ReferenceCountUtil.SafeRelease(write.Messages);
            this.Recycle(write, true);
            return promise;
        }

        /// <summary>
        ///     Return the current message or <c>null</c> if empty.
        /// </summary>
        public List<object> Current
        {
            get
            {
                Contract.Assert(this.ctx.Executor.InEventLoop);

                return this.head?.Messages;
            }
        }

        public long? CurrentSize
        {
            get
            {
                Contract.Assert(this.ctx.Executor.InEventLoop);

                return this.head?.Size;
            }
        }

        private bool CanBatch(int size, long currentBatchSize)
        {
            if (size < 0) return false;
            return currentBatchSize + size <= this.maxSize;
        }

        private void Recycle(PendingWrite write, bool update)
        {
            PendingWrite next = write.Next;
            long writeSize = write.Size;

            if (update)
            {
                if (next == null)
                {
                    this.Reset();
                }
                else
                {
                    this.head = next;
                    this.size--;
                    Contract.Assert(this.size > 0);
                }
            }
            PendingWrite.Recycle(write);
            this.buffer?.DecrementPendingOutboundBytes(writeSize);
        }

        private static void ReleaseMessages(List<object> messages)
        {
            foreach (var msg in messages)
            {
                ReferenceCountUtil.SafeRelease(msg);
            }
        }

        private void Reset()
        {
            this.head = this.tail = null;
            this.size = 0;
        }
        
        private sealed class PendingWrite : IRecycle
        {
            private static readonly ThreadLocalPool<PendingWrite> Pool = new ThreadLocalPool<PendingWrite>(() => new PendingWrite());

            public PendingWrite Next;
            public long Size;
            public TaskCompletionSource Promise;
            public readonly List<object> Messages = new List<object>();
            private IRecycleHandle<PendingWrite> handle;

            public static PendingWrite Acquire(object msg, int size, TaskCompletionSource promise)
            {
                var write = Pool.Acquire(out var handle);
                write.Add(msg, size);
                write.Promise = promise;
                write.handle = handle;
                return write;
            }
            
            public static void Recycle(PendingWrite obj)
            {
                Pool.Recycle(obj.handle);
            }

            public void Add(object msg, int size)
            {
                this.Messages.Add(msg);
                this.Size += size;
            }

            void IRecycle.Recycle()
            {
                this.Size = 0;
                this.Next = null;
                this.Messages.Clear();
                this.Promise = null;
            }
        }
    }
}