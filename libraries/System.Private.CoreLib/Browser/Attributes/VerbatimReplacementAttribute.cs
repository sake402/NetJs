using System;

namespace NetJs
{
    /// <summary>
    /// Replace a string pattern in the annotated class with a replacement
    /// </summary>
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true)]
    public class VerbatimReplacementAttribute : Attribute
    {
        public VerbatimReplacementAttribute(string pattern, string replace) { }
    }
}