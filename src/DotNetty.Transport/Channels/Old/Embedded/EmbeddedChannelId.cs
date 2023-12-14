using System;

namespace DotNetty.Transport.Channels.Embedded
{
    /// <summary>
    ///     A dummy <see cref="IChannelId" /> implementation
    /// </summary>
    public sealed class EmbeddedChannelId : IChannelId
    {
        public static readonly EmbeddedChannelId Instance = new EmbeddedChannelId();

        private EmbeddedChannelId()
        {
        }

        public override int GetHashCode() => 0;

        public override bool Equals(object obj) => obj is EmbeddedChannelId;

        public int CompareTo(IChannelId other)
        {
            if (other is EmbeddedChannelId)
            {
                return 0;
            }
            return string.Compare(this.AsLongText(), other.AsLongText(), StringComparison.Ordinal);
        }

        public override string ToString() => "embedded";

        public string AsShortText() => this.ToString();

        public string AsLongText() => this.ToString();
    }
}