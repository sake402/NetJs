using System;

namespace NetJs
{
    /// <summary>
    /// Specifies that the name of the entity should be kept as-is, when emitting JavaScript-equivalent, even when performing minification
    /// </summary>
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Delegate | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Constructor)]
    public sealed class KeepMemberNamesAttribute : Attribute
    {
        public KeepMemberNamesAttribute(string value)
        {
        }
    }
}