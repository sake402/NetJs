using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// NodeList wrapper.
    /// </summary>
    [NetJs.External]
    public class NodeList
    {
        public extern int length { get; }
        public extern Node? item(int index);
        public extern Node this[int i] { get; }
        public extern void forEach(NativeAction<Node> action);
    }

    /// <summary>
    /// HTMLCollection wrapper.
    /// </summary>
    [NetJs.External]
    public class HTMLCollection
    {
        public extern int length { get; }
        public extern Element? item(int index);
        public extern Element? namedItem(string name);
    }

    /// <summary>
    /// DOMTokenList wrapper (classList).
    /// </summary>
    [NetJs.External]
    public class DOMTokenList
    {
        public extern int length { get; }
        public extern bool contains(string token);
        public extern void add(params string[] tokens);
        public extern void remove(params string[] tokens);
        public extern void toggle(string token, bool? force = null);
        public extern string? item(int index);
    }

    /// <summary>
    /// CSSStyleDeclaration wrapper (element.style)
    /// </summary>
    [NetJs.External]
    public class CSSStyleDeclaration
    {
        public extern string? cssText { get; set; }
        public extern string? getPropertyValue(string property);
        public extern void setProperty(string property, string value, string? priority = null);
        public extern void removeProperty(string property);
        public extern string? this[string property] { get; set; }
    }
}