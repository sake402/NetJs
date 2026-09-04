using System;

namespace NetJs
{
    [External]
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class AssemblyHandleAttribute : Attribute
    {
        //Handle 1 is reserved for runtime
        public const int RuntimeHandle = 1;
        public const int SystemPrivateCoreLib = 1;
        public AssemblyHandleAttribute(uint handle) { }
    }
}