using System;

namespace NetJs
{
    /// <summary>
    /// Mark a delegate as being a native javascript function, not wrapped into generated delegate class
    /// </summary>
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Delegate)]
    public class NativeDelegateAttribute : Attribute
    {

    }
}