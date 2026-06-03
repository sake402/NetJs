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
    public delegate T NativeFunction<T>();

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
    }
}
