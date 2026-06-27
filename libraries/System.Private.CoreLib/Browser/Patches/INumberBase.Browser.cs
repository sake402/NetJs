namespace System.Numerics
{
    [NetJs.ForcePartial(typeof(INumberBase<>))]
    public partial interface INumberBase_Partial<TSelf>
    {
        [NetJs.Name(NetJs.Constants.IsTypeName)]
        public static bool Is(object value)
        {
            var t = NetJs.Script.TypeOf(value);
            return t.NativeEquals("number") || t.NativeEquals("bigint");
        }
    }
}