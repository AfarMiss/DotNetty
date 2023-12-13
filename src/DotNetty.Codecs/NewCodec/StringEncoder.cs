using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codec;
using DotNetty.Transport.Channels;

namespace DotNetty.Codecs.NewCodec
{
    /// <summary>
    /// 编码器
    /// </summary>
    public class StringEncoder : Encoder<string>
    {
        private readonly Encoding encoding;

        public StringEncoder(Encoding encoding = null)
        {
            if (encoding == null) this.encoding = Encoding.GetEncoding(0);
            this.encoding = encoding ?? throw new NullReferenceException("encoding");
        }
        
        public override void Encode(object context, string input, List<object> output)
        {
            if (input.Length == 0) return;
            output.Add(ByteBufferUtil.EncodeString(((IChannelHandlerContext)context).Allocator, input, this.encoding));
        }
    }
}