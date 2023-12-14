using System.Diagnostics.Contracts;
using System.Threading;

namespace DotNetty.Common.Internal
{
    public static class PlatformProvider
    {
        private static IPlatform defaultPlatform;

        public static IPlatform Platform
        {
            get
            {
                IPlatform platform = Volatile.Read(ref defaultPlatform);
                if(platform == null)
                {
                    platform = new DefaultPlatform();
                    IPlatform current = Interlocked.CompareExchange(ref defaultPlatform, platform, null);
                    if (current != null)
                    {
                        return current;
                    }
                }
                return platform;
            }

            set
            {
                Contract.Requires(value != null);
                Volatile.Write(ref defaultPlatform, value);
            }
        }
    }
}