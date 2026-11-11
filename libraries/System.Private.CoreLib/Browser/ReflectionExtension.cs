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
            return
                value == KnownTypeHandle.SystemByte ||
                value == KnownTypeHandle.SystemSByte ||
                value == KnownTypeHandle.SystemChar ||
                value == KnownTypeHandle.SystemInt16 ||
                value == KnownTypeHandle.SystemUInt16 ||
                value == KnownTypeHandle.SystemInt32 ||
                value == KnownTypeHandle.SystemUInt32 ||
                value == KnownTypeHandle.SystemIntPtr ||
                value == KnownTypeHandle.SystemUIntPtr ||
                value == KnownTypeHandle.SystemInt64 ||
                value == KnownTypeHandle.SystemUInt64 ||
                value == KnownTypeHandle.SystemEnum;
        }
        public static bool IsLongIntegerNumeric(this KnownTypeHandle value)
        {
            return value == KnownTypeHandle.SystemInt64 ||
               value == KnownTypeHandle.SystemUInt64;
        }
        public static bool IsNumeric(this KnownTypeHandle value)
        {
            return
                value == KnownTypeHandle.SystemBool ||
                value == KnownTypeHandle.SystemByte ||
                value == KnownTypeHandle.SystemSByte ||
                value == KnownTypeHandle.SystemChar ||
                value == KnownTypeHandle.SystemInt16 ||
                value == KnownTypeHandle.SystemUInt16 ||
                value == KnownTypeHandle.SystemInt32 ||
                value == KnownTypeHandle.SystemUInt32 ||
                value == KnownTypeHandle.SystemIntPtr ||
                value == KnownTypeHandle.SystemUIntPtr ||
                value == KnownTypeHandle.SystemInt64 ||
                value == KnownTypeHandle.SystemUInt64 ||
                value == KnownTypeHandle.SystemSingle ||
                value == KnownTypeHandle.SystemDouble ||
                value == KnownTypeHandle.SystemEnum;
        }

        public static bool IsPrimitive(this KnownTypeHandle value)
        {
            return 
                value == KnownTypeHandle.SystemBool ||
                value == KnownTypeHandle.SystemByte ||
                value == KnownTypeHandle.SystemSByte ||
                value == KnownTypeHandle.SystemChar ||
                value == KnownTypeHandle.SystemInt16 ||
                value == KnownTypeHandle.SystemUInt16 ||
                value == KnownTypeHandle.SystemInt32 ||
                value == KnownTypeHandle.SystemUInt32 ||
                value == KnownTypeHandle.SystemIntPtr ||
                value == KnownTypeHandle.SystemUIntPtr ||
                value == KnownTypeHandle.SystemInt64 ||
                value == KnownTypeHandle.SystemUInt64 ||
                value == KnownTypeHandle.SystemSingle ||
                value == KnownTypeHandle.SystemDouble ||
                value == KnownTypeHandle.SystemString ||
                value == KnownTypeHandle.SystemEnum;
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
                var args = typeArguments!.map(t => t.As<RuntimeType>()._prototype);
                handle = NetJs.Script.Write<uint>("handle( ...args)");
            }
            return handle.As<uint>();
        }

        public static void CollectConstructors(this TypePrototype prototype, ConstructorModel[] allConstructors)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Constructors))
            {
                allConstructors.push(metadata!.Constructors!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && basePrototype != Object.Prototype)
            {
                CollectConstructors(basePrototype!, allConstructors);
            }
        }

        public static void CollectMethods(this TypePrototype prototype, MethodModel[] allMethods)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Methods))
            {
                allMethods.push(metadata!.Methods!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && basePrototype != Object.Prototype)
            {
                CollectMethods(basePrototype!, allMethods);
            }
        }

        public static void CollectProperties(this TypePrototype prototype, PropertyModel[] allProperties)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Properties))
            {
                allProperties.push(metadata!.Properties!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && basePrototype != Object.Prototype)
            {
                CollectProperties(basePrototype!, allProperties);
            }
        }

        public static void CollectFields(this TypePrototype prototype, FieldModel[] allFields)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Fields))
            {
                allFields.push(metadata!.Fields!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && basePrototype != Object.Prototype)
            {
                CollectFields(basePrototype!, allFields);
            }
        }

        public static void CollectEvents(this TypePrototype prototype, EventModel[] allEvents)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Events))
            {
                allEvents.push(metadata!.Events!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && basePrototype != Object.Prototype)
            {
                CollectEvents(basePrototype!, allEvents);
            }
        }

    }
}
