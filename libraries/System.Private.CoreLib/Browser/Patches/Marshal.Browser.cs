using NetJs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        //const uint MarshalledPointerFlag = 0x80000000;
        //static SimpleDictionary<object?> marsalTable = new SimpleDictionary<object?>();
        internal static unsafe IntPtr MarshalObject(void* value)
        {
            var rref = NetJs.Script.Ref<object>(value);
            return InteropUtility.castPtr2Address(rref).As<IntPtr>();
            //if (Script.TypeOf(value).NativeEquals("number"))
            //    return (IntPtr)value;
            //uint reference = MarshalledPointerFlag + Random.Shared.Next(1, 0x7FFFFFFF).As<uint>();
            //while (marsalTable.ContainsKey(reference))
            //{
            //    reference = MarshalledPointerFlag + Random.Shared.Next(1, 0x7FFFFFFF).As<uint>();
            //}
            //marsalTable[reference] = *(object*)value;
            //return reference.As<IntPtr>();
        }

        internal static IntPtr MarshalObject(object? value, IntPtr handle = 0, bool deleteOld = false)
        {
            //if (value == null)
            //return 0;
            if (Script.TypeOf(value).NativeEquals("number"))
                return value.As<IntPtr>();
            return InteropUtility.castObject2Address(value, handle.As<uint>(), deleteOld).As<IntPtr>();
            //if (handle == 0)
            //{
            //    handle = (MarshalledPointerFlag + Random.Shared.Next(1, 0x7FFFFFFF)).As<IntPtr>();
            //    while (marsalTable.ContainsKey(handle.As<int>()))
            //    {
            //        handle = (MarshalledPointerFlag + Random.Shared.Next(1, 0x7FFFFFFF)).As<IntPtr>();
            //    }
            //}
            ////var key = handle.ToString();
            //if (!deleteOld && marsalTable.ContainsKey(handle.As<int>()))
            //{
            //    throw new InvalidOperationException();
            //}
            //marsalTable[handle.As<int>()] = value;
            //return handle;
        }

        internal static object? MarshalObject(IntPtr value)
        {
            return InteropUtility.castAddress2Object(value.As<uint>());
            //return marsalTable[value.As<int>()];
        }

        internal static void Remove(IntPtr value)
        {
            InteropUtility.free(value.As<uint>());
            //marsalTable.Remove(value.As<int>());
        }

        static int lastPInvokeError;
        [NetJs.MemberReplace(nameof(GetLastPInvokeError))]
        public static int GetLastPInvokeErrorImpl()
        {
            return lastPInvokeError;
        }

        /// <summary>
        /// Set the last platform invoke error on the current thread
        /// </summary>
        /// <param name="error">Error to set</param>
        [NetJs.MemberReplace(nameof(SetLastPInvokeError))]
        public static void SetLastPInvokeErrorImpl(int error)
        {
            lastPInvokeError = error;
        }

        [NetJs.MemberReplace(nameof(DestroyStructure))]
        public static void DestroyStructureImpl(IntPtr ptr, Type structuretype)
        {

        }

        [NetJs.MemberReplace(nameof(OffsetOf))]
        public static IntPtr OffsetOfImpl(Type t, string fieldName)
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace(nameof(StructureToPtr) + "(object, IntPtr, bool)")]
        public static void StructureToPtrImpl(object structure, IntPtr ptr, bool fDeleteOld)
        {
            MarshalObject(structure, ptr, fDeleteOld);
        }

        [NetJs.MemberReplace(nameof(PtrToStructureHelper))]
        private static void PtrToStructureHelperImpl(IntPtr ptr, object structure, bool allowValueClasses)
        {

        }

        [NetJs.MemberReplace(nameof(GetDelegateForFunctionPointerInternal) + "(QCallTypeHandle, IntPtr, ObjectHandleOnStack)")]
        private static void GetDelegateForFunctionPointerInternalImpl(QCallTypeHandle t, IntPtr ptr, ObjectHandleOnStack res)
        {
            Delegate? d = null;
            if (InteropUtility.IsVirtualAddress(ptr.As<uint>()))
            {
                d = MarshalObject(ptr) as Delegate;
            }
            else
            {
                var methodInfo = AppDomain.GetMember((uint)ptr) as MethodInfo;
                if (methodInfo != null)
                {
                    //new JSFunctionDelegate()
                }
            }
            res.GetObjectHandleOnStack<Delegate?>() = d;
        }

        [NetJs.MemberReplace(nameof(GetFunctionPointerForDelegateInternal))]
        private static IntPtr GetFunctionPointerForDelegateInternalImpl(Delegate d)
        {
            if (d.Method != null)
            {
                return (IntPtr)d.Method.As<RuntimeMethodInfo>()._model.Handle;
            }
            return MarshalObject(d);
        }

        [NetJs.MemberReplace(nameof(PrelinkInternal))]
        private static void PrelinkInternalImpl(MethodInfo m)
        {

        }

        [NetJs.MemberReplace(nameof(SizeOfHelper))]
        private static int SizeOfHelperImpl(QCallTypeHandle t, bool throwIfNotMarshalable)
        {
            var type = t.QCallTypeHandleToRuntimeType();
            return CalculateSizeOf(type);
        }

        internal static int CalculateSizeOf(RuntimeType type)
        {
            if (!type.IsValueType)
                return IntPtr.Size;
            if (NetJs.Script.IsDefined(type._prototype.Size))
            {
                return type._prototype.Size;
            }
            int sz = 0;
            var fields = type.GetFields(BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                unchecked
                {
                    if (fields[i].FieldType != null && fields[i].FieldType.As<RuntimeType>().IsValueType)
                    {
                        sz += SizeOf(fields[i].FieldType);
                    }
                    else
                    {
                        sz += IntPtr.Size;
                    }
                }
            }
            return sz;
        }
    }
}
