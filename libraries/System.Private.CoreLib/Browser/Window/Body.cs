using System;
using System.Threading.Tasks;

namespace Window
{
    /// <summary>
    /// Common base for Request/Response bodies.
    /// </summary>
    [NetJs.External]
    public class Body
    {
        public extern bool bodyUsed { get; }
        public extern Promise<ArrayBuffer> arrayBuffer();
        public extern Promise<Blob> blob();
        public extern Promise<string> text();
        public extern Promise<object?> json();
        public extern Promise<object> formData();
    }
}