using System.Collections.Generic;

namespace DotNetty.Codec
{
    public abstract class Encoder<T>
    {
        public abstract void Encode(object context, T input, List<object> output);
    }
}