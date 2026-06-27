using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;

namespace System.Threading
{
    [NetJs.ForcePartial(typeof(Volatile))]
    public static partial class Volatile_Partial
    {
        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly bool)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern bool ReadBool([NotNullIfNotNull(nameof(location))] ref readonly bool location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref bool, bool)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteBool([NotNullIfNotNull(nameof(value))] ref bool location, bool value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly byte)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern byte ReadByte([NotNullIfNotNull(nameof(location))] ref readonly byte location);//=> location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref byte, byte)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteByte([NotNullIfNotNull(nameof(value))] ref byte location, byte value);//=> location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly double)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern double ReadDouble([NotNullIfNotNull(nameof(location))] ref readonly double location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref double, double)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteDouble([NotNullIfNotNull(nameof(value))] ref double location, double value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly short)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern short ReadShort([NotNullIfNotNull(nameof(location))] ref readonly short location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref short, short)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteShort([NotNullIfNotNull(nameof(value))] ref short location, short value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly int)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern int ReadInt([NotNullIfNotNull(nameof(location))] ref readonly int location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref int, int)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteInt([NotNullIfNotNull(nameof(value))] ref int location, int value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly long)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static long ReadLong([NotNullIfNotNull(nameof(location))] ref readonly long location) =>
#if TARGET_64BIT
        location;
#else
        location;
#endif

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref long, long)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static void WriteLong([NotNullIfNotNull(nameof(value))] ref long location, long value) =>
#if TARGET_64BIT
        location = value;
#else
        location = value;
#endif

        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly nint)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern nint ReadNInt([NotNullIfNotNull(nameof(location))] ref readonly nint location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref nint, nint)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteNInt([NotNullIfNotNull(nameof(value))] ref nint location, nint value);//=> location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly sbyte)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern sbyte ReadSByte([NotNullIfNotNull(nameof(location))] ref readonly sbyte location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref sbyte, sbyte)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteSByte([NotNullIfNotNull(nameof(value))] ref sbyte location, sbyte value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly float)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern float ReadFloat([NotNullIfNotNull(nameof(location))] ref readonly float location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref float, float)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteFloat([NotNullIfNotNull(nameof(value))] ref float location, float value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly ushort)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern ushort ReadUShort([NotNullIfNotNull(nameof(location))] ref readonly ushort location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref ushort, ushort)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteUShort([NotNullIfNotNull(nameof(value))] ref ushort location, ushort value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly uint)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern uint ReadUInt([NotNullIfNotNull(nameof(location))] ref readonly uint location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref uint, uint)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteUInt([NotNullIfNotNull(nameof(value))] ref uint location, uint value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly ulong)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern ulong ReadULong([NotNullIfNotNull(nameof(location))] ref readonly ulong location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref ulong, ulong)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteULong([NotNullIfNotNull(nameof(value))] ref ulong location, ulong value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "(ref readonly nuint)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern nuint ReadNUInt([NotNullIfNotNull(nameof(location))] ref readonly nuint location);// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "(ref nuint, nuint)")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteNUInt([NotNullIfNotNull(nameof(value))] ref nuint location, nuint value);// => location = value;


        [NetJs.MemberReplace(nameof(Volatile.Read) + "<>")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName)]
        public static extern T ReadT<T>([NotNullIfNotNull(nameof(location))] ref readonly T location) where T : class?;// => location;

        [NetJs.MemberReplace(nameof(Volatile.Write) + "<>")]
        [NetJs.Template("{location}." + NetJs.Constants.RefValueName + " = {value}")]
        public static extern void WriteT<T>([NotNullIfNotNull(nameof(value))] ref T location, T value) where T : class?;// => location = value;
        
        [NetJs.MemberReplace(nameof(Volatile.ReadBarrier) )]
        public static void ReadBarrierImpl() { }

        [NetJs.MemberReplace(nameof(Volatile.WriteBarrier) )]
        public static void WriteBarrierImpl() { }

    }
}
