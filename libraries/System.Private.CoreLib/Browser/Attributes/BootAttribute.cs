using System;

namespace NetJs
{
    /// <summary>
    /// Mark a class as implementing a boot code
    /// </summary>
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    public sealed class BootAttribute : Attribute
    {
        public BootAttribute()
        {
        }
    }

}