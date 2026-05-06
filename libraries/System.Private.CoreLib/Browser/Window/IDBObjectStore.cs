using System;

namespace Window
{
    /// <summary>
    /// ObjectStore operations.
    /// </summary>
    [NetJs.External]
    public class IDBObjectStore
    {
        public extern string name { get; }
        public extern IDBRequest add(object value, object? key = null);
        public extern IDBRequest put(object value, object? key = null);
        public extern IDBRequest get(object key);
        public extern IDBRequest delete(object key);
        public extern IDBRequest clear();
        public extern IDBIndex index(string name);
        public extern IDBRequest openCursor(object? range = null, string direction = "next");
    }
}