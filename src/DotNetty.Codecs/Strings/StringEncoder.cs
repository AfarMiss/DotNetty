using System.Collections.Generic;
using System.Text;
using DotNetty.Transport.Channels;

namespace DotNetty.Codecs
{
    public class StringEncoder : MessageToMessageEncoder<string>
    {
        public override bool IsSharable => true;
        private readonly NewCodec.StringEncoder encoder;

        public StringEncoder(Encoding encoding = null) => this.encoder = new NewCodec.StringEncoder(encoding);

        protected internal override void Encode(IChannelHandlerContext context, string message, List<object> output)
        {
            this.encoder.Encode(context, message, output);
        }
    }
}