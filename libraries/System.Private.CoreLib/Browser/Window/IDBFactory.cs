using System;
using System.Threading.Tasks;

namespace Window
{
    /// <summary>
    /// Entry point for IndexedDB operations.
    /// </summary>
    [NetJs.External]
    public class IDBFactory
    {
        public extern IDBOpenDBRequest open(string name, long? version = null);
        public extern IDBOpenDBRequest deleteDatabase(string name);
        public extern bool cmp(object? first, object? second);
    }
}