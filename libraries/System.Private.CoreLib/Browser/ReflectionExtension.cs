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

        public static void CollectConstructors(this TypePrototype prototype, ConstructorModel[] allCOnstructors)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Constructors))
            {
                allCOnstructors.Push(metadata!.Constructors!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && !basePrototype!.Equals(Object.Prototype))
            {
                CollectConstructors(basePrototype, allCOnstructors);
            }
        }

        public static void CollectMethods(this TypePrototype prototype, MethodModel[] allMethods)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Methods))
            {
                allMethods.Push(metadata!.Methods!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && !basePrototype!.Equals(Object.Prototype))
            {
                CollectMethods(basePrototype, allMethods);
            }
        }

        public static void CollectProperties(this TypePrototype prototype, PropertyModel[] allProperties)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Properties))
            {
                allProperties.Push(metadata!.Properties!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && !basePrototype!.Equals(Object.Prototype))
            {
                CollectProperties(basePrototype, allProperties);
            }
        }

        public static void CollectFields(this TypePrototype prototype, FieldModel[] allFields)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Fields))
            {
                allFields.Push(metadata!.Fields!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && !basePrototype!.Equals(Object.Prototype))
            {
                CollectFields(basePrototype, allFields);
            }
        }

        public static void CollectEvents(this TypePrototype prototype, EventModel[] allEvents)
        {
            var metadata = prototype.Metadata;
            if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Events))
            {
                allEvents.Push(metadata!.Events!);
            }
            var basePrototype = prototype.Base;
            if (NetJs.Script.IsDefined(basePrototype) && !basePrototype!.Equals(Object.Prototype))
            {
                CollectEvents(basePrototype, allEvents);
            }
        }

    }
}
