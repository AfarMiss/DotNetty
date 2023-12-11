// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Mqtt.Packets
{
    using DotNetty.Buffers;
    using DotNetty.Common;

    public sealed class PublishPacket : PacketWithId, IByteBufferHolder
    {
        readonly QualityOfService qos;
        readonly bool duplicate;
        readonly bool retainRequested;

        public PublishPacket(QualityOfService qos, bool duplicate, bool retain)
        {
            this.qos = qos;
            this.duplicate = duplicate;
            this.retainRequested = retain;
        }

        public override PacketType PacketType => PacketType.PUBLISH;

        public override bool Duplicate => this.duplicate;

        public override QualityOfService QualityOfService => this.qos;

        public override bool RetainRequested => this.retainRequested;

        public string TopicName { get; set; }

        public IByteBuffer Payload { get; set; }

        public int ReferenceCount => this.Payload.ReferenceCount;

        public IReferenceCounted Retain(int increment = 1)
        {
            this.Payload.Retain(increment);
            return this;
        }

        public bool Release(int decrement = 1) => this.Payload.Release(decrement);

        IByteBuffer IByteBufferHolder.Content => this.Payload;

        public IByteBufferHolder Copy() => this.Replace(this.Payload.Copy());

        public IByteBufferHolder Replace(IByteBuffer content)
        {
            var result = new PublishPacket(this.qos, this.duplicate, this.retainRequested);
            result.TopicName = this.TopicName;
            result.Payload = content;
            return result;
        }

        IByteBufferHolder IByteBufferHolder.Duplicate() => this.Replace(this.Payload.Duplicate());

        public IByteBufferHolder RetainedDuplicate() => this.Replace(this.Payload.RetainedDuplicate());
    }
}