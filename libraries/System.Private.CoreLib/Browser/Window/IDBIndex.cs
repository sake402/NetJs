using System;

namespace Window
{
    /// <summary>
    /// Index on an object store.
    /// </summary>
    [NetJs.External]
    public class IDBIndex
    {
        public extern string name { get; }
        public extern IDBRequest get(object key);
        public extern IDBRequest getKey(object key);
        public extern IDBRequest openCursor(object? range = null, string direction = "next");
        public extern IDBRequest count(object? key = null);
    }
}