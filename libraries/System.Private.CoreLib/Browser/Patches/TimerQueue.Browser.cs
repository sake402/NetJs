using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading
{
    internal partial class TimerQueue
    {
        [NetJs.MemberReplace(nameof(MainThreadScheduleTimer))]
        private static unsafe void MainThreadScheduleTimerImpl(void* callback, int shortestDueTimeMs)
        {
            var call = NetJs.Script.Write<NativeAction>("callback." + Constants.RefValueName);
            Global.SetTimeout(call, shortestDueTimeMs);
        }
    }
}
