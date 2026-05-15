
using NetJs;
using System.Reflection;

namespace System
{
    [NetJs.Reflectable(false)]
    [NetJs.External]
    public interface IJsProxyHandler
    {
        //[Name("$isProxy")]
        //bool IsProxy => true;
        [Name("get")]
        public object? Get(object target, string property, object receiver);
        [Name("set")]
        public bool Set(object target, string property, object value);
    }

    [NetJs.Reflectable(false)]
    public static class JSProxy
    {
        [NetJs.IgnoreGeneric]
        public static T Create<T>(IJsProxyHandler handler)
        {
            object? proxy = null;
            Script.Write("proxy = new Proxy({}, handler)");
            return proxy.As<T>();
        }
    }
}
