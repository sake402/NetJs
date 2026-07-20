using System;

namespace NetJs
{
    public interface IArrayProxyHandler : IJsProxyHandler
    {
        Array Array { get; }
        Type? ElementType { get; }
    }
}
