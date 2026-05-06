using System;

namespace Window
{
    /// <summary>
    /// Simple DOM string list.
    /// </summary>
    [NetJs.External]
    public class DOMStringList
    {
        public extern int length { get; }
        public extern string? item(int index);
        public extern bool contains(string str);
    }
}