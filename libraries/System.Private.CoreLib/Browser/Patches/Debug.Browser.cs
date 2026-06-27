using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
    public static partial class Debug
    {
        //We are using native js interpolation in some places, so message can end up being a string already interpolated
        [NetJs.MemberReplace(nameof(Assert) + "(bool, ref AssertInterpolatedStringHandler)")]
        public static void AssertImpl([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument(nameof(condition))] ref AssertInterpolatedStringHandler message)
        {
            if (NetJs.Script.Write<bool>("typeof(message) == \"string\""))
            {
                Assert(condition, NetJs.Script.Write<string>("message"));
            }
            else
            {
                Assert(condition, message.ToStringAndClear());
            }
        }

        [NetJs.MemberReplace(nameof(Assert) + "(bool, ref AssertInterpolatedStringHandler, ref AssertInterpolatedStringHandler)")]
        public static void AssertImpl([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument(nameof(condition))] ref AssertInterpolatedStringHandler message, [InterpolatedStringHandlerArgument(nameof(condition))] ref AssertInterpolatedStringHandler detailMessage)
        {
            if (NetJs.Script.Write<bool>("typeof(message) == \"string\"") && NetJs.Script.Write<bool>("typeof(detailMessage) == \"string\""))
            {
                Assert(condition, NetJs.Script.Write<string>("message"), NetJs.Script.Write<string>("detailMessage"));
            }
            else
            {
                Assert(condition, message.ToStringAndClear(), detailMessage.ToStringAndClear());
            }
        }

    }
}
