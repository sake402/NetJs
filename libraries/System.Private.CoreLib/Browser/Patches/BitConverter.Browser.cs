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
            var view = RuntimeHelpers.GetReUsableDataView();
            view.setFloat64(0, value, BitConverter.IsLittleEndian);
            return view.getBigInt64(0, BitConverter.IsLittleEndian);
        }

        [NetJs.MemberReplace]
        public static double Int64BitsToDouble(long value)
        {
            var view = RuntimeHelpers.GetReUsableDataView();
            view.setBigInt64(0, value, BitConverter.IsLittleEndian);
            return view.getFloat64(0, BitConverter.IsLittleEndian);
        }

        [NetJs.MemberReplace]
        public static ulong DoubleToUInt64Bits(double value)
        {
            var view = RuntimeHelpers.GetReUsableDataView();
            view.setFloat64(0, value, BitConverter.IsLittleEndian);
            return view.getBigUint64(0, BitConverter.IsLittleEndian);
        }

        [NetJs.MemberReplace]
        public static double UInt64BitsToDouble(ulong value)
        {
            var view = RuntimeHelpers.GetReUsableDataView();
            view.setBigUint64(0, value, BitConverter.IsLittleEndian);
            return view.getFloat64(0, BitConverter.IsLittleEndian);
        }

        [NetJs.MemberReplace]
        public static int SingleToInt32Bits(float value)
        {
            var view = RuntimeHelpers.GetReUsableDataView();
            view.setFloat32(0, value, BitConverter.IsLittleEndian);
            return view.getInt32(0, BitConverter.IsLittleEndian);
        }

        [NetJs.MemberReplace]
        public static float Int32BitsToSingle(int value)
        {
            var view = RuntimeHelpers.GetReUsableDataView();
            view.setInt32(0, value, BitConverter.IsLittleEndian);
            return view.getFloat32(0, BitConverter.IsLittleEndian);
        }

        [NetJs.MemberReplace]
        public static uint SingleToUInt32Bits(float value)
        {
            var view = RuntimeHelpers.GetReUsableDataView();
            view.setFloat32(0, value, BitConverter.IsLittleEndian);
            return view.getUint32(0, BitConverter.IsLittleEndian);
        }

        [NetJs.MemberReplace]
        public static float UInt32BitsToSingle(uint value)
        {
            var view = RuntimeHelpers.GetReUsableDataView();
            view.setUint32(0, value, BitConverter.IsLittleEndian);
            return view.getFloat32(0, BitConverter.IsLittleEndian);
        }

    }
}
