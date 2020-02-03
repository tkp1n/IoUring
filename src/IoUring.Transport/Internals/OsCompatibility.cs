using System;
using System.Runtime.InteropServices;

namespace IoUring.Transport.Internals
{
    internal static class OsCompatibility
    {
        // Enforce Linux with kernel version >= 5.4
        public static bool IsCompatible
        {
            get
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return false;
                
                var osVersion = Environment.OSVersion.Version;
                if (osVersion.Major > 5) return true;
                return osVersion.Major == 5 && osVersion.Minor >= 4;
            }
        }
    }
}
