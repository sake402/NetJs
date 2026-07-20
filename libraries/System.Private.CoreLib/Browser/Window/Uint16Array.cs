using System;

namespace Window
{
    [NetJs.External]
    public class Uint16Array
    {
        public extern Uint16Array(int length);
        public extern Uint16Array(Union<Array, ArrayBuffer> buffer);
        public extern Uint16Array(Union<Array, ArrayBuffer> buffer, int byteOffset);
        public extern Uint16Array(Union<Array, ArrayBuffer> buffer, int byteOffset, int length);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern ushort this[int index] { get; set; }
        public extern Uint16Array slice(int start = 0, int? end = null);
    }
}