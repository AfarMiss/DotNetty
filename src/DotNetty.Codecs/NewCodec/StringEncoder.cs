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

        public StringEncoder(Encoding encoding = null) => this.encoding = encoding ?? Encoding.GetEncoding(0);

        public override void Encode(object context, string input, List<object> output)
        {
            if (input.Length == 0) return;
            output.Add(ByteBufferUtil.EncodeString(((IChannelHandlerContext)context).Allocator, input, this.encoding));
        }
    }
}