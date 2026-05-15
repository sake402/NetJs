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

    /// <summary>
    /// Make a params array parameter be spread into individual arguments when calling a native function
    /// </summary>
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class SpreadAttribute : Attribute
    {

    }
}