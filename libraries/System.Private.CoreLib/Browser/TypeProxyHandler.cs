using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Window;

namespace NetJs
{
    /// <summary>
    /// For types that needs to reference itself(defined via typeproxy)
    /// This is simply a JS proxy handler that forwards request to the proxy to the System.Type/Prototype itself
    /// </summary>
    [NetJs.Boot]
    [NetJs.Reflectable(false)]
    class TypeProxyHandler : IJsProxyHandler
    {
        [DynamicDependency(nameof(Get))]
        [DynamicDependency(nameof(Set))]
        public TypeProxyHandler(string referenceFullName, string prototypeFullName)
        {
            if (!NetJs.Script.IsDefined(prototypeFullName))
                prototypeFullName = referenceFullName;
            ReferenceFullName = referenceFullName;
            PrototypeFullName = prototypeFullName;
        }
        [Name("$parent")]
        TypeProxyHandler? Parent { get; set; }
        [Name(Constants.PrototypeReferenceFullName)]
        internal string ReferenceFullName { get; }
        [Name(Constants.PrototypeFullName)]
        internal string PrototypeFullName { get; set; }
        /// <summary>
        /// The finally created type we will proxy to
        /// </summary>
        [NetJs.Name("TargetType")] //prevent from name shrinking as we use nameof() on it. We may add this feature to the transpiler in the future to rewrite the nameof parameter accordingly too
        internal Type? TargetType { get; set; }
        [NetJs.Name("Prototype")]
        internal new TypePrototype? Prototype { get; set; }

        public object? Get(object target, string property, object receiver)
        {
            if (property.NativeEquals(Constants.IsProxy))
                return true.As<object>();
            if (property.NativeEquals(Constants.ProxyHandler))
                return this;
            if (property.NativeEquals(Constants.TypeProxyHandlerPrototype) && Prototype != null)
                return Prototype;
            if (property.NativeEquals(Constants.TypeProxyHandlerTargetType) && TargetType != null)
                return TargetType;
            if (TargetType is not null && Prototype is not null)
            {
                var v1 = Prototype[property];
                var v2 = TargetType[property];
                if (Script.IsDefined(v1) && Script.IsDefined(v2) && v1 != v2)
                {
                    throw new AmbiguousMatchException($"Due to a limitation on the type system, Type \"{PrototypeFullName}\"(being dependent on itself and implemented via a TypeProxy), cannot have a member whose name clashes with a System.Type member. Name \"{property}\" caused a clash.");
                }
                return v1 ?? v2;
            }
            //We will need these properties early before the proxy is bound to its type
            if (property.NativeEquals(Constants.PrototypeFullName))
            {
                if (PrototypeFullName.NativeStartsWith("$"))
                {
                    var dot = PrototypeFullName.NativeIndexOf(".");
                    if (dot >= 0)
                        return PrototypeFullName.NativeSubstring(dot + 1).As<object>();
                }
                return PrototypeFullName.As<object>();
            }
            else if (property.NativeEquals(Constants.PrototypeFullNameWithSpecialSeperator))
            {
                string getFullName(string separator)
                {
                    if (PrototypeFullName.NativeStartsWith("$"))
                    {
                        var dot = PrototypeFullName.NativeIndexOf(".");
                        if (dot >= 0)
                            return PrototypeFullName.NativeSubstring(dot + 1).NativeReplaceAll(".", separator).NativeReplaceAll("+", separator);
                    }
                    return PrototypeFullName.NativeReplaceAll(".", separator).NativeReplaceAll("+", separator);
                }
                NativeFunction<string, string> ret = getFullName;
                return ret;
            }
            else if (property.NativeEquals(Constants.PrototypeReferenceFullName))
                return ReferenceFullName.As<object>();
            else if (property.NativeEquals(Constants.TypeProxyHandlerTargetType))
                return receiver;
            else if (property.NativeEquals(nameof(Type.UnderlyingSystemType)))
                return receiver;
            else if (NetJs.Script.TypeOf(property).NativeEquals("string") && property.NativeStartsWith("$itype$")) //proxy inner types that are not yet defined
            {
                var prototypeFullTypeName = PrototypeFullName + "." + property.NativeSubstring(7);
                var referenceFullTypeName = ReferenceFullName + "." + property.NativeSubstring(7);
                var proxyHandler = new TypeProxyHandler(referenceFullTypeName, prototypeFullTypeName) { Parent = this };
                object? proxy = null;
                NetJs.Script.Write("proxy = new Proxy({}, proxyHandler)");
                //AppDomain.GlobalPrototypeRegistry.SetNested(metadataFullTypeName.NativeReplaceAll("<", "$").NativeReplaceAll(",", "$").NativeReplaceAll(">", "$"), proxy.As<TypePrototype>());
                return proxy;
            }
            else if (property.NativeEquals(Constants.IsProxy))
                return true.As<object>();
            else if (property.NativeEquals("IsGenericTypeDefinition"))
                return PrototypeFullName.NativeEndsWith(">").As<object>();
            else if (property.NativeEquals("_prototype"))
                return this;
            return null;
        }

        public bool Set(object target, string property, object value, object receiver)
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
            if (TargetType != null)
            {
                if (Reflect.set(TargetType, property, value, TargetType))
                    return true;
            }
            return false;
        }
    }

    //class InnerTypeProxyHandler
    //{
    //    TypeProxyHandler _parentProxy;
    //    string _innerTypeName;
    //    public InnerTypeProxyHandler(TypeProxyHandler parentProxy, string innerTypeName)
    //    {
    //        _parentProxy = parentProxy;
    //        _innerTypeName = innerTypeName;
    //    }
    //    /// <summary> 
    //    /// The finally created type we will proxy to
    //    /// </summary>
    //    internal Type? TargetType { get; set; }
    //    internal TypePrototype? Prototype { get; set; }
    //    [Name("get")]
    //    public object? Get(object target, string property, object receiver)
    //    {
    //        if (TargetType is not null && Prototype is not null)
    //        {
    //            var v1 = Prototype[property];
    //            var v2 = TargetType[property];
    //            if (Script.IsDefined(v1) && Script.IsDefined(v2) & v1 != v2)
    //            {
    //                throw new AmbiguousMatchException($"Due to a limitation on the type system, Type \"{_parentProxy.MetadataFullName}.{_innerTypeName}\"(being dependent on itself and implemented via a TypeProxy), cannot have a member whose name clashes with a System.Type member. Name \"{property}\" caused a clash.");
    //            }
    //            return v1 ?? v2;
    //        }
    //        if (property.NativeEquals("IsGenericTypeDefinition"))
    //            return _innerTypeName.NativeEndsWith(">").As<object>();
    //        else if (property.NativeEquals("$type"))
    //            return this;
    //        else if (property.NativeEquals("_prototype"))
    //            return this;
    //        return null;
    //    }
    //}
}
