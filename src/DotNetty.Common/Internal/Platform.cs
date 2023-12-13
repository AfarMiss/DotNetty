using DotNetty.Common.Internal;

namespace DotNetty.Common
{
    public static class Platform
    {
        public static int GetCurrentProcessId() => PlatformProvider.Platform.GetCurrentProcessId();
        public static byte[] GetDefaultDeviceId() => PlatformProvider.Platform.GetDefaultDeviceId();
    }
}