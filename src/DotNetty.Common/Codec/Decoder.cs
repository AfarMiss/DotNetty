using System.Collections.Generic;

namespace DotNetty.Codec
{
    public abstract class Decoder<T>
    {
        public abstract void Decode(object context, T input, List<object> output);
    }
}