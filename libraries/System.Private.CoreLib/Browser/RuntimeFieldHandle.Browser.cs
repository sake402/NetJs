using NetJs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace System
{
    [NetJs.ForcePartial(typeof(RuntimeFieldHandle))]
    //[NetJs.Boot]
    //[NetJs.Reflectable(false)]
    //[NetJs.OutputOrder(int.MinValue + 1)] //make sure we emit this type immediately after AppDomain
    public partial struct RuntimeFieldHandle_Partial
    {
        [NetJs.MemberReplace]
        private static void SetValueInternal(FieldInfo fi, object? obj, object? value)
        {
            var model = fi.As<RuntimeFieldInfo>()._model.As<FieldModel>();
            if (model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic))
            {
                var prototype = AppDomain.GetType(model.FieldType.As<uint>())!._prototype;
                prototype![model.Name] = value;
            }
            obj![model.Name] = value;
        }

        [NetJs.MemberReplace]
        internal static  unsafe object GetValueDirect(RuntimeFieldInfo field, RuntimeType fieldType, void* pTypedRef, RuntimeType? contextType)
        {
            var obj = *(object*)pTypedRef;
            var model = field.As<RuntimeFieldInfo>()._model.As<FieldModel>();
            if (model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic))
            {
                var prototype = AppDomain.GetType(model.FieldType.As<uint>())!._prototype;
                return prototype![model.Name];
            }
            return obj![model.Name];
        }

        [NetJs.MemberReplace]
        internal static  unsafe void SetValueDirect(RuntimeFieldInfo field, RuntimeType fieldType, void* pTypedRef, object value, RuntimeType? contextType)
        {
            var obj = *(object*)pTypedRef;
            var model = field.As<RuntimeFieldInfo>()._model.As<FieldModel>();
            if (model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic))
            {
                var prototype = fieldType._prototype;
                prototype![model.Name] = value;
            }
            obj![model.Name] = value;
        }

    }
}
