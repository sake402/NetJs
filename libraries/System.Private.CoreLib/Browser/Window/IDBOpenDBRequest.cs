using System;

namespace Window
{
    /// <summary>
    /// Open/delete DB request.
    /// </summary>
    [NetJs.External]
    public class IDBOpenDBRequest : IDBRequest
    {
        public extern IDBDatabase? result { get; }
        public extern EventHandler? onupgradeneeded { get; set; }
    }
}