using System;

namespace DotNetty.Common.Internal.Logging
{
    public static class InternalLoggerFactory
    {
        public static Func<string, IInternalLogger> Factory = name => new InternalDebugLogger(name);

        public static IInternalLogger GetInstance<T>() => GetInstance(typeof(T));
        public static IInternalLogger GetInstance(Type type) => GetInstance(type.FullName);
        public static IInternalLogger GetInstance(string name) => Factory(name);
    }
}