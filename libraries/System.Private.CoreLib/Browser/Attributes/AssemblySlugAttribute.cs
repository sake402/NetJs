using System;

namespace NetJs
{
    [AttributeUsage(AttributeTargets.Assembly)]
    [NonScriptable]
    public class AssemblySlugAttribute : Attribute
    {
        public AssemblySlugAttribute(string value) { }
    }
}