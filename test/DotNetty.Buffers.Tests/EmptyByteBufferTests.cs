﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers.Tests
{
    using System;
    using Xunit;

    public class EmptyByteBufferTests
    {
        [Fact]
        public void IsWritable()
        {
            var empty = new EmptyByteBuffer(ByteBufferAllocator.Default);
            Assert.False(empty.IsWritable());
            Assert.False(empty.IsWritable(1));
        }

        [Fact]
        public void WriteEmptyByteBuf()
        {
            var empty = new EmptyByteBuffer(ByteBufferAllocator.Default);
            empty.WriteBytes(Unpooled.Empty); // Ok
            IByteBuffer nonEmpty = ByteBufferAllocator.Default.Buffer();
            nonEmpty.Write<bool>(false);
            Assert.Throws<IndexOutOfRangeException>(() => empty.WriteBytes(nonEmpty));
            nonEmpty.Release();
        }

        [Fact]
        public void IsReadable()
        {
            var empty = new EmptyByteBuffer(ByteBufferAllocator.Default);
            Assert.False(empty.IsReadable());
            Assert.False(empty.IsReadable(1));
        }

        [Fact]
        public void Array()
        {
            var empty = new EmptyByteBuffer(ByteBufferAllocator.Default);
            Assert.True(empty.HasArray);
            Assert.Empty(empty.Array);
            Assert.Equal(0, empty.ArrayOffset);
        }

        [Fact]
        public void MemoryAddress()
        {
            var empty = new EmptyByteBuffer(ByteBufferAllocator.Default);
            Assert.False(empty.HasMemoryAddress);
            Assert.Throws<NotSupportedException>(() => empty.GetPinnableMemoryAddress());
        }
    }
}
