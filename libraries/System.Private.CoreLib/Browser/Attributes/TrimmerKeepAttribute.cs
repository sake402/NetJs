using System;
using System.Diagnostics.CodeAnalysis;

namespace NetJs
{
    /// <summary>
    /// Specify a symbol be not trimmed even if not referenced
    /// </summary>
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Delegate | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Constructor)]
    public sealed class TrimmerKeepAttribute : Attribute
    {
        public TrimmerKeepAttribute()
        {
        }
        public TrimmerKeepAttribute(KeepDependencyFlags reason)
        {
        }
        public TrimmerKeepAttribute(DynamicallyAccessedMemberTypes members, bool _)
        {
        }
    }
}