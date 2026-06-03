using NetJs;

namespace System.Reflection
{
    /// <summary>
    /// For types that needs to reference itself(defined via typeproxy)
    /// This is simply a JS proxy handler that forwards request to the proxy to the System.Type/Prototype itself
    /// </summary>
    [Boot]
    [Reflectable(false)]
    class TypeProxyHandler
    {
        public TypeProxyHandler(string fullName)
        {
            FullName = fullName;
        }

        [Name(Constants.PrototypeFullName)]
        internal string FullName { get; }
        /// <summary>
        /// The finally created type we will proxy to
        /// </summary>
        internal Type? TargetType { get; set; }
        internal TypePrototype? Prototype { get; set; }
        [Name("get")]
        public object? Get(object target, string property, object receiver)
        {
            if (TargetType is not null && Prototype is not null)
            {
                var v1 = Prototype[property];
                var v2 = TargetType[property];
                if (Script.IsDefined(v1) && Script.IsDefined(v2) & v1 != v2)
                {
                    throw new AmbiguousMatchException($"Due to a limitation on the type system, Type \"{FullName}\"(being dependent on itself and implemented via a TypeProxy), cannot have a member whose name clashes with a System.Type member. Name \"{property}\" caused a clash.");
                }
                return v1 ?? v2;
            }
            //We will need these properties early before the proxy is bound to its type
            if (property.NativeEquals(Constants.PrototypeFullName))
                return FullName.As<object>();
            else if (property.NativeEquals("$type"))
                return this;
            else if (NetJs.Script.TypeOf(property).NativeEquals("string") && property.NativeStartsWith("$itype$")) //proxy inner types that are not yet defined
            {
                var name = property.NativeSubstring(7);
                var proxyHandler = new InnerTypeProxyHandler(this, name);
                object? proxy = null;
                NetJs.Script.Write("proxy = new Proxy({}, proxyHandler)");
                return proxy;
            }
            else if (property.NativeEquals("$isProxy"))
                return true.As<object>();
            else if (property.NativeEquals("IsGenericTypeDefinition"))
                return FullName.NativeEndsWith(">").As<object>();
            return null;
        }
        [Name("set")]
        public bool Set(object target, string property, object value)
        {
            //Update the target of the proxy
            if (property.NativeEquals(nameof(TargetType)))
            {
                TargetType = value.As<Type>();
                return true;
            }
            else if (property.NativeEquals(nameof(Prototype)))
            {
                Prototype = value.As<TypePrototype>();
                return true;
            }
            return false;
        }
    }

    class InnerTypeProxyHandler
    {
        TypeProxyHandler _parentProxy;
        string _innerTypeName;
        public InnerTypeProxyHandler(TypeProxyHandler parentProxy, string innerTypeName) 
        {
            _parentProxy = parentProxy;
            _innerTypeName = innerTypeName;
        }
        /// <summary> 
        /// The finally created type we will proxy to
        /// </summary>
        internal Type? TargetType { get; set; }
        internal TypePrototype? Prototype { get; set; }
        [Name("get")]
        public object? Get(object target, string property, object receiver)
        {
            if (TargetType is not null && Prototype is not null)
            {
                var v1 = Prototype[property];
                var v2 = TargetType[property];
                if (Script.IsDefined(v1) && Script.IsDefined(v2) & v1 != v2)
                {
                    throw new AmbiguousMatchException($"Due to a limitation on the type system, Type \"{_parentProxy.FullName}.{_innerTypeName}\"(being dependent on itself and implemented via a TypeProxy), cannot have a member whose name clashes with a System.Type member. Name \"{property}\" caused a clash.");
                }
                return v1 ?? v2;
            }
            if (property.NativeEquals("IsGenericTypeDefinition"))
                return _innerTypeName.NativeEndsWith(">").As<object>();
            else if (property.NativeEquals("$type"))
                return this;
            else if (property.NativeEquals("_prototype"))
                return this;
            return null;
        }
    }
}
