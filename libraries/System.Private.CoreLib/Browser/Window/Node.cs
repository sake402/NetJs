using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// Base DOM Node.
    /// </summary>
    [NetJs.External]
    public class Node: EventTarget
    {
        public extern string? nodeName { get; }
        public extern string? tagName { get; }
        public extern string? nodeValue { get; set; }
        public extern string? textContent { get; set; }
        public extern NodeType nodeType { get; }
        public extern Node? parentNode { get; }
        public extern NodeList childNodes { get; }
        public extern Node nextSibling { get; }
        public extern Node? previousSibling { get; }
        public extern Node? lastChild { get; }
        public extern Document? ownerDocument { get; }
        public extern string? namespaceURI { get; }
        public extern bool hasChildNodes();

        public extern Node appendChild(Node node);
        public extern Node insertBefore(Node newNode, Node? referenceNode);
        public extern Node removeChild(Node node);
        public extern Node replaceChild(Node newChild, Node oldChild);
        public extern Node cloneNode(bool deep = false);

        public extern bool contains(Node? other);
    }
}