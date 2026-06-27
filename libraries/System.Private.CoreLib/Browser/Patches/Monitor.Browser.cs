using NetJs;
using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading
{
    public static partial class Monitor
    {
        [NetJs.MemberReplace(nameof(IsEntered))]
        public static bool IsEnteredImpl(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return NetJs.Script.Write<bool>("obj[\"$monitor_entered\"] == true");
        }

        [NetJs.MemberReplace(nameof(Enter))]
        public static void EnterImpl(object obj)
        {
            obj["$monitor_entered"] = true.As<object>();
        }

        [NetJs.MemberReplace(nameof(Exit))]
        public static void ExitImpl(object obj)
        {
            obj["$monitor_entered"] = false.As<object>();
        }

        [NetJs.MemberReplace(nameof(InternalExit))]
        private static void InternalExitImpl(object obj)
        {
            obj["$monitor_entered"] = false.As<object>();
        }

        [NetJs.MemberReplace(nameof(Monitor_pulse))]
        private static void Monitor_pulseImpl(object obj)
        {
        }

        [NetJs.MemberReplace(nameof(Monitor_pulse_all))]
        private static void Monitor_pulse_allImpl(object obj)
        {

        }
        
        [NetJs.MemberReplace(nameof(Monitor_wait))]
        internal static bool Monitor_waitImpl(object obj, int ms, bool allowInterruption)
        {
            return true;
        }

        [NetJs.MemberReplace(nameof(try_enter_with_atomic_var))]
        internal static void try_enter_with_atomic_varImpl(object obj, int millisecondsTimeout, bool allowInterruption, ref bool lockTaken)
        {

        }

        [NetJs.MemberReplace(nameof(Monitor_get_lock_contention_count))]
        private static long Monitor_get_lock_contention_countImpl()
        {
            return 0;
        }

    }
}
