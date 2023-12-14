using System;
using System.Text;
using DotNetty.Common.Internal.Logging;

namespace DotNetty.Common
{
    public static class MessageFormatter
    {
        public static string Format(InternalLogLevel level, string name, string message, Exception e)
        {
            var buf = new StringBuilder();
            buf.Append($"{name}: [{level}] -> {message} \nException:{e}");
            return buf.ToString();
        }

        public static string Format(InternalLogLevel level, string name, string message, params object[] arguments)
        {
            var buf = new StringBuilder();
            if (string.IsNullOrEmpty(message) || arguments.Length <= 0)
            {
                buf.Append($"{name}: [{level}] -> {message}");
            }
            else if (message.IndexOf("{}", StringComparison.Ordinal) >= 0)
            {
                foreach (var obj in arguments)
                {
                    message = message.Replace("{}", obj.ToString());
                }

                buf.Append($"{name}: [{level}] -> {message}");
            }
            else
            {
                buf.Append($"{name}: [{level}] -> {string.Format(message, arguments)}");
            }
            return buf.ToString();
        }
    }
}