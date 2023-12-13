using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace DotNetty.Codecs
{
    public class StringDecoder : MessageToMessageDecoder<IByteBuffer>
    {
        public override bool IsSharable => true;
        private readonly NewCodec.StringDecoder decoder;

        public StringDecoder(Encoding encoding = null) => this.decoder = new NewCodec.StringDecoder(encoding);

        protected internal override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            this.decoder.Decode(context, input, output);
        }
    }
}