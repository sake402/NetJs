using System;
using System.Reflection;

namespace NetJs
{
    public class ArrayMutationEventArgs : EventArgs
    {
        public string Property { get; set; } = default!;
        public object? OldValue { get; set; }
        public object? Value { get; set; }
    }

    [NetJs.Reflectable(false)]
    public class ArrayMutationProxyHandler : IArrayProxyHandler
    {
        public ArrayMutationProxyHandler(Array array)
        {
            _array = array;
        }
        Array _array;
        public event EventHandler<ArrayMutationEventArgs>? OnMutated;
        public Array Array => _array;
        public Type? ElementType { get; private set; }

        public object? Get(object target, string propertyName, object receiver)
        {
            if (propertyName.NativeEquals(Constants.IsProxy))
            {
                return true.As<object>();
            }
            if (propertyName.NativeEquals(Constants.ProxyHandler))
            {
                return this;
            }
            if (propertyName.NativeEquals(Constants.ProxyType))
            {
                return _array.GetType();
            }
            if (propertyName.NativeEquals(Array.ElementTypeName))
            {
                return this[Array.ElementTypeName].As<object>();
            }
            if (propertyName.NativeEquals(Array.SizesName))
            {
                return this[Array.SizesName].As<object>();
            }
            if (propertyName.NativeEquals(Array.LowerBoundsName))
            {
                return this[Array.LowerBoundsName].As<object>();
            }
            unchecked
            {
                return Window.Reflect.get(_array, propertyName, _array);
                //return NetJs.Script.Write<object>("Reflect.get(this._array, propertyName, this._array)");
                //return _array[NetJs.Script.ParseInt(property)];
            }
        }
        public bool Set(object target, string propertyName, object value, object receiver)
        {
            var propertyType = NetJs.Script.TypeOf(propertyName);
            if (propertyType.NativeEquals("string"))
            {
                if (propertyName.NativeEquals(Array.ElementTypeName))
                {
                    this[Array.ElementTypeName] = value;
                    ElementType = value.As<Type>();
                    return true;
                }
                else if (propertyName.NativeEquals(Array.SizesName))
                {
                    this[Array.SizesName] = value;
                    return true;
                }
                else if (propertyName.NativeEquals(Array.LowerBoundsName))
                {
                    this[Array.LowerBoundsName] = value;
                    return true;
                }
            }
            var oldValue = _array[propertyName];
            unchecked
            {
                Window.Reflect.set(_array, propertyName, value, _array);
                //NetJs.Script.Write<object>("Reflect.set(this._array, propertyName, value, this._array)");
                //_array[NetJs.Script.ParseInt(property)] = value;
            }
            OnMutated?.Invoke(_array, new ArrayMutationEventArgs()
            {
                Property = propertyName,
                OldValue = oldValue,
                Value = value
            });
            return true;
        }
    }
}
