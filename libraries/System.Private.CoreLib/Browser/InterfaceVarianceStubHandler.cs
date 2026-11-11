using System;
using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;
using Window;

namespace NetJs
{
    public class InterfaceVarianceStubHandler : IJsProxyHandler
    {
        private readonly object _target;
        private readonly Map<string, string> _nameMap;

        public static object? Create(object? target, TypePrototype from, TypePrototype to)
        {
            if (target == null)
                return null;
            var fromMethods = (from.Metadata?.Methods ?? NetJs.Script.NewArray<MethodModel>());
            var toMethods = (to.Metadata?.Methods ?? NetJs.Script.NewArray<MethodModel>());

            Map<string, string>? _nameMap = null;
            for (int i = 0; i < fromMethods.Length && i < toMethods.Length; i++)
            {
                var fromName = fromMethods[i].GetOutputName();
                var toName = toMethods[i].GetOutputName();
                if (fromName.NativeEquals(toName))
                    continue;
                _nameMap ??= new();
                // Contravariance: Maps the general expected call (to) -> specific implemented action (from)
                // Covariance:     Maps the specific expected call (to) -> general implemented action (from)
                // Since both need to intercept what the caller asks for (to) and map it to reality (from), 
                // the mapping key is 'toName' when moving down the type hierarchy or across explicit boundaries!
                _nameMap.set(toName, fromName);
            }

            var fromProps = (from.Metadata?.Properties ?? NetJs.Script.NewArray<PropertyModel>());
            var toProps = (to.Metadata?.Properties ?? NetJs.Script.NewArray<PropertyModel>());

            for (int i = 0; i < fromProps.Length && i < toProps.Length; i++)
            {
                var fromName = fromProps[i].GetOutputName();
                var toName = toProps[i].GetOutputName();

                if (fromName.NativeEquals(toName))
                    continue;

                _nameMap ??= new();
                _nameMap.set(toName, fromName);
            }

            if (_nameMap == null)
                return target;
            var handler = new InterfaceVarianceStubHandler(target, from, to, _nameMap);
            return JSProxy.Create<InterfaceVarianceStubHandler>(handler, target);
        }

        private InterfaceVarianceStubHandler(object target, TypePrototype from, TypePrototype to, Map<string, string> nameMap)
        {
            this._target = target;
            _nameMap = nameMap;
        }

        public object? Get(object target, string property, object receiver)
        {
            if (_nameMap.has(property))
            {
                property = _nameMap.get(property);
            }
            return Reflect.get(target, property, receiver);
        }

        public bool Set(object target, string property, object value, object receiver)
        {
            if (_nameMap.has(property))
            {
                property = _nameMap.get(property);
            }
            return Reflect.set(target, property, value, receiver);
        }
    }
}