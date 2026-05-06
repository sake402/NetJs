using System;
using System.Threading.Tasks;

namespace Window
{
    /// <summary>
    /// Represents a blob (binary data).
    /// </summary>
    [NetJs.External]
    public class Blob
    {
        public extern Blob(object? parts = null, object? options = null);
        public extern long size { get; }
        public extern string type { get; }
        public extern Blob slice(long start = 0, long? end = null, string? contentType = null);
        public extern Promise<string> text();
        public extern Promise<ArrayBuffer> arrayBuffer();
    }
}