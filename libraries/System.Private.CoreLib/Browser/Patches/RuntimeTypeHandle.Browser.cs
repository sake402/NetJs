using NetJs;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System
{
    [NetJs.ForcePartial(typeof(RuntimeTypeHandle))]
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    //[NetJs.OutputOrder(int.MinValue + 1)] //make sure we emit this type immediately after AppDomain
    public partial struct RuntimeTypeHandle_Partial
    {
        [NetJs.MemberReplace]
        internal static CorElementType GetCorElementType(QCallTypeHandle type)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            if (runtimeType._prototype.IsGenericParameter())
            {
                return CorElementType.ELEMENT_TYPE_VAR;
                //return CorElementType.ELEMENT_TYPE_MVAR;
            }
            switch (runtimeType._prototype.KnownType)
            {
                case KnownTypeHandle.SystemString:
                    return CorElementType.ELEMENT_TYPE_STRING;
                case KnownTypeHandle.SystemArray:
                    return CorElementType.ELEMENT_TYPE_ARRAY;
                case KnownTypeHandle.SystemByte:
                    return CorElementType.ELEMENT_TYPE_U1;
                case KnownTypeHandle.SystemSByte:
                    return CorElementType.ELEMENT_TYPE_I1;
                case KnownTypeHandle.SystemInt16:
                    return CorElementType.ELEMENT_TYPE_I2;
                case KnownTypeHandle.SystemUInt16:
                    return CorElementType.ELEMENT_TYPE_U2;
                case KnownTypeHandle.SystemInt32:
                    return CorElementType.ELEMENT_TYPE_I4;
                case KnownTypeHandle.SystemUint32:
                    return CorElementType.ELEMENT_TYPE_U4;
                case KnownTypeHandle.SystemInt64:
                    return CorElementType.ELEMENT_TYPE_I8;
                case KnownTypeHandle.SystemUint64:
                    return CorElementType.ELEMENT_TYPE_U8;
                case KnownTypeHandle.SystemChar:
                    return CorElementType.ELEMENT_TYPE_CHAR;
                case KnownTypeHandle.SystemBool:
                    return CorElementType.ELEMENT_TYPE_BOOLEAN;
                case KnownTypeHandle.SystemSingle:
                    return CorElementType.ELEMENT_TYPE_R4;
                case KnownTypeHandle.SystemDouble:
                    return CorElementType.ELEMENT_TYPE_R8;
                case KnownTypeHandle.SystemPointer:
                    return CorElementType.ELEMENT_TYPE_PTR;
            }
            if (runtimeType._prototype!.Flags.TypeHasFlag(TypeFlagsModel.IsPointer))
            {
                return CorElementType.ELEMENT_TYPE_PTR;
            }
            return CorElementType.ELEMENT_TYPE_VOID;
        }

        [NetJs.MemberReplace]
        internal static TypeAttributes GetAttributes(QCallTypeHandle type)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            TypeAttributes att = TypeAttributes.NotPublic;
            if (runtimeType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsPublic))
            {
                att |= TypeAttributes.Public;
            }
            if (runtimeType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsSealed))
            {
                att |= TypeAttributes.Sealed;
            }
            if (runtimeType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsNested) && !runtimeType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsPublic))
            {
                att |= TypeAttributes.NestedPrivate;
            }
            if (runtimeType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsNestedPublic))
            {
                att |= TypeAttributes.NestedPublic;
            }
            if (runtimeType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsAbstract))
            {
                att |= TypeAttributes.Abstract;
            }
            if (runtimeType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsInterface))
            {
                att |= TypeAttributes.Interface;
            }
            return att;
        }

        [NetJs.MemberReplace]
        private static int GetMetadataToken(QCallTypeHandle type)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            return runtimeType._prototype.TypeHandle.As<int>();
        }

        [NetJs.MemberReplace]
        private static void GetGenericTypeDefinition_impl(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<Type?>() = runtimeType._parentGenericTypeDefinition;
        }

        [NetJs.MemberReplace]
        internal static bool HasInstantiation(QCallTypeHandle type)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            if (NetJs.Script.IsUndefined(runtimeType._prototype.Flags)) //boot type dont have complete model
                return false;
            return runtimeType._prototype.Flags.TypeHasFlag(TypeFlagsModel.IsGenericType);// && runtimeType._typeArguments != null;
        }

        [NetJs.MemberReplace]
        internal static bool IsInstanceOfType(QCallTypeHandle type, [NotNullWhen(true)] object? o)
        {
            if (o == null)
                return false;
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            return o.Is(runtimeType);
        }

        [NetJs.MemberReplace]
        internal static bool HasReferences(QCallTypeHandle type)
        {
            return true;
        }

        [NetJs.MemberReplace]
        internal static int GetArrayRank(QCallTypeHandle type)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            return runtimeType._arrayTypeRank;
        }

        [NetJs.MemberReplace]
        internal static void GetAssembly(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<Assembly?>() = runtimeType._assembly;
        }

        [NetJs.MemberReplace]
        internal static void GetElementType(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<Type?>() = runtimeType._arrayElementType;
        }

        [NetJs.MemberReplace]
        internal static void GetModule(QCallTypeHandle type, ObjectHandleOnStack res)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            res.GetObjectHandleOnStack<Module?>() = runtimeType._assembly.As<RuntimeAssembly_Partial>()._module.As<Module>();
        }

        [NetJs.MemberReplace]
        private static IntPtr GetMonoClass(QCallTypeHandle type)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            return (IntPtr)runtimeType._model.Handle;
        }

        [NetJs.MemberReplace]
        private static bool type_is_assignable_from(QCallTypeHandle a, QCallTypeHandle b)
        {
            var ra = a.QCallTypeHandleToRuntimeType();
            var rb = b.QCallTypeHandleToRuntimeType();
            return RuntimeType.IsAssignableInternal(ra, rb);
        }

        [NetJs.MemberReplace]
        internal static bool IsGenericTypeDefinition(QCallTypeHandle type)
        {
            var runtimeType = type.QCallTypeHandleToRuntimeType();
            return runtimeType.IsGenericType && runtimeType._typeArguments == null;
        }

        [NetJs.MemberReplace]
        internal static bool is_subclass_of(QCallTypeHandle childType, QCallTypeHandle baseType)
        {
            var _childType = childType.QCallTypeHandleToRuntimeType();
            var _baseType = baseType.QCallTypeHandleToRuntimeType();
            return RuntimeType.IsSubClassOfInternal(_childType, _baseType);
        }

        [NetJs.MemberReplace]
        internal static bool IsByRefLike(QCallTypeHandle type)
        {
            var mtype = type.QCallTypeHandleToRuntimeType();
            return mtype._model.Flags.TypeHasFlag(TypeFlagsModel.IsByRef);
        }

        [NetJs.MemberReplace]
        private static void internal_from_name(IntPtr name, ref StackCrawlMark stackMark, ObjectHandleOnStack res, bool throwOnError, bool ignoreCase)
        {
            var str = (string?)System.Runtime.InteropServices.Marshal.MarshalObject(name);
            var type = AppDomain.GetTypeInternal(str, ignoreCase, throwOnError);
            res.GetObjectHandleOnStack<Type?>() = type;
        }

    }
}
