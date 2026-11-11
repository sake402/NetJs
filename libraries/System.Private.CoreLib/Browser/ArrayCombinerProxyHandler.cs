using System;
using System.Diagnostics.CodeAnalysis;

namespace NetJs
{
    [NetJs.Reflectable(false)]
    public class ArrayCombinerProxyHandler : IArrayProxyHandler
    {
        [DynamicDependency(nameof(Get))]
        [DynamicDependency(nameof(Set))]
        public ArrayCombinerProxyHandler(Array[] arrays, int offset = 0, int length = -1)
        {
            _arrays = arrays;
            _offset = offset;
            if (length < 0)
            {
                length = 0;
                arrays.forEach(a => length += a.Length);
            }
            _length = length;
        }
        internal Array[] _arrays;
        int _offset;
        int _length;

        public Array Array => _arrays[0];
        Type? _elementType;
        public Type? ElementType => _elementType ?? Array.GetArrayElementType(_arrays[0]);

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
                    return _arrays[0].GetType();
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
                    return _arrays[0][property];
                }
                unchecked
                {
                    var index = NetJs.Script.ParseInt(property);
                    if (!NetJs.Script.IsNaN(index))
                    {
                        index += _offset;
                        int array_i = 0;
                        while (array_i < _arrays.Length)
                        {
                            if (index < _arrays[array_i].Length)
                            {
                                return _arrays[array_i][index];
                            }
                            index -= _arrays[array_i].Length;
                            array_i++;
                        }
                        throw null!;
                    }
                }
            }
            return Window.Reflect.get(_arrays[0], property, _arrays[0]);
            //return NetJs.Script.Write<object>("Reflect.get(this.{nameof(ArrayWindowProxyHandler._array)}, property, this.{nameof(ArrayWindowProxyHandler._array)})");
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
                    {
                        index += _offset;
                        int array_i = 0;
                        while (array_i < _arrays.Length)
                        {
                            if (index < _arrays[array_i].Length)
                            {
                                _arrays[array_i][index] = value;
                                return true;
                            }
                            index -= _arrays[array_i].Length;
                            array_i++;
                        }
                        throw null!;
                    }
                }
                if (property.NativeEquals(Array.ElementTypeName))
                {
                    this[Array.ElementTypeName] = value;
                    _elementType = value.As<Type>();
                }
                else if (property.NativeEquals(Array.SizesName))
                {
                    this[Array.SizesName] = value;
                }
                else if (property.NativeEquals(Array.LowerBoundsName))
                {
                    this[Array.LowerBoundsName] = value;
                }
            }
            else
            {
                Window.Reflect.set(_arrays[0], property, value, _arrays[0]);
                //NetJs.Script.Write<object>("Reflect.set(this.{nameof(ArrayWindowProxyHandler._array)}, property, value, this.{nameof(ArrayWindowProxyHandler._array)})");
            }
            return true;
        }
    }
}
