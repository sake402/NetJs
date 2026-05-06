using System;

namespace Window
{
    /// <summary>
    /// Represents an open IndexedDB database.
    /// </summary>
    [NetJs.External]
    public class IDBDatabase : EventTarget
    {
        public extern string name { get; }
        public extern long version { get; }
        public extern DOMStringList objectStoreNames { get; }

        public extern IDBObjectStore createObjectStore(string name, object? options = null);
        public extern void deleteObjectStore(string name);
        public extern IDBTransaction transaction(string[] storeNames, string mode = "readonly");
        public extern void close();
    }
}