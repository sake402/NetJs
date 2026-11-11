using System;

namespace NetJs
{
    //[NonScriptable]
    //[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    //public class BaseAttribute : Attribute
    //{
    //    public BaseAttribute(Type type) { }
    //}

    /// <summary>
    /// Mark a class as base class of  another class
    /// </summary>
    [NonScriptable]
    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct )]
    public sealed class BaseAttribute : Attribute
    {
        public BaseAttribute(Type type)
        {
        }
    }

}