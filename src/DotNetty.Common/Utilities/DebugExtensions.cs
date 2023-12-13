using System.Collections.Generic;
using System.Text;

namespace DotNetty.Common.Utilities
{
    public static class DebugExtensions
    {
        public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                if (first)
                {
                    first = false;
                    sb.Append('{');
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append("{`").Append(pair.Key).Append("`: ").Append(pair.Value).Append('}');
            }
            return sb.Append('}').ToString();
        }
    }
}