using System;

namespace Window
{
    /// <summary>
    /// MessageChannel and MessagePort.
    /// </summary>
    [NetJs.External]
    public class MessageChannel
    {
        public extern MessageChannel();
        public extern MessagePort port1 { get; }
        public extern MessagePort port2 { get; }
    }

    [NetJs.External]
    public class MessagePort : EventTarget
    {
        public extern void postMessage(object? message, object? transfer = null);
        public extern void start();
        public extern void close();
    }
}