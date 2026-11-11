using System;

namespace NetJs
{
    /// <summary>
    /// Rewrites a generic argument using the provided value
    /// </summary>
    [AttributeUsage(AttributeTargets.GenericParameter)]
    [NonScriptable]
    public class CoarseGenericArgument : Attribute
    {
        public CoarseGenericArgument(string name) { }
    }
}