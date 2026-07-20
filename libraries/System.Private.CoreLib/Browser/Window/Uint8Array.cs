using System;

namespace Window
{
    /// <summary>
    /// Uint8Array typed array.
    /// </summary>
    [NetJs.External]
    [NetJs.Name("Uint8Array")]
    public class Uint8Array : TypedArray
    {
        public extern Uint8Array(int length);
        public extern Uint8Array(Union<Array, ArrayBuffer> buffer);
        public extern Uint8Array(Union<Array, ArrayBuffer> buffer, int byteOffset);
        public extern Uint8Array(Union<Array, ArrayBuffer> buffer, int byteOffset, int length);
        public extern int length { get; }
        public extern int byteLength { get; }
        public extern ArrayBuffer buffer { get; }
        public extern void set(Uint8Array source);
        public extern byte this[int index] { get; set; }
        public extern Uint8Array slice(int start = 0, int? end = null);
        public extern Uint8Array subarray(int start = 0, int? end = null);
        public static extern bool isView(object arg);
        public static extern Uint8Array fromBase64(string base64Str);
    }
}