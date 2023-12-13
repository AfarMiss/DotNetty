using System.Diagnostics.Contracts;
using System.Net;
using DotNetty.Common;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public class DefaultAddressedEnvelope<T> : IAddressedEnvelope<T>
    {
        public T Content { get; }
        public EndPoint Sender { get; }
        public EndPoint Recipient { get; }

        public int ReferenceCount
        {
            get
            {
                var counted = this.Content as IReferenceCounted;
                return counted?.ReferenceCount ?? 1;
            }
        }
        
        public DefaultAddressedEnvelope(T content, EndPoint recipient) : this(content, null, recipient)
        {
        }

        public DefaultAddressedEnvelope(T content, EndPoint sender, EndPoint recipient)
        {
            Contract.Requires(content != null);
            Contract.Requires(sender != null || recipient != null);

            this.Content = content;
            this.Sender = sender;
            this.Recipient = recipient;
        }

        public virtual IReferenceCounted Retain(int increment = 1)
        {
            ReferenceCountUtil.Retain(this.Content, increment);
            return this;
        }

        public bool Release(int decrement = 1) => ReferenceCountUtil.Release(this.Content, decrement);

        public override string ToString() => $"{nameof(DefaultAddressedEnvelope<T>)}"
            + (this.Sender != null ? $"({this.Sender} => {this.Recipient}, {this.Content})" : $"(=> {this.Recipient}, {this.Content})");
    }
}