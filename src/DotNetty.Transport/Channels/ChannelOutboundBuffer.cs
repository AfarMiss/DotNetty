using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using DotNetty.Buffers;
using DotNetty.Common;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public sealed class ChannelOutboundBuffer
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ChannelOutboundBuffer>();
        private static readonly ThreadLocalByteBufferList NioBuffers = new ThreadLocalByteBufferList();

        private readonly IChannel channel;

        private Entry flushedEntry;
        private Entry unflushedEntry;
        private Entry tailEntry;
        private int flushed;
        private long nioBufferSize;
        private bool inFail;
        private long totalPendingSize;
        private volatile int unwritable;

        internal ChannelOutboundBuffer(IChannel channel)
        {
            this.channel = channel;
        }

        public void AddMessage(object msg, int size, TaskCompletionSource promise)
        {
            var entry = Entry.Acquire(msg, size, promise);
            if (this.tailEntry == null)
            {
                this.flushedEntry = null;
                this.tailEntry = entry;
            }
            else
            {
                var tail = this.tailEntry;
                tail.Next = entry;
                this.tailEntry = entry;
            }
            this.unflushedEntry ??= entry;

            this.IncrementPendingOutboundBytes(size, false);
        }

        public void AddFlush()
        {
            var entry = this.unflushedEntry;
            if (entry != null)
            {
                this.flushedEntry ??= entry;
                
                do
                {
                    this.flushed++;
                    if (!entry.Promise.SetUncancellable())
                    {
                        //已取消,确保释放内存并通知释放的字节
                        int pending = entry.Cancel();
                        this.DecrementPendingOutboundBytes(pending, false, true);
                    }
                    entry = entry.Next;
                }
                while (entry != null);

                // 完成 重置
                this.unflushedEntry = null;
            }
        }

        internal void IncrementPendingOutboundBytes(long size) => this.IncrementPendingOutboundBytes(size, true);

        private void IncrementPendingOutboundBytes(long size, bool invokeLater)
        {
            if (size == 0)
            {
                return;
            }

            var newWriteBufferSize = Interlocked.Add(ref this.totalPendingSize, size);
            if (newWriteBufferSize >= this.channel.Configuration.WriteBufferHighWaterMark)
            {
                this.SetUnwritable(invokeLater);
            }
        }

        internal void DecrementPendingOutboundBytes(long size) => this.DecrementPendingOutboundBytes(size, true, true);

        private void DecrementPendingOutboundBytes(long size, bool invokeLater, bool notifyWritability)
        {
            if (size == 0)
            {
                return;
            }

            var newWriteBufferSize = Interlocked.Add(ref this.totalPendingSize, -size);
            if (notifyWritability && (newWriteBufferSize == 0
                || newWriteBufferSize <= this.channel.Configuration.WriteBufferLowWaterMark))
            {
                this.SetWritable(invokeLater);
            }
        }

        public object Current => this.flushedEntry?.Message;

        public void Progress(long amount)
        {
        }

        public bool Remove()
        {
            var entry = this.flushedEntry;
            if (entry == null)
            {
                ClearNioBuffers();
                return false;
            }

            var promise = entry.Promise;
            int size = entry.PendingSize;

            this.RemoveEntry(entry);

            if (!entry.Cancelled)
            {
                // only release message, notify and decrement if it was not canceled before.
                ReferenceCountUtil.SafeRelease(entry.Message);
                SafeSuccess(promise);
                this.DecrementPendingOutboundBytes(size, false, true);
            }

            Entry.Recycle(entry);

            return true;
        }

        public bool Remove(Exception cause) => this.Remove0(cause, true);

        private bool Remove0(Exception cause, bool notifyWritability)
        {
            var entry = this.flushedEntry;
            if (entry == null)
            {
                ClearNioBuffers();
                return false;
            }

            var promise = entry.Promise;
            int size = entry.PendingSize;

            this.RemoveEntry(entry);

            if (!entry.Cancelled)
            {
                // only release message, fail and decrement if it was not canceled before.
                ReferenceCountUtil.SafeRelease(entry.Message);
                SafeFail(promise, cause);
                this.DecrementPendingOutboundBytes(size, false, notifyWritability);
            }

            Entry.Recycle(entry);

            return true;
        }

        private void RemoveEntry(Entry e)
        {
            if (--this.flushed == 0)
            {
                this.flushedEntry = null;
                if (e == this.tailEntry)
                {
                    this.tailEntry = null;
                    this.unflushedEntry = null;
                }
            }
            else
            {
                this.flushedEntry = e.Next;
            }
        }

        public void RemoveBytes(long writtenBytes)
        {
            while (true)
            {
                if (!(this.Current is IByteBuffer buf))
                {
                    Contract.Assert(writtenBytes == 0);
                    break;
                }

                int readerIndex = buf.ReaderIndex;
                int readableBytes = buf.WriterIndex - readerIndex;

                if (readableBytes <= writtenBytes)
                {
                    if (writtenBytes != 0)
                    {
                        Progress(readableBytes);
                        writtenBytes -= readableBytes;
                    }
                    this.Remove();
                }
                else
                {
                    // readableBytes > writtenBytes
                    if (writtenBytes != 0)
                    {
                        //Invalid nio buffer cache for partial writen, see https://github.com/Azure/DotNetty/issues/422
                        this.flushedEntry.Buffer = new ArraySegment<byte>();
                        this.flushedEntry.Buffers = null;

                        buf.SetReaderIndex(readerIndex + (int)writtenBytes);
                        Progress(writtenBytes);
                    }
                    break;
                }
            }
            ClearNioBuffers();
        }

        private static void ClearNioBuffers() => NioBuffers.Value.Clear();

        public List<ArraySegment<byte>> GetSharedBufferList() => this.GetSharedBufferList(int.MaxValue, int.MaxValue);

        public List<ArraySegment<byte>> GetSharedBufferList(int maxCount, long maxBytes)
        {
            Debug.Assert(maxCount > 0);
            Debug.Assert(maxBytes > 0);

            long ioBufferSize = 0;
            int nioBufferCount = 0;
            var nioBuffers = NioBuffers.Value;
            Entry entry = this.flushedEntry;
            while (this.IsFlushedEntry(entry) && entry.Message is IByteBuffer buffer)
            {
                if (!entry.Cancelled)
                {
                    int readerIndex = buffer.ReaderIndex;
                    int readableBytes = buffer.WriterIndex - readerIndex;

                    if (readableBytes > 0)
                    {
                        if (maxBytes - readableBytes < ioBufferSize && nioBufferCount != 0)
                        {
                            // If the nioBufferSize + readableBytes will overflow an Integer we stop populate the
                            // ByteBuffer array. This is done as bsd/osx don't allow to write more bytes then
                            // Integer.MAX_VALUE with one writev(...) call and so will return 'EINVAL', which will
                            // raise an IOException. On Linux it may work depending on the
                            // architecture and kernel but to be safe we also enforce the limit here.
                            // This said writing more the Integer.MAX_VALUE is not a good idea anyway.
                            //
                            // See also:
                            // - https://www.freebsd.org/cgi/man.cgi?query=write&sektion=2
                            // - http://linux.die.net/man/2/writev
                            break;
                        }
                        ioBufferSize += readableBytes;
                        int count = entry.Count;
                        if (count == -1)
                        {
                            entry.Count = count = buffer.IoBufferCount;
                        }
                        if (count == 1)
                        {
                            var nioBuf = entry.Buffer;
                            if (nioBuf.Array == null)
                            {
                                // cache ByteBuffer as it may need to create a new ByteBuffer instance if its a
                                // derived buffer
                                entry.Buffer = nioBuf = buffer.GetIoBuffer(readerIndex, readableBytes);
                            }
                            nioBuffers.Add(nioBuf);
                            nioBufferCount++;
                        }
                        else
                        {
                            var nioBufs = entry.Buffers;
                            if (nioBufs == null)
                            {
                                // cached ByteBuffers as they may be expensive to create in terms
                                // of Object allocation
                                entry.Buffers = nioBufs = buffer.GetIoBuffers();
                            }
                            for (int i = 0; i < nioBufs.Length && nioBufferCount < maxCount; i++)
                            {
                                ArraySegment<byte> nioBuf = nioBufs[i];
                                if (nioBuf.Array == null)
                                {
                                    break;
                                }
                                else if (nioBuf.Count == 0)
                                {
                                    continue;
                                }
                                nioBuffers.Add(nioBuf);
                                nioBufferCount++;
                            }
                        }
                        if (nioBufferCount == maxCount)
                        {
                            break;
                        }
                    }
                }
                entry = entry.Next;
            }
            this.nioBufferSize = ioBufferSize;

            return nioBuffers;
        }

        public long NioBufferSize => this.nioBufferSize;


        public bool IsWritable => this.unwritable == 0;


        public bool GetUserDefinedWritability(int index) => (this.unwritable & WritabilityMask(index)) == 0;

        public void SetUserDefinedWritability(int index, bool writable)
        {
            if (writable)
            {
                this.SetUserDefinedWritability(index);
            }
            else
            {
                this.ClearUserDefinedWritability(index);
            }
        }

        private void SetUserDefinedWritability(int index)
        {
            int mask = ~WritabilityMask(index);
            while (true)
            {
                int oldValue = this.unwritable;
                int newValue = oldValue & mask;
                if (Interlocked.CompareExchange(ref this.unwritable, newValue, oldValue) == oldValue)
                {
                    if (oldValue != 0 && newValue == 0)
                    {
                        this.FireChannelWritabilityChanged(true);
                    }
                    break;
                }
            }
        }

        private void ClearUserDefinedWritability(int index)
        {
            int mask = WritabilityMask(index);
            while (true)
            {
                int oldValue = this.unwritable;
                int newValue = oldValue | mask;
                if (Interlocked.CompareExchange(ref this.unwritable, newValue, oldValue) == oldValue)
                {
                    if (oldValue == 0 && newValue != 0)
                    {
                        this.FireChannelWritabilityChanged(true);
                    }
                    break;
                }
            }
        }

        private static int WritabilityMask(int index)
        {
            if (index < 1 || index > 31)
            {
                throw new InvalidOperationException("index: " + index + " (expected: 1~31)");
            }
            return 1 << index;
        }

        private void SetWritable(bool invokeLater)
        {
            while (true)
            {
                int oldValue = this.unwritable;
                int newValue = oldValue & ~1;
                if (Interlocked.CompareExchange(ref this.unwritable, newValue, oldValue) == oldValue)
                {
                    if (oldValue != 0 && newValue == 0)
                    {
                        this.FireChannelWritabilityChanged(invokeLater);
                    }
                    break;
                }
            }
        }

        private void SetUnwritable(bool invokeLater)
        {
            while (true)
            {
                int oldValue = this.unwritable;
                int newValue = oldValue | 1;
                if (Interlocked.CompareExchange(ref this.unwritable, newValue, oldValue) == oldValue)
                {
                    if (oldValue == 0 && newValue != 0)
                    {
                        this.FireChannelWritabilityChanged(invokeLater);
                    }
                    break;
                }
            }
        }

        private void FireChannelWritabilityChanged(bool invokeLater)
        {
            IChannelPipeline pipeline = this.channel.Pipeline;
            if (invokeLater)
            {
                this.channel.EventLoop.Execute(p => ((IChannelPipeline)p).FireChannelWritabilityChanged(), pipeline);
            }
            else
            {
                pipeline.FireChannelWritabilityChanged();
            }
        }

        public int Size => this.flushed;

        public bool IsEmpty => this.flushed == 0;

        public void FailFlushed(Exception cause, bool notify)
        {
            // Make sure that this method does not reenter.  A listener added to the current promise can be notified by the
            // current thread in the tryFailure() call of the loop below, and the listener can trigger another fail() call
            // indirectly (usually by closing the channel.)
            //
            // See https://github.com/netty/netty/issues/1501
            if (this.inFail) return;

            try
            {
                this.inFail = true;
                for (;;)
                {
                    if (!this.Remove0(cause, notify))
                    {
                        break;
                    }
                }
            }
            finally
            {
                this.inFail = false;
            }
        }

        private sealed class CloseChannelTask : IRunnable
        {
            private readonly ChannelOutboundBuffer buf;
            private readonly Exception cause;
            private readonly bool allowChannelOpen;

            public CloseChannelTask(ChannelOutboundBuffer buf, Exception cause, bool allowChannelOpen)
            {
                this.buf = buf;
                this.cause = cause;
                this.allowChannelOpen = allowChannelOpen;
            }

            public void Run() => this.buf.Close(this.cause, this.allowChannelOpen);
        }

        internal void Close(Exception cause, bool allowChannelOpen)
        {
            if (this.inFail)
            {
                this.channel.EventLoop.Execute(new CloseChannelTask(this, cause, allowChannelOpen));
                return;
            }

            this.inFail = true;

            if (!allowChannelOpen && this.channel.Open)
            {
                throw new InvalidOperationException("close() must be invoked after the channel is closed.");
            }

            if (!this.IsEmpty)
            {
                throw new InvalidOperationException("close() must be invoked after all flushed writes are handled.");
            }

            // Release all unflushed messages.
            try
            {
                Entry e = this.unflushedEntry;
                while (e != null)
                {
                    // Just decrease; do not trigger any events via DecrementPendingOutboundBytes()
                    int size = e.PendingSize;
                    Interlocked.Add(ref this.totalPendingSize, -size);

                    if (!e.Cancelled)
                    {
                        ReferenceCountUtil.SafeRelease(e.Message);
                        SafeFail(e.Promise, cause);
                    }
                    e = e.RecycleAndGetNext();
                }
            }
            finally
            {
                this.inFail = false;
            }
            ClearNioBuffers();
        }

        internal void Close(ClosedChannelException cause) => this.Close(cause, false);

        private static void SafeSuccess(TaskCompletionSource promise)
        {
            // TODO:ChannelPromise
            // Only log if the given promise is not of type VoidChannelPromise as trySuccess(...) is expected to return
            // false.
            Util.SafeSetSuccess(promise, Logger);
        }

        private static void SafeFail(TaskCompletionSource promise, Exception cause)
        {
            // TODO:ChannelPromise
            // Only log if the given promise is not of type VoidChannelPromise as tryFailure(...) is expected to return
            // false.
            Util.SafeSetFailure(promise, cause, Logger);
        }

        public long TotalPendingWriteBytes() => Volatile.Read(ref this.totalPendingSize);

        public long BytesBeforeUnwritable()
        {
            long bytes = this.channel.Configuration.WriteBufferHighWaterMark - this.totalPendingSize;
            // If bytes is negative we know we are not writable, but if bytes is non-negative we have to check writability.
            // Note that totalPendingSize and isWritable() use different volatile variables that are not synchronized
            // together. totalPendingSize will be updated before isWritable().
            if (bytes > 0)
            {
                return this.IsWritable ? bytes : 0;
            }
            return 0;
        }

        public long BytesBeforeWritable()
        {
            long bytes = this.totalPendingSize - this.channel.Configuration.WriteBufferLowWaterMark;
            // If bytes is negative we know we are writable, but if bytes is non-negative we have to check writability.
            // Note that totalPendingSize and isWritable() use different volatile variables that are not synchronized
            // together. totalPendingSize will be updated before isWritable().
            if (bytes > 0)
            {
                return this.IsWritable ? 0 : bytes;
            }
            return 0;
        }

        public void ForEachFlushedMessage(IMessageProcessor processor)
        {
            Contract.Requires(processor != null);

            Entry entry = this.flushedEntry;
            if (entry == null)
            {
                return;
            }

            do
            {
                if (!entry.Cancelled)
                {
                    if (!processor.ProcessMessage(entry.Message))
                    {
                        return;
                    }
                }
                entry = entry.Next;
            }
            while (this.IsFlushedEntry(entry));
        }

        private bool IsFlushedEntry(Entry e) => e != null && e != this.unflushedEntry;

        public interface IMessageProcessor
        {
            bool ProcessMessage(object msg);
        }

        private sealed class Entry : IRecycle
        {
            private static readonly ThreadLocalPool<Entry> Pool = new ThreadLocalPool<Entry>();
            public Entry Next;
            public object Message;
            public ArraySegment<byte>[] Buffers;
            public ArraySegment<byte> Buffer;
            public TaskCompletionSource Promise;
            public int PendingSize;
            public int Count = -1;
            public bool Cancelled;
            private IRecycleHandle<Entry> handle;

            public static Entry Acquire(object msg, int size, TaskCompletionSource promise)
            {
                var entry = Pool.Acquire(out var handle);
                entry.Message = msg;
                entry.PendingSize = size;
                entry.Promise = promise;
                entry.handle = handle;
                return entry;
            }

            public static void Recycle(Entry obj)
            {
                Pool.Recycle(obj.handle);
            }
            
            public int Cancel()
            {
                if (!this.Cancelled)
                {
                    this.Cancelled = true;
                    int pSize = this.PendingSize;

                    // release message and replace with an empty buffer
                    ReferenceCountUtil.SafeRelease(this.Message);
                    this.Message = ByteBuffer.Empty;

                    this.PendingSize = 0;
                    this.Buffers = null;
                    this.Buffer = new ArraySegment<byte>();
                    return pSize;
                }
                return 0;
            }

            void IRecycle.Recycle()
            {
                this.Next = null;
                this.Buffers = null;
                this.Buffer = new ArraySegment<byte>();
                this.Message = null;
                this.Promise = null;
                this.PendingSize = 0;
                this.Count = -1;
                this.Cancelled = false;
            }

            public Entry RecycleAndGetNext()
            {
                Entry next = this.Next;
                Recycle(this);
                return next;
            }
        }

        private sealed class ThreadLocalByteBufferList : FastThreadLocal<List<ArraySegment<byte>>>
        {
            protected override List<ArraySegment<byte>> GetInitialValue() => new List<ArraySegment<byte>>(1024);
        }
    }
}