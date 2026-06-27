using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
    public partial class StackFrame
    {
        [NetJs.MemberReplace(nameof(GetFrameInfo))]
        private static  bool GetFrameInfoImpl(int skipFrames, bool needFileInfo,
                                        ObjectHandleOnStack out_method, ObjectHandleOnStack out_file,
                                        out int ilOffset, out int nativeOffset, out int line, out int column)
        {
            ilOffset = 0;
            nativeOffset = 0;
            line = 0;
            column = 0;
            return false;
        }

    }
}
