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
    public sealed class PendingWriteQueue
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<PendingWriteQueue>();

        private readonly IChannelHandlerContext ctx;
        private readonly ChannelOutboundBuffer buffer;
        private readonly IMessageSizeEstimatorHandle estimatorHandle;

        // head and tail pointers for the linked-list structure. If empty head and tail are null.
        private PendingWrite head;
        private PendingWrite tail;
        private int size;

        public PendingWriteQueue(IChannelHandlerContext ctx)
        {
            Contract.Requires(ctx != null);

            this.ctx = ctx;
            this.buffer = ctx.Channel.Unsafe.OutboundBuffer;
            this.estimatorHandle = ctx.Channel.Configuration.MessageSizeEstimator.NewHandle();
        }

        /// <summary>
        /// Returns <c>true</c> if there are no pending write operations left in this queue.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                Contract.Assert(this.ctx.Executor.InEventLoop);

                return this.head == null;
            }
        }

        /// <summary>
        /// Returns the number of pending write operations.
        /// </summary>
        public int Size
        {
            get
            {
                Contract.Assert(this.ctx.Executor.InEventLoop);

                return this.size;
            }
        }

        /// <summary>
        /// Adds the given message to this <see cref="PendingWriteQueue"/>.
        /// </summary>
        /// <param name="msg">The message to add to the <see cref="PendingWriteQueue"/>.</param>
        /// <returns>An await-able task.</returns>
        public Task Add(object msg)
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);
            Contract.Requires(msg != null);

            int messageSize = this.estimatorHandle.Size(msg);
            if (messageSize < 0)
            {
                // Size may be unknow so just use 0
                messageSize = 0;
            }
            var promise = new TaskCompletionSource();
            PendingWrite write = PendingWrite.Acquire(msg, messageSize, promise);
            PendingWrite currentTail = this.tail;
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
            // We need to guard against null as channel.Unsafe.OutboundBuffer may returned null
            // if the channel was already closed when constructing the PendingWriteQueue.
            // See https://github.com/netty/netty/issues/3967
            this.buffer?.IncrementPendingOutboundBytes(write.Size);
            return promise.Task;
        }

        /// <summary>
        /// Removes all pending write operations, and fail them with the given <see cref="Exception"/>. The messages
        /// will be released via <see cref="ReferenceCountUtil.SafeRelease(object)"/>.
        /// </summary>
        /// <param name="cause">The <see cref="Exception"/> to fail with.</param>
        public void RemoveAndFailAll(Exception cause)
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);
            Contract.Requires(cause != null);

            // Guard against re-entrance by directly reset
            PendingWrite write = this.head;
            this.head = this.tail = null;
            this.size = 0;

            while (write != null)
            {
                PendingWrite next = write.Next;
                ReferenceCountUtil.SafeRelease(write.Msg);
                TaskCompletionSource promise = write.Promise;
                this.Recycle(write, false);
                Util.SafeSetFailure(promise, cause, Logger);
                write = next;
            }
            this.AssertEmpty();
        }

        /// <summary>
        /// Remove a pending write operation and fail it with the given <see cref="Exception"/>. The message will be
        /// released via <see cref="ReferenceCountUtil.SafeRelease(object)"/>.
        /// </summary>
        /// <param name="cause">The <see cref="Exception"/> to fail with.</param>
        public void RemoveAndFail(Exception cause)
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);
            Contract.Requires(cause != null);

            PendingWrite write = this.head;

            if (write == null)
            {
                return;
            }
            ReferenceCountUtil.SafeRelease(write.Msg);
            TaskCompletionSource promise = write.Promise;
            Util.SafeSetFailure(promise, cause, Logger);
            this.Recycle(write, true);
        }

        /// <summary>
        /// Removes all pending write operation and performs them via <see cref="IChannelHandlerContext.WriteAsync"/>
        /// </summary>
        /// <returns>An await-able task.</returns>
        public Task RemoveAndWriteAllAsync()
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);

            if (this.size == 1)
            {
                // No need to use ChannelPromiseAggregator for this case.
                return this.RemoveAndWriteAsync();
            }
            PendingWrite write = this.head;
            if (write == null)
            {
                // empty so just return null
                return null;
            }

            // Guard against re-entrance by directly reset
            this.head = this.tail = null;
            int currentSize = this.size;
            this.size = 0;

            var tasks = new List<Task>(currentSize);
            while (write != null)
            {
                PendingWrite next = write.Next;
                object msg = write.Msg;
                TaskCompletionSource promise = write.Promise;
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
        /// Removes a pending write operation and performs it via <see cref="IChannelHandlerContext.WriteAsync"/>.
        /// </summary>
        /// <returns>An await-able task.</returns>
        public Task RemoveAndWriteAsync()
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);

            PendingWrite write = this.head;
            if (write == null)
            {
                return null;
            }
            object msg = write.Msg;
            TaskCompletionSource promise = write.Promise;
            this.Recycle(write, true);
            this.ctx.WriteAsync(msg).LinkOutcome(promise);
            return promise.Task;
        }

        /// <summary>
        /// Removes a pending write operation and releases it's message via
        /// <see cref="ReferenceCountUtil.SafeRelease(object)"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="TaskCompletionSource" /> of the pending write, or <c>null</c> if the queue is empty.
        /// </returns>
        public TaskCompletionSource Remove()
        {
            Contract.Assert(this.ctx.Executor.InEventLoop);

            PendingWrite write = this.head;
            if (write == null)
            {
                return null;
            }
            TaskCompletionSource promise = write.Promise;
            ReferenceCountUtil.SafeRelease(write.Msg);
            this.Recycle(write, true);
            return promise;
        }

        /// <summary>
        /// Return the current message, or <c>null</c> if the queue is empty.
        /// </summary>
        public object Current
        {
            get
            {
                Contract.Assert(this.ctx.Executor.InEventLoop);

                return this.head?.Msg;
            }
        }

        private void Recycle(PendingWrite write, bool update)
        {
            PendingWrite next = write.Next;
            long writeSize = write.Size;

            if (update)
            {
                if (next == null)
                {
                    // Handled last PendingWrite so rest head and tail
                    // Guard against re-entrance by directly reset
                    this.head = this.tail = null;
                    this.size = 0;
                }
                else
                {
                    this.head = next;
                    this.size--;
                    Contract.Assert(this.size > 0);
                }
            }

            PendingWrite.Recycle(write);
            // We need to guard against null as channel.unsafe().outboundBuffer() may returned null
            // if the channel was already closed when constructing the PendingWriteQueue.
            // See https://github.com/netty/netty/issues/3967
            this.buffer?.DecrementPendingOutboundBytes(writeSize);
        }

        /// <summary>
        /// Holds all meta-data and constructs the linked-list structure.
        /// </summary>
        private sealed class PendingWrite : IRecycle
        {
            private static readonly ThreadLocalPool<PendingWrite> Pool = new ThreadLocalPool<PendingWrite>();

            public PendingWrite Next;
            public long Size;
            public TaskCompletionSource Promise;
            public object Msg;
            private IRecycleHandle<PendingWrite> handle;

            public static PendingWrite Acquire(object msg, int size, TaskCompletionSource promise)
            {
                var write = Pool.Acquire(out var handle);
                write.Size = size;
                write.Msg = msg;
                write.Promise = promise;
                write.handle = handle;
                return write;
            }

            public static void Recycle(PendingWrite obj)
            {
                Pool.Recycle(obj.handle);
            }
            
            void IRecycle.Recycle()
            {
                this.Size = 0;
                this.Next = null;
                this.Msg = null;
                this.Promise = null;
            }
        }
    }
}