namespace System
{
    [NetJs.Reflectable(false)]
    public class ArrayMutationProxyHandler : IJsProxyHandler
    {
        public ArrayMutationProxyHandler(Array array)
        {
            _array = array;
        }
        Array _array;
        public event EventHandler? OnMutated;
        public object? Get(object target, string property, object receiver)
        {
            if (property.NativeEquals("$isProxy"))
            {
                return true.As<object>();
            }
            unchecked
            {
                return _array[NetJs.Script.ParseInt(property)];
            }
        }
        public bool Set(object target, string property, object value)
        {
            unchecked
            {
                _array[NetJs.Script.ParseInt(property)] = value;
            }
            OnMutated?.Invoke(_array, EventArgs.Empty);
            return true;
        }
    }
}
