using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO
{
    public abstract partial class Stream
    {
        [NetJs.MemberReplace(nameof(HasOverriddenBeginEndRead))]
        private bool HasOverriddenBeginEndReadImpl()
        {
            return GetType().GetMethod(nameof(BeginEndReadAsync))!.DeclaringType != GetType();
        }

        [NetJs.MemberReplace(nameof(HasOverriddenBeginEndWrite))]
        private bool HasOverriddenBeginEndWriteImpl()
        {
            return GetType().GetMethod(nameof(BeginEndWriteAsync))!.DeclaringType != GetType();
        }
    }
}
