using System;
using Window;

namespace NetJs
{
    public class SuperClassProxyHandler : IJsProxyHandler
    {
        object _instance;
        TypePrototype _basePrototype;
        public SuperClassProxyHandler(object instance)
        {
            this._instance = instance;
            _basePrototype = Object.GetPrototypeOf(Object.GetPrototypeOf(instance));
        }

        public object? Get(object target, string property, object receiver)
        {
            var value = Reflect.get(_basePrototype, property, _instance);

            // If it's a method, bind it to 'this' so it runs in the current instance context
            if (Script.TypeOf(value).NativeEquals("function"))
            {
                return Script.Write<object>("value.bind(this._instance)");
            }
            return value;
        }

        public bool Set(object target, string property, object value, object receiver)
        {
            return Reflect.set(_basePrototype, property, value, _instance);
        }
    }
}
