using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// Represents the HTML or XML document.
    /// </summary>
    [NetJs.External]
    public class Document : Node
    {
        public extern static Document Instance
        {
            [NetJs.Template("document")]
            get;
        }
        public extern string? title { get; set; }
        public extern Element? documentElement { get; }
        public extern Element? body { get; set; }
        public extern Element? head { get; }

        public extern Element? getElementById(string id);
        public extern NodeList getElementsByTagName(string name);
        public extern HTMLCollection getElementsByClassName(string name);
        public extern Element? querySelector(string selectors);
        public extern NodeList querySelectorAll(string selectors);

        public extern Element createElement(string tagName);
        public extern Text createTextNode(string data);
        public extern Comment createComment(string data);
        public extern Attr createAttribute(string name);

        public extern Node importNode(Node node, bool deep = false);
        public extern Event createEvent(string eventType);
    }
}