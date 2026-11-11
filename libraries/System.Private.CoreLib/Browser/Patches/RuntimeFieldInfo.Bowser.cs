using NetJs;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(RuntimeFieldInfo))]
    [NetJs.Boot]
    //[NetJs.Reflectable(false)]
    internal sealed partial class RuntimeFieldInfo_Partial : ForcedPartialBase<RuntimeFieldInfo>
    {
        //internal FieldModel _model;
        internal RuntimeFieldInfo_Partial()
        {
        }

        internal RuntimeFieldInfo_Partial(FieldModel model)
        {
            THIS._model = model;
            NetJs.Script.Write("this.{nameof(System.Reflection.RuntimeFieldInfo.name)} = model.{nameof(NetJs.MemberModel.Name)}");
            //THIS.name = model.Name;
            //_model = model;
        }

        [NetJs.MemberReplace]
        internal object? UnsafeGetValue(object obj)
        {
            var fieldTypePrototype = AppDomain.GetType(THIS._model.As<FieldModel>().FieldType.As<uint>())!._prototype;
            if (THIS._model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic))
            {
                return NetJs.Script.Box(THIS.DeclaringType.As<RuntimeType>()._prototype[THIS._model.GetOutputName()], fieldTypePrototype);
            }
            return NetJs.Script.Box(obj![THIS._model.GetOutputName()], fieldTypePrototype);
        }

        [NetJs.MemberReplace]
        private object? GetValueInternal(object? obj)
        {
            var fieldTypePrototype = AppDomain.GetType(THIS._model.As<FieldModel>().FieldType.As<uint>())?._prototype;
            if (THIS._model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic))
            {
                var value = THIS.DeclaringType.As<RuntimeType>()._prototype[THIS._model.GetOutputName()];
                return fieldTypePrototype != null ? NetJs.Script.Box(value, fieldTypePrototype) : value;
            }
            var val = obj![THIS._model.GetOutputName()];
            return fieldTypePrototype != null ? NetJs.Script.Box(val, fieldTypePrototype) : val;
        }

        [NetJs.MemberReplace]
        private Type ResolveType()
        {
            return AppDomain.GetType(THIS._model.As<FieldModel>().FieldType.As<uint>())!;
        }

        [NetJs.MemberReplace]
        private Type GetParentType(bool declaring)
        {
            return AppDomain.GetType(THIS._model.DeclaringType.As<uint>())!;
        }

        [NetJs.MemberReplace]
        internal int GetFieldOffset()
        {
            throw new NotImplementedException();
        }

        [NetJs.MemberReplace]
        private static void SetValueInternal(FieldInfo fi, object? obj, object? value)
        {
            var field = fi.As<RuntimeFieldInfo>();
            if (field._model.Flags.TypeHasFlag(MemberFlagsModel.IsStatic))
            {
                field.DeclaringType.As<RuntimeType>()._prototype[field._model.Name] = NetJs.Script.Unbox(value);
            }
            obj![field._model.Name] = NetJs.Script.Unbox(value);
        }

        [NetJs.MemberReplace]
        public object GetRawConstantValue()
        {
            var prototype = AppDomain.GetType(THIS._model.As<FieldModel>().FieldType.As<uint>())!._prototype;
            return prototype![THIS._model.Name]!;
        }

        [NetJs.MemberReplace]
        internal static int get_metadata_token(RuntimeFieldInfo monoField)
        {
            return NetJs.Script.IsDefined(monoField._model.OriginalHandle) ? monoField._model.OriginalHandle.As<int>() : monoField._model.Handle.As<int>();
        }

        [NetJs.MemberReplace]
        private Type[] GetTypeModifiers(bool optional, int genericArgumentPosition = -1)
        {
            return Type.EmptyTypes;
        }

    }
}
