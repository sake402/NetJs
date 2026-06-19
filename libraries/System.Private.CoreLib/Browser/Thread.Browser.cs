using System;
using System.Collections.Generic;
using System.Text;

namespace System.Threading
{
    public partial class Thread
    {
        static Thread _thread = new Thread();
        [NetJs.MemberReplace(nameof(StartInternal))]
        private static void StartInternalImpl(Thread runtimeThread, int stackSize)
        {
            if (runtimeThread != _thread)
                throw new PlatformNotSupportedException();
        }

        [NetJs.MemberReplace(nameof(GetCurrentOSThreadId))]
        private static ulong GetCurrentOSThreadIdImpl()
        {
            return 0;
        }
        
        [NetJs.MemberReplace(nameof(InitInternal))]
        private static void InitInternalImpl(Thread thread)
        {

        }

        [NetJs.MemberReplace(nameof(GetCurrentThread))]
        private static Thread GetCurrentThreadImpl()
        {
            return _thread;
        }

        [NetJs.MemberReplace(nameof(FreeInternal))]
        private void FreeInternalImpl()
        {

        }

        [NetJs.MemberReplace(nameof(GetState))]
        private static ThreadState GetStateImpl(Thread thread)
        {
            return ThreadState.Running;
        }

        [NetJs.MemberReplace(nameof(SetState))]
        private static void SetStateImpl(Thread thread, ThreadState set)
        {
            throw new PlatformNotSupportedException();
        }

        [NetJs.MemberReplace(nameof(ClrState))]
        private static void ClrStateImpl(Thread thread, ThreadState clr)
        {
            throw new PlatformNotSupportedException();
        }

        [NetJs.MemberReplace(nameof(GetName))]
        private static string GetNameImpl(Thread thread)
        {
            return "browser";
        }

        [NetJs.MemberReplace(nameof(SetName_icall))]
        private static unsafe void SetName_icallImpl(Thread thread, char* name, int nameLength)
        {
            throw new PlatformNotSupportedException();
        }

        [NetJs.MemberReplace(nameof(YieldInternal))]
        private static bool YieldInternalImpl()
        {
            throw new PlatformNotSupportedException();
        }

        [NetJs.MemberReplace(nameof(JoinInternal))]
        private static bool JoinInternalImpl(Thread thread, int millisecondsTimeout)
        {
            throw new PlatformNotSupportedException();
        }

        [NetJs.MemberReplace(nameof(InterruptInternal))]
        private static void InterruptInternalImpl(Thread thread)
        {
            throw new PlatformNotSupportedException();
        }

        [NetJs.MemberReplace(nameof(SetPriority))]
        private static void SetPriorityImpl(Thread thread, int priority)
        {
            throw new PlatformNotSupportedException();
        }

        //[NetJs.MemberReplace(nameof(CurrentThreadIsFinalizerThread))]
        //internal static bool CurrentThreadIsFinalizerThreadImpl()
        //{
        //    return false;
        //}
    }
}
