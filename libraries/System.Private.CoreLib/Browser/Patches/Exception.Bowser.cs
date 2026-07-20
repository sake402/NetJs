using NetJs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System
{
    public partial class Exception : Error
    {
        [NetJs.MemberReplace(nameof(GetStackTrace))]
        private string GetStackTraceImpl()
        {
            return stack;
        }

    }
}
