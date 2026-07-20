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

        [NetJs.MemberReplace(nameof(TryEnter) + "(object, int, ref bool)")]
        public static void TryEnterImpl(object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));
            //Cant wait on timeout in this port as it is single threaded and will block
            if (IsEntered(obj))
            {
                lockTaken = false;
            }
            else
            {
                Enter(obj);
                lockTaken = true;
            }
        }

        [NetJs.MemberReplace(nameof(Enter) + "(object, ref bool)")]
        public static void EnterImpl(object obj, ref bool lockTaken)
        {
            // TODO: Interpreter is missing this intrinsic
            if (lockTaken)
                throw new ArgumentException(SR.Argument_MustBeFalse, nameof(lockTaken));
            //Cant wait on timeout in this port as it is single threaded and will block
            if (IsEntered(obj))
            {
                lockTaken = false;
            }
            else
            {
                Enter(obj);
                lockTaken = true;
            }
        }


        [NetJs.MemberReplace(nameof(Enter)+"(object)")]
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
