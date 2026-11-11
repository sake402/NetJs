using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// Represents an Element in the DOM.
    /// </summary>
    [NetJs.External]
    public class Element : Node
    {
        //public extern string tagName { get; }
        public extern string? id { get; set; }
        public extern string? className { get; set; }
        public extern DOMTokenList classList { get; }
        public extern CSSStyleDeclaration style { get; }

        public extern string? getAttribute(string name);
        public extern void setAttribute(string name, string value);
        public extern void removeAttribute(string name);
        public extern bool hasAttribute(string name);
        public extern Attr? getAttributeNode(string name);

        public extern HTMLCollection children { get; }
        public extern Element? closest(string selectors);
        public extern Element? querySelector(string selectors);
        public extern NodeList querySelectorAll(string selectors);

        public extern void focus();
        public extern void blur();

        //public extern void addEventListener(string type, object listener, object? options = null);
        //public extern void removeEventListener(string type, object listener, object? options = null);
    }
}