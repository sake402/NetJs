namespace System.Numerics
{
    [NetJs.ForcePartial(typeof(INumberBase<>))]
    public partial interface INumberBase_Partial<TSelf>
    {
        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object value, out object? result)
        {
            result = NetJs.Script.Undefined;
            var type = NetJs.Script.TypeOf(value);
            var tSelfPrototype = NetJs.Script.GetPrototype<TSelf>();
            if ((type.NativeEquals("number") || type.NativeEquals("bigint")) && tSelfPrototype.KnownType.IsNumeric())
                return true;
            //if ((t.NativeEquals("number") && tSelfPrototype.KnownType.IsNumeric()) ||
            //    (t.NativeEquals("bigint") && tSelfPrototype.KnownType.IsLongIntegerNumeric()))
            //    return true;
            if (type.NativeEquals("object"))
            {
                var valueType = value.GetType().As<RuntimeType>();
                var valueTypePrototype = valueType._prototype;
                if (valueTypePrototype.Kind == TypeKindModel.Enum && tSelfPrototype.KnownType.IsNumeric())
                {
                    var unboxedValue = NetJs.Script.Unbox(value);
                    result = unboxedValue;
                    return true;
                }
            }
            return false;
        }
    }
}