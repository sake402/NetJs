using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// UI event (keyboard, mouse wrappers).
    /// </summary>
    [NetJs.External]
    public class UIEvent : Event
    {
        //public extern Window? view { get; }
        public extern int detail { get; }
        public extern UIEvent(string type, object? eventInitDict = null);
    }
}