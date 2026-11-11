using System;

namespace Window
{
    /// <summary>
    /// WebSocket wrapper.
    /// </summary>
    [NetJs.External]
    public class WebSocket : EventTarget
    {
        public const int CONNECTING = 0;
        public const int OPEN = 1;
        public const int CLOSING = 2;
        public const int CLOSED = 3;
        public extern WebSocket(string url, object? protocols = null);
        public extern string url { get; }
        public extern string? protocol { get; }
        public extern int readyState { get; }
        public extern long bufferedAmount { get; }
        public extern void send([NetJs.Box(false)] object data);
        public extern void close(int code = 1000, string? reason = null);
        //public extern void addEventListener(string type, object listener, object? options = null);

        public class MessageEvent : Event
        {
            public extern Union<string, Blob, ArrayBuffer> data { get; }
        }

        public class CloseEvent : Event
        {
            public extern int code { get; }
            public extern string? reason { get; }
            public extern bool wasClean { get; }
        }

        public class ErrorEvent : Event
        {
            public extern object? error { get; }
        }

    }
}