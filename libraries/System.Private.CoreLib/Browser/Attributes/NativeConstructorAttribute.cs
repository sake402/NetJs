using System;

namespace NetJs
{
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class NativeConstructorAttribute : Attribute
    {
    }
}