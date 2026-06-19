using NetJs;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    [NetJs.Reflectable(false)]
    public class ArrayWindowProxyHandler : IJsProxyHandler
    {
        public ArrayWindowProxyHandler(Array array, int offset, int length)
        {
            _array = array;
            _offset = offset;
            _length = length;
        }
        Array _array;
        int _offset;
        int _length;
        public object? Get(object target, string property, object receiver)
        {
            var propertyType = NetJs.Script.TypeOf(property);
            if (propertyType.NativeEquals("string"))
            {
                if (property.NativeEquals(Constants.IsProxy))
                {
                    return true.As<object>();
                }
                if (property.NativeEquals(Constants.ProxyType))
                {
                    return _array.GetType();
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
        public bool Set(object target, string property, object value, object receiver)
        {
            var propertyType = NetJs.Script.TypeOf(property);
            if (propertyType.NativeEquals("string"))
            {
                unchecked
                {
                    var index = NetJs.Script.ParseInt(property);
                    if (!NetJs.Script.IsNaN(index))
                        _array[index + _offset] = value;
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
