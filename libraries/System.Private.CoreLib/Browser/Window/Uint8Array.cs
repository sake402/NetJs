using System;

namespace Window
{
    /// <summary>
    /// Uint8Array typed array.
    /// </summary>
    [NetJs.External]
    [NetJs.Name("Uint8Array")]
    public class Uint8Array
    {
        public extern Uint8Array(int length);
        public extern Uint8Array(Array buffer, int byteOffset = 0, int? length = null);
        public extern Uint8Array(ArrayBuffer buffer, int byteOffset = 0, int? length = null);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern void set(Uint8Array source);
        public extern byte this[int index] { get; set; }
        public extern Uint8Array slice(int start = 0, int? end = null);
        public static extern bool isView(object arg);
        public static extern Uint8Array fromBase64(string base64Str);
    }
}