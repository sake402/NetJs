using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs
{
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate void NativeAction();
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate void NativeAction<T1>(T1 arg);
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate void NativeAction<T1, T2>(T1 arg, T2 arg2);

    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate T NativeFunction<T>();
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate TRet NativeFunction<T1, TRet>(T1 arg);
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate TRet NativeFunction<T1, T2, TRet>(T1 arg1, T2 arg2);
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate TRet NativeFunction<T1, T2, T3, TRet>(T1 arg, T2 arg2, T3 arg3);
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate TRet NativeFunction<T1, T2, T3, T4, TRet>(T1 arg, T2 arg2, T3 arg3, T4 arg4);
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate TRet NativeFunction<T1, T2, T3, T4, T5, TRet>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate TRet NativeFunction<T1, T2, T3, T4, T5, T6, TRet>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate TRet NativeFunction<T1, T2, T3, T4, T5, T6, T7, TRet>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
    [NetJs.NativeDelegate]
    [NetJs.External]
    public delegate TRet NativeFunction<T1, T2, T3, T4, T5, T6, T7, T8, TRet>(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

    public static class NativeActionExtensions
    {
        [NetJs.IgnoreGeneric]
        public static void Invoke(this NativeAction action, object? thisArg = null)
        {
            NetJs.Script.Write("action.call(thisArg)");
        }
        [NetJs.IgnoreGeneric]
        public static T Invoke<T>(this NativeFunction<T> func, object? thisArg = null)
        {
            return NetJs.Script.Write<T>("func.call(thisArg)");
        }
        [NetJs.Template("{func}.length")]
        public static extern int NativeFunctionParametersCount(this Delegate func);
    }
}
