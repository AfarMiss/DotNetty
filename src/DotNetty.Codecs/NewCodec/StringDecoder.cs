﻿using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codec;

namespace DotNetty.Codecs.NewCodec
{
    /// <summary>
    /// 解码器
    /// </summary>
    public class StringDecoder : Decoder<IByteBuffer>
    {
        private readonly Encoding encoding;

        public StringDecoder(Encoding encoding = null)
        {
            if (encoding == null) this.encoding = Encoding.GetEncoding(0);
            this.encoding = encoding ?? throw new NullReferenceException("encoding");
        }
        
        public override void Decode(object context, IByteBuffer input, List<object> output)
        {
            var decoded = this.Decode(input);
            output.Add(decoded);
        }

        private string Decode(IByteBuffer input) => input.ToString(this.encoding);
    }
}