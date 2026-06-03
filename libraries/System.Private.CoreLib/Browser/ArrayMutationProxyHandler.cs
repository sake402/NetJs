using System.Reflection;

namespace System
{
    public class ArrayMutationEventArgs : EventArgs
    {
        public string Property { get; set; } = default!;
        public object? OldValue { get; set; }
        public object? Value { get; set; }
    }
    [NetJs.Reflectable(false)]
    public class ArrayMutationProxyHandler : IJsProxyHandler
    {
        public ArrayMutationProxyHandler(Array array)
        {
            _array = array;
        }
        Array _array;
        public event EventHandler<ArrayMutationEventArgs>? OnMutated;
        public object? Get(object target, string property, object receiver)
        {
            if (property.NativeEquals("$isProxy"))
            {
                return true.As<object>();
            }
            unchecked
            {
                return NetJs.Script.Write<object>("Reflect.get(this._array, property, this._array)");
                //return _array[NetJs.Script.ParseInt(property)];
            }
        }
        public bool Set(object target, string property, object value, object receiver)
        {
            var oldValue = _array[property];
            unchecked
            {
                NetJs.Script.Write<object>("Reflect.set(this._array, property, value, this._array)");
                //_array[NetJs.Script.ParseInt(property)] = value;
            }
            OnMutated?.Invoke(_array, new ArrayMutationEventArgs()
            {
                Property = property,
                OldValue = oldValue,
                Value = value
            });
            return true;
        }
    }
}
