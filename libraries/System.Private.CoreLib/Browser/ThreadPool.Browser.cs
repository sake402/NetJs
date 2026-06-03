using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        [NetJs.MemberReplace(nameof(MainThreadScheduleBackgroundJob))]
        internal static unsafe void MainThreadScheduleBackgroundJobImpl(void* callback)
        {
            var call = NetJs.Script.Write<Action>("callback." + NetJs.Constants.RefValueName);
            Global.SetTimeout(call, 1);
        }
    }
}
