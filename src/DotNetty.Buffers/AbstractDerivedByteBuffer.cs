// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using DotNetty.Common;

    /// <inheritdoc />
    /// <summary>
    ///     Abstract base class for <see cref="T:DotNetty.Buffers.IByteBuffer" /> implementations that wrap another
    ///     <see cref="T:DotNetty.Buffers.IByteBuffer" />.
    /// </summary>
    public abstract class AbstractDerivedByteBuffer : AbstractByteBuffer
    {
        protected AbstractDerivedByteBuffer(int maxCapacity)
            : base(maxCapacity)
        {
        }

        public sealed override int ReferenceCount => this.ReferenceCount0();

        protected virtual int ReferenceCount0() => this.Unwrap().ReferenceCount;

        public sealed override IReferenceCounted Retain(int increment = 1) => this.Retain0(increment);

        protected virtual IByteBuffer Retain0(int increment)
        {
            this.Unwrap().Retain(increment);
            return this;
        }

        public sealed override bool Release(int decrement = 1) => this.Release0(decrement);

        protected virtual bool Release0(int decrement) => this.Unwrap().Release(decrement);

        public override ArraySegment<byte> GetIoBuffer(int index, int length) => this.Unwrap().GetIoBuffer(index, length);

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length) => this.Unwrap().GetIoBuffers(index, length);

    }
}