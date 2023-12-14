// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace DotNetty.Codecs.Tests.Frame
{
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels.Embedded;
    using Xunit;

    public class LengthFieldBasedFrameDecoderTests
    {
        [Fact]
        public void FailSlowTooLongFrameRecovery()
        {
            var ch = new EmbeddedChannel(new LengthFieldBasedFrameDecoder(5, 0, 4, 0, 4, false));
            for (int i = 0; i < 2; i++)
            {
                var bytes = BitConverter.GetBytes(2);
                var bytes1 = BitConverter.GetBytes((short)0);

                var bytes2 = new byte[5];
                var memoryStream = new MemoryStream(bytes2);
                var writer = new BinaryWriter(memoryStream);
                writer.Write(1);
                writer.Write((byte)'A');
                Assert.False(ch.WriteInbound(ByteBuffer.WrappedBuffer(bytes)));
                Assert.Throws<TooLongFrameException>(() => ch.WriteInbound(ByteBuffer.WrappedBuffer(bytes1)));
                ch.WriteInbound(ByteBuffer.WrappedBuffer(bytes2));
                var buf = ch.ReadInbound<IByteBuffer>();
                Assert.Equal("A", buf.ToString(Encoding.UTF8));
                buf.Release();
            }
        }

        [Fact]
        public void TestFailFastTooLongFrameRecovery()
        {
            var ch = new EmbeddedChannel(new LengthFieldBasedFrameDecoder(5, 0, 4, 0, 4));

            for (int i = 0; i < 2; i++)
            {
                var directBuffer = ByteBuffer.Buffer(2);
                directBuffer.Write(2);
                var directBuffer1 = ByteBuffer.Buffer(7);
                directBuffer1.Write((short)0);
                directBuffer1.Write(1);
                directBuffer1.Write((byte)'A');
                
                Assert.Throws<TooLongFrameException>(() => ch.WriteInbound(ByteBuffer.WrappedBuffer(directBuffer)));

                ch.WriteInbound(ByteBuffer.WrappedBuffer(directBuffer1));
                var buf = ch.ReadInbound<IByteBuffer>();
                var s = buf.ToString(Encoding.UTF8);
                Assert.Equal("A", s);
                buf.Release();
            }
        }
    }
}