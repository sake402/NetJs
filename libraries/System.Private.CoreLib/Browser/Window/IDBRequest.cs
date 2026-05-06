using System;

namespace Window
{
    /// <summary>
    /// Represents async IDB request.
    /// </summary>
    [NetJs.External]
    public class IDBRequest : EventTarget
    {
        public extern object? result { get; }
        public extern object? error { get; }
        public extern short readyState { get; }
        public extern IDBTransaction? transaction { get; }
    }
}