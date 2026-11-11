using NetJs;
using System;
using System.Collections.Generic;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {        
        internal static partial long GetLowResolutionTimestamp()
        {
            int time = Script.Write<int>("Math.floor(performance.now())");
            return time;
        }
    }
}
