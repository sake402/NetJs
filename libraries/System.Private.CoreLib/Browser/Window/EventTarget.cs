using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// Base class for all event targets (elements, window, document).
    /// </summary>
    [NetJs.External]
    public class EventTarget
    {
        public extern void addEventListener(string type, object listener, object? options = null);
        public extern void removeEventListener(string type, object listener, object? options = null);
        public extern void addEventListener(string type, NativeAction<object> listener, object? options = null);
        public extern void removeEventListener(string type, NativeAction<object> listener, object? options = null);
        public extern bool dispatchEvent(Event evt);
    }
}