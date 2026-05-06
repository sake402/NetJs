using System;

namespace Window
{
    [NetJs.External]
    public class BroadcastChannel : EventTarget
    {
        public extern BroadcastChannel(string name);
        public extern string name { get; }
        public extern void postMessage(object? message);
        public extern void close();
    }
}