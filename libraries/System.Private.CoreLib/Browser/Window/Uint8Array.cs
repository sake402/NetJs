using System;

namespace Window
{
    /// <summary>
    /// Uint8Array typed array.
    /// </summary>
    [NetJs.External]
    public class Uint8Array
    {
        public extern Uint8Array(int length);
        public extern Uint8Array(ArrayBuffer buffer, int byteOffset = 0, int? length = null);
        public extern int length { get; }
        public extern byte this[int index] { get; set; }
        public extern Uint8Array slice(int start = 0, int? end = null);
        public static extern bool isView(object arg);
    }
}