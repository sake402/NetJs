using System;
using System.Runtime.InteropServices;

namespace NetJs
{
    [Reflectable(false)]
    public static class ReflectionExtension
    {
        //[Template("{i} & 0xFFFF")]
        //public extern static int TypeHandle(this uint i);
        //[Template("{i} >> 16")]
        //public extern static int AssemblyHandle(this uint i);
        [Template("(({value} & ({flag})) === ({flag}))")]
        public extern static bool TypeHasFlag(this Enum value, Enum flag);
        public static bool IsIntegerNumeric(this KnownTypeHandle value)
        {
            return value switch
            {
                KnownTypeHandle.SystemByte => true,
                KnownTypeHandle.SystemSByte => true,
                KnownTypeHandle.SystemChar => true,
                KnownTypeHandle.SystemInt16 => true,
                KnownTypeHandle.SystemUInt16 => true,
                KnownTypeHandle.SystemInt32 => true,
                KnownTypeHandle.SystemUint32 => true,
                KnownTypeHandle.SystemIntPtr => true,
                KnownTypeHandle.SystemUIntPtr => true,
                KnownTypeHandle.SystemInt64 => true,
                KnownTypeHandle.SystemUint64 => true,
                KnownTypeHandle.SystemEnum => true,
                _ => false
            };
        }
        public static bool IsLongIntegerNumeric(this KnownTypeHandle value)
        {
            return value switch
            {
                KnownTypeHandle.SystemInt64 => true,
                KnownTypeHandle.SystemUint64 => true,
                _ => false
            };
        }
        public static bool IsNumeric(this KnownTypeHandle value)
        {
            return value switch
            {
                KnownTypeHandle.SystemBool => true,
                KnownTypeHandle.SystemByte => true,
                KnownTypeHandle.SystemSByte => true,
                KnownTypeHandle.SystemChar => true,
                KnownTypeHandle.SystemInt16 => true,
                KnownTypeHandle.SystemUInt16 => true,
                KnownTypeHandle.SystemInt32 => true,
                KnownTypeHandle.SystemUint32 => true,
                KnownTypeHandle.SystemIntPtr => true,
                KnownTypeHandle.SystemUIntPtr => true,
                KnownTypeHandle.SystemInt64 => true,
                KnownTypeHandle.SystemUint64 => true,
                KnownTypeHandle.SystemSingle => true,
                KnownTypeHandle.SystemDouble => true,
                KnownTypeHandle.SystemEnum => true,
                _ => false
            };
        }

        public static bool IsPrimitive(this KnownTypeHandle value)
        {
            return value switch
            {
                KnownTypeHandle.SystemBool => true,
                KnownTypeHandle.SystemByte => true,
                KnownTypeHandle.SystemSByte => true,
                KnownTypeHandle.SystemChar => true,
                KnownTypeHandle.SystemInt16 => true,
                KnownTypeHandle.SystemUInt16 => true,
                KnownTypeHandle.SystemInt32 => true,
                KnownTypeHandle.SystemUint32 => true,
                KnownTypeHandle.SystemIntPtr => true,
                KnownTypeHandle.SystemUIntPtr => true,
                KnownTypeHandle.SystemInt64 => true,
                KnownTypeHandle.SystemUint64 => true,
                KnownTypeHandle.SystemSingle => true,
                KnownTypeHandle.SystemDouble => true,
                KnownTypeHandle.SystemString => true,
                KnownTypeHandle.SystemEnum => true,
                _ => false
            };
        }

        public static string GetOutputName(this MemberModel member)
        {
            var name = NetJs.Script.IsDefined(member.OutputName) ? member.OutputName!.NativeReplace("@", member.Name) : member.Name;
            return name;
        }

        public static uint ResolveHandle(this NetJs.Union<ulong, NetJs.NativeFunction<ulong>> handle, Type[]? typeArguments)
        {
            if (NetJs.Script.TypeOf(handle).NativeEquals("function"))
            {
                var args = typeArguments!.Map(t => t.As<RuntimeType>()._prototype);
                handle = NetJs.Script.Write<uint>("handle( ...args)");
            }
            return handle.As<uint>();
        }
    }
}
