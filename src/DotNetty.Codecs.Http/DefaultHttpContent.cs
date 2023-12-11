// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertToAutoProperty
namespace DotNetty.Codecs.Http
{
    using System.Diagnostics.Contracts;
    using DotNetty.Buffers;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    public class DefaultHttpContent : DefaultHttpObject, IHttpContent
    {
        readonly IByteBuffer content;

        public DefaultHttpContent(IByteBuffer content)
        {
            Contract.Requires(content != null);

            this.content = content;
        }

        public IByteBuffer Content => this.content;

        public IByteBufferHolder Copy() => this.Replace(this.content.Copy());

        public IByteBufferHolder Duplicate() => this.Replace(this.content.Duplicate());

        public IByteBufferHolder RetainedDuplicate() => this.Replace(this.content.RetainedDuplicate());

        public virtual IByteBufferHolder Replace(IByteBuffer buffer) => new DefaultHttpContent(buffer);

        public int ReferenceCount => this.content.ReferenceCount;

        public IReferenceCounted Retain(int increment = 1)
        {
            this.content.Retain(increment);
            return this;
        }

        public bool Release(int decrement = 1) => this.content.Release(decrement);

        public override string ToString() => $"{StringUtil.SimpleClassName(this)} (data: {this.content}, decoderResult: {this.Result})";
    }
}
