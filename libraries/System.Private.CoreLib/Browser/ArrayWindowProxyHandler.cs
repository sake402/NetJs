using System;
using System.Diagnostics.CodeAnalysis;

namespace NetJs
{
    [NetJs.Reflectable(false)]
    public class ArrayWindowProxyHandler : IArrayProxyHandler
    {
        public ArrayWindowProxyHandler(Array array, int offset, int length)
        {
            _array = array;
            _offset = offset;
            _length = length;
        }
        internal Array _array;
        int _offset;
        int _length;

        public Array Array => _array;
        public Type? ElementType { get; private set; }

        public object? Get(object target, string property, object receiver)
        {
            var propertyName = NetJs.Script.TypeOf(property);
            if (propertyName.NativeEquals("string"))
            {
                if (property.NativeEquals(Constants.IsProxy))
                {
                    return true.As<object>();
                }
                if (property.NativeEquals(Constants.ProxyHandler))
                {
                    return this;
                }
                if (property.NativeEquals(Constants.ProxyType))
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
                if (property.NativeEquals("length"))
                {
                    return _length.As<object>();
                }
                if (property.NativeStartsWith("$"))
                {
                    return _array[property];
                }
                unchecked
                {
                    var index = NetJs.Script.ParseInt(property);
                    if (!NetJs.Script.IsNaN(index))
                        return _array[index + _offset];
                }
            }
            return NetJs.Script.Write<object>("Reflect.get(this._array, property, this._array)");
        }
        public bool Set(object target, string propertyName, object value, object receiver)
        {
            var propertyType = NetJs.Script.TypeOf(propertyName);
            if (propertyType.NativeEquals("string"))
            {
                unchecked
                {
                    var index = NetJs.Script.ParseInt(propertyName);
                    if (!NetJs.Script.IsNaN(index))
                        _array[index + _offset] = value;
                }
                if (propertyName.NativeEquals(Array.ElementTypeName))
                {
                    this[Array.ElementTypeName] = value;
                    ElementType = value.As<Type>();
                }
                else if (propertyName.NativeEquals(Array.SizesName))
                {
                    this[Array.SizesName] = value;
                }
                else if (propertyName.NativeEquals(Array.LowerBoundsName))
                {
                    this[Array.LowerBoundsName] = value;
                }
            }
            else
            {
                NetJs.Script.Write<object>("Reflect.set(this._array, property, value, this._array)");
            }
            return true;
        }
    }
}
