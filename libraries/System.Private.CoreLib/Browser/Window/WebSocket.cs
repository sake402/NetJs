using System;

namespace Window
{
    /// <summary>
    /// WebSocket wrapper.
    /// </summary>
    [NetJs.External]
    public class WebSocket : EventTarget
    {
        public extern WebSocket(string url, object? protocols = null);
        public extern string url { get; }
        public extern string? protocol { get; }
        public extern int readyState { get; }
        public extern long bufferedAmount { get; }
        public extern void send(object data);
        public extern void close(int code = 1000, string? reason = null);
        public extern void addEventListener(string type, object listener, object? options = null);
    }
}