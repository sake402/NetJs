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
            if (property.NativeEquals("$isProxy"))
            {
                return true.As<object>();
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
                return _array[NetJs.Script.ParseInt(property) + _offset];
            }
        }
        public bool Set(object target, string property, object value)
        {
            unchecked
            {
                _array[NetJs.Script.ParseInt(property) + _offset] = value;
            }
            return true;
        }
    }
}
