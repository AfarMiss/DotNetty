using DotNetty.Buffers;

namespace DotNetty.Transport.Channels
{
    public abstract class AbstractRecvByteBufAllocator : IRecvByteBufAllocator
    {
        private volatile int maxMessagesPerRead;

        public int MaxMessagesPerRead
        {
            get => this.maxMessagesPerRead;
            set => this.maxMessagesPerRead = value;
        }
        
        protected AbstractRecvByteBufAllocator(int maxMessagesPerRead = 1) => this.MaxMessagesPerRead = maxMessagesPerRead;

        public abstract IRecvByteBufAllocatorHandle NewHandle();

        protected abstract class MaxMessageHandle<T> : IRecvByteBufAllocatorHandle where T : IRecvByteBufAllocator
        {
            private readonly T owner;
            private IChannelConfiguration config;
            /// 每次读的最大消息数
            private int maxMessagePerRead;
            /// 总共读了多少次消息
            private int totalMessages;
            /// 总共读的字节数
            private int totalBytesRead;
            /// 上一次读的字节数
            private int lastBytesRead;

            protected MaxMessageHandle(T owner) => this.owner = owner;

            public abstract int Guess();

            public void Reset(IChannelConfiguration config)
            {
                this.config = config;
                this.maxMessagePerRead = this.owner.MaxMessagesPerRead;
                this.totalMessages = this.totalBytesRead = 0;
            }

            public IByteBuffer Allocate(IByteBufferAllocator alloc) => alloc.Buffer(this.Guess());

            public void IncMessagesRead(int amt) => this.totalMessages += amt;

            public int LastBytesRead
            {
                get => this.lastBytesRead;
                set
                {
                    // 记录上次读取的字节数
                    this.lastBytesRead = value;
                    this.totalBytesRead += value;
                    if (this.totalBytesRead < 0)
                    {
                        this.totalBytesRead = int.MaxValue;
                    }
                }
            }

            public virtual bool ContinueReading()
            {
                return this.config.AutoRead
                    && this.AttemptedBytesRead == this.lastBytesRead
                    //没超过最大读取消息数
                    && this.totalMessages < this.maxMessagePerRead
                    && this.totalBytesRead < int.MaxValue;
            }

            public virtual void ReadComplete()
            {
            }

            public virtual int AttemptedBytesRead { get; set; }

            protected int TotalBytesRead() => this.totalBytesRead;
        }
    }
}