using System;

namespace Window
{
    /// <summary>
    /// EventSource (Server-Sent Events) wrapper.
    /// </summary>
    [NetJs.External]
    public class EventSource : EventTarget
    {
        public extern EventSource(string url, object? options = null);
        public extern string url { get; }
        public extern int readyState { get; }
        public extern void close();
    }
}