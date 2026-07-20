using NetJs;
using System;
using System.Collections.Generic;
using System.Text;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {        
        internal static partial long GetSystemTimeAsTicks()
        {
            int jsMilliseconds = Script.Write<int>("Date.now()");
            long currentNetTicks =  jsMilliseconds * TimeSpan.TicksPerMillisecond;
            return currentNetTicks;
        }

    }
}
