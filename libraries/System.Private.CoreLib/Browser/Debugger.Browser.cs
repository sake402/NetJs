using System;
using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics
{
    public static partial class Debugger
    {
        [NetJs.MemberReplace(nameof(IsAttached_internal))]
        private static bool IsAttached_internalImpl()
        {
            return false;
        }

        [NetJs.MemberReplace(nameof(Break))]
        public static void BreakImpl()
        {
            NetJs.Script.Write("debugger");
        }
        
        [NetJs.MemberReplace(nameof(IsLogging))]
        public static bool IsLoggingImpl()
        {
            return true;
        }
        
        [NetJs.MemberReplace(nameof(Log_icall))]
        private static void Log_icallImpl(int level, ref string category, ref string message)
        {
            Window.Window.console.log(message);
        }

    }
}
