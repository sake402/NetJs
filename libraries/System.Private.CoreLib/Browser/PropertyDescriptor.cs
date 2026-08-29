namespace NetJs
{
    [NetJs.External]
    [NetJs.ObjectLiteral]
    [NetJs.Convention(NetJs.Notation.CamelCase)]
    public class PropertyDescriptor
    {
        public bool? Configurable { get; set; }
        public bool? Enumerable { get; set; }
        public object? Value { get; set; }
        public bool? Writable { get; set; }
        public NativeFunction<object>? Get { get; set; }
        public NativeAction<object>? Set { get; set; }
    }
}
