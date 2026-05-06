using System;

namespace Window
{
    /// <summary>
    /// Represents an IndexedDB transaction.
    /// </summary>
    [NetJs.External]
    public class IDBTransaction : EventTarget
    {
        public extern IDBObjectStore objectStore(string name);
        public extern string mode { get; }
        public extern void abort();
        public extern IDBDatabase? db { get; }
    }
}