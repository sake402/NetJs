using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    [NetJs.ForcePartial(typeof(BitConverter))]
    public static partial class BitConverter_Partial
    {
        [NetJs.MemberReplace]
        public static long DoubleToInt64Bits(double value)
        {
            var buffer = new Window.ArrayBuffer(8);
            var view = new Window.DataView(buffer);
            view.setFloat64(0, value);
            return view.getBigInt64(0, true);
        }

        [NetJs.MemberReplace]
        public static double Int64BitsToDouble(long value)
        {
            var buffer = new Window.ArrayBuffer(8);
            var view = new Window.DataView(buffer);
            view.setBigInt64(0, value);
            return view.getFloat64(0, true);
        }

        [NetJs.MemberReplace]
        public static ulong DoubleToUInt64Bits(double value)
        {
            var buffer = new Window.ArrayBuffer(8);
            var view = new Window.DataView(buffer);
            view.setFloat64(0, value);
            return view.getBigUint64(0, true);
        }

        [NetJs.MemberReplace]
        public static double UInt64BitsToDouble(ulong value)
        {
            var buffer = new Window.ArrayBuffer(8);
            var view = new Window.DataView(buffer);
            view.setBigUint64(0, value);
            return view.getFloat64(0, true);
        }

        [NetJs.MemberReplace]
        public static int SingleToInt32Bits(float value)
        {
            var buffer = new Window.ArrayBuffer(4);
            var view = new Window.DataView(buffer);
            view.setFloat32(0, value);
            return view.getInt32(0, true);
        }

        [NetJs.MemberReplace]
        public static float Int32BitsToSingle(int value)
        {
            var buffer = new Window.ArrayBuffer(4);
            var view = new Window.DataView(buffer);
            view.setInt32(0, value);
            return view.getFloat32(0, true);
        }

        [NetJs.MemberReplace]
        public static uint SingleToUInt32Bits(float value)
        {
            var buffer = new Window.ArrayBuffer(4);
            var view = new Window.DataView(buffer);
            view.setFloat32(0, value);
            return view.getUint32(0, true);
        }

        [NetJs.MemberReplace]
        public static float UInt32BitsToSingle(uint value)
        {
            var buffer = new Window.ArrayBuffer(4);
            var view = new Window.DataView(buffer);
            view.setUint32(0, value);
            return view.getFloat32(0, true);
        }

    }
}
