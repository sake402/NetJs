using System;

namespace Window
{
    /// <summary>
    /// ServiceWorker instance.
    /// </summary>
    [NetJs.External]
    public class ServiceWorker : EventTarget
    {
        public extern string? scriptURL { get; }
        public extern string? state { get; }
        public extern void postMessage(object? message, object? transfer = null);
    }
}