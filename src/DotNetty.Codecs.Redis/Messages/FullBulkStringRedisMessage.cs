// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Redis.Messages
{
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    public sealed class FullBulkStringRedisMessage : DefaultByteBufferHolder, IFullBulkStringRedisMessage
    {
        public static readonly IFullBulkStringRedisMessage Null = new NullFullBulkStringRedisMessage();
        public static readonly IFullBulkStringRedisMessage Empty = new EmptyFullBulkStringRedisMessage();

        public FullBulkStringRedisMessage(IByteBuffer content)
            : base(content)
        {
        }

        public bool IsNull => false;

        public override string ToString() => 
            new StringBuilder(StringUtil.SimpleClassName(this))
            .Append('[')
            .Append("content=")
            .Append(this.Content)
            .Append(']')
            .ToString();

        sealed class NullFullBulkStringRedisMessage : IFullBulkStringRedisMessage
        {
            public bool IsNull => true;

            public IByteBuffer Content => Unpooled.Empty;

            public IByteBufferHolder Copy() => this;

            public IByteBufferHolder Duplicate() => this;

            public IByteBufferHolder RetainedDuplicate() => this;

            public int ReferenceCount => 1;

            public IReferenceCounted Retain(int increment = 1) => this;


            public IByteBufferHolder Replace(IByteBuffer content) => this;

            public bool Release(int decrement = 1) => false;
        }

        sealed class EmptyFullBulkStringRedisMessage : IFullBulkStringRedisMessage
        {
            public bool IsNull => false;

            public IByteBuffer Content => Unpooled.Empty;

            public IByteBufferHolder Copy() => this;

            public IByteBufferHolder Duplicate() => this;

            public IByteBufferHolder RetainedDuplicate() => this;

            public int ReferenceCount => 1;

            public IReferenceCounted Retain(int increment = 1) => this;


            public IByteBufferHolder Replace(IByteBuffer content) => this;

            public bool Release(int decrement = 1) => false;
        }

        public override IByteBufferHolder Replace(IByteBuffer content) => new FullBulkStringRedisMessage(content);
    }
}