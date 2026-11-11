using System.Reflection;

namespace NetJs
{
    [Reflectable(false)]
    [External]
    public interface IJsProxyHandler
    {
        //[Name("$isProxy")]
        //bool IsProxy => true;
        [Name("get")]
        [TrimmerKeep(KeepDependencyFlags.TargetConstructorCalled)]
        public object? Get(object target, string property, object receiver);
        [Name("set")]
        [TrimmerKeep(KeepDependencyFlags.TargetConstructorCalled)]
        public bool Set(object target, string property, object value, object receiver);
    }

    [Reflectable(false)]
    public static class JSProxy
    {
        [IgnoreGeneric]
        public static T Create<T>(IJsProxyHandler handler, object? target = null)
        {
            object? proxy = null;
            Script.Write("proxy = new Proxy(target??{}, handler)");
            return proxy.As<T>();
        }
    }
}
