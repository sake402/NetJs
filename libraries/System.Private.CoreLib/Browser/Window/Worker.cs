using System;

namespace Window
{
    /// <summary>
    /// Dedicated Worker wrapper.
    /// </summary>
    [NetJs.External]
    public class Worker : EventTarget
    {
        public extern Worker(string scriptURL, object? options = null);
        public extern void postMessage(object? message, object? transfer = null);
        public extern void terminate();
        public extern string? scriptURL { get; }
    }
}