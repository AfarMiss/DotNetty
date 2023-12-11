﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers.Tests
{
    using System;
    using Xunit;

    public abstract class AbstractByteBufferAllocatorTests : ByteBufferAllocatorTests
    {
        protected abstract IByteBufferAllocator NewUnpooledAllocator();

        protected override bool IsDirectExpected(bool preferDirect) => preferDirect;

        protected sealed override int DefaultMaxCapacity => AbstractByteBufferAllocator.DefaultMaxCapacity;

        protected sealed  override int DefaultMaxComponents =>  AbstractByteBufferAllocator.DefaultMaxComponents;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CalculateNewCapacity(bool preferDirect)
        {
            IByteBufferAllocator allocator = this.NewAllocator(preferDirect);
            Assert.Equal(8, allocator.CalculateNewCapacity(1, 8));
            Assert.Equal(7, allocator.CalculateNewCapacity(1, 7));
            Assert.Equal(64, allocator.CalculateNewCapacity(1, 129));

            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.CalculateNewCapacity(8, 7));
            Assert.Throws<ArgumentOutOfRangeException>(() => allocator.CalculateNewCapacity(-1, 8));
        }

        // [Fact]
        // public void UnsafeHeapBufferAndUnsafeDirectBuffer()
        // {
        //     IByteBufferAllocator allocator = this.NewUnpooledAllocator();
        //     IByteBuffer directBuffer = allocator.DirectBuffer();
        //     AssertInstanceOf<UnpooledUnsafeDirectByteBuffer>(directBuffer);
        //     directBuffer.Release();
        //
        //     IByteBuffer heapBuffer = allocator.HeapBuffer();
        //     AssertInstanceOf<HeapByteBuffer>(heapBuffer);
        //     heapBuffer.Release();
        // }

        protected static void AssertInstanceOf<T>(IByteBuffer buffer) where T : IByteBuffer
        {
            Assert.IsAssignableFrom<T>(buffer);
        }


        protected virtual long ExpectedUsedMemory(IByteBufferAllocator allocator, int capacity) => capacity;

        protected virtual long ExpectedUsedMemoryAfterRelease(IByteBufferAllocator allocator, int capacity) => 0;
    }
}
