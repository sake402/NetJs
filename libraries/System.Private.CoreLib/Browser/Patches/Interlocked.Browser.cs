using NetJs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;


namespace System.Threading
{
    public static partial class Interlocked
    {
        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref int, int, int)")]
        public static int CompareExchangeImpl(ref int location1, int value, int comparand)
        {
            var v = location1;
            if (v == comparand)
            {
                location1 = value;
            }
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref object?, ref object?, ref object?, ref object?)")]
        private static void CompareExchangeImpl(ref object? location1, ref object? value, ref object? comparand, [NotNullIfNotNull(nameof(location1))] ref object? result)
        {
            var v = location1;
            if (v == comparand)
            {
                location1 = value;
            }
            result = v;
        }

        [NetJs.MemberReplace(nameof(Decrement) + "(ref int)")]
        public static int DecrementImpl(ref int location)
        {
            location--;
            return location;
        }

        [NetJs.MemberReplace(nameof(Decrement) + "(ref long)")]
        public static long DecrementImpl(ref long location)
        {
            location--;
            return location;
        }

        [NetJs.MemberReplace(nameof(Increment) + "(ref int)")]
        public static int IncrementImpl(ref int location)
        {
            location++;
            return location;
        }

        [NetJs.MemberReplace(nameof(Increment) + "(ref long)")]
        public static long IncrementImpl(ref long location)
        {
            location++;
            return location;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref byte, byte)")]
        public static byte ExchangeByteImpl(ref byte location1, byte value)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref sbyte, sbyte)")]
        public static sbyte ExchangeSByteImpl(ref sbyte location1, sbyte value)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref short, short)")]
        public static short ExchangeShortImpl(ref short location1, short value)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref ushort, ushort)")]
        public static ushort ExchangeUShortImpl(ref ushort location1, ushort value)
        {
            var v = location1;
            location1 = value;
            return v;
        }


        [NetJs.MemberReplace(nameof(Exchange) + "(ref uint, uint)")]
        public static uint ExchangeUIntImpl(ref uint location1, uint value)
        {
            var v = location1;
            location1 = value;
            return v;
        }


        [NetJs.MemberReplace(nameof(Exchange) + "(ref int, int)")]
        public static int ExchangeIntImpl(ref int location1, int value)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref ulong, ulong)")]
        public static ulong ExchangeUlongImpl(ref ulong location1, ulong value)
        {
            var v = location1;
            location1 = value;
            return v;
        }


        [NetJs.MemberReplace(nameof(Exchange) + "(ref float, float)")]
        public static float ExchangeFloatImpl(ref float location1, float value)
        {
            var v = location1;
            location1 = value;
            return v;
        }


        [NetJs.MemberReplace(nameof(Exchange) + "(ref double, double)")]
        public static double ExchangeDoubleImpl(ref double location1, double value)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref nint, nint)")]
        public static nint ExchangeNintImpl(ref nint location1, nint value)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref nuint, nuint)")]
        public static nuint ExchangeNUintImpl(ref nuint location1, nuint value)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref long, long)")]
        public static long ExchangeLongImpl(ref long location1, long value)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Exchange) + "(ref object?, ref object?, ref object?)")]
        private static void ExchangeObjectImpl([NotNullIfNotNull(nameof(value))] ref object? location1, ref object? value, [NotNullIfNotNull(nameof(location1))] ref object? result)
        {
            var v = location1;
            location1 = value;
            result = v;
        }


        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref sbyte, sbyte, sbyte)")]
        public static sbyte CompareExchangeSByteImpl(ref sbyte location1, sbyte value, sbyte comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref short, short, short)")]
        public static short CompareExchangeShortImpl(ref short location1, short value, short comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref byte, byte, byte)")]
        public static byte CompareExchangeByteImpl(ref byte location1, byte value, byte comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref ushort, ushort, ushort)")]
        public static ushort CompareExchangeUShortImpl(ref ushort location1, ushort value, ushort comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref uint, uint, uint)")]
        public static uint CompareExchangeUIntImpl(ref uint location1, uint value, uint comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref ulong, ulong, ulong)")]
        public static ulong CompareExchangeUlongImpl(ref ulong location1, ulong value, ulong comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref float, float, float)")]
        public static float CompareExchangeFloatImpl(ref float location1, float value, float comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref double, double, double)")]
        public static double CompareExchangeDoubleImpl(ref double location1, double value, double comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }


        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref nint, nint, nint)")]
        public static nint CompareExchangeNintImpl(ref nint location1, nint value, nint comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }


        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref nuint, nuint, nuint)")]
        public static nuint CompareExchangeNUintImpl(ref nuint location1, nuint value, nuint comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }


        [NetJs.MemberReplace(nameof(CompareExchange) + "(ref long, long, long)")]
        public static long CompareExchangeLongImpl(ref long location1, long value, long comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(CompareExchange) + "<>(ref T, T, T)")]
        public static T CompareExchangeTImpl<T>(ref T location1, T value, T comparand)
        {
            var v = location1;
            location1 = value;
            return v;
        }

        [NetJs.MemberReplace(nameof(Read) + "(ref long)")]
        public static long ReadImpl(ref long location)
        {
            return location;
        }

        [NetJs.MemberReplace(nameof(Add) + "(ref int, int)")]
        public static int AddImpl(ref int location1, int value)
        {
            location1 += value;
            return location1;
        }

        [NetJs.MemberReplace(nameof(Add) + "(ref long, long)")]
        public static long AddImpl(ref long location1, long value)
        {
            location1 += value;
            return location1;
        }

        [NetJs.MemberReplace(nameof(MemoryBarrierProcessWide))]
        public static void MemoryBarrierProcessWideImpl()
        {

        }
        

        [NetJs.MemberReplace(nameof(MemoryBarrier))]
        public static void MemoryBarrierImpl()
        {

        }
    }
}
