namespace System.Numerics
{
    [NetJs.ForcePartial(typeof(INumberBase<>))]
    public partial interface INumberBase_Partial<TSelf>
    {
        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object value, NativeAction<object>? result)
        {
            var unboxedValue = NetJs.Script.Unbox(value);
            var type = NetJs.Script.TypeOf(unboxedValue);
            var tSelfPrototype = NetJs.Script.GetPrototype<TSelf>();
            if ((type.NativeEquals("number") || type.NativeEquals("bigint")) && tSelfPrototype.KnownType.IsNumeric())
            {
                if (NetJs.Script.IsDefined(result) && result != null)
                    result(unboxedValue);
                return true;
            }
            //if ((t.NativeEquals("number") && tSelfPrototype.KnownType.IsNumeric()) ||
            //    (t.NativeEquals("bigint") && tSelfPrototype.KnownType.IsLongIntegerNumeric()))
            //    return true;
            if (type.NativeEquals("object"))
            {
                var valueTypePrototype = Object.GetClassPrototypeOf(value);
                if (valueTypePrototype.Kind == TypeKindModel.Enum && tSelfPrototype.KnownType.IsNumeric())
                {
                    if (NetJs.Script.IsDefined(result) && result != null)
                        result(unboxedValue);
                    return true;
                }
            }
            return false;
        }
    }
}