using System;
using System.Diagnostics.Contracts;

namespace DotNetty.Common.Internal
{
    internal class DefaultPlatform : IPlatform
    {
        private readonly int processId;

        public DefaultPlatform()
        {
            // We cannot use System.Diagnostics.Process.GetCurrentProcess here because System.Diagnostics.Process
            // is not part of .Net Standard 1.3. Since a time-seeded random number is already used in DefaultChannelId 
            // (the only consumer of this API), we'll use the first 4 bytes of a UUID for better entropy

            var processGuid = Guid.NewGuid();
            processId = BitConverter.ToInt32(processGuid.ToByteArray(), 0);

            // DotNetty expects process id to be no greater than 0x400000, so clear this higher bits:
            processId = processId & 0x3FFFFF;
            Contract.Assert(processId <= 0x400000);
        }

        int IPlatform.GetCurrentProcessId() => processId;

        byte[] IPlatform.GetDefaultDeviceId() => MacAddressUtil.GetBestAvailableMac();
    }
}