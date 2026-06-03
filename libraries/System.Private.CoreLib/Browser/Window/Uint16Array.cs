using System;

namespace Window
{
    [NetJs.External]
    public class Uint16Array
    {
        public extern Uint16Array(int length);
        public extern Uint16Array(Array buffer, int byteOffset = 0, int? length = null);
        public extern Uint16Array(ArrayBuffer buffer, int byteOffset = 0, int? length = null);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern ushort this[int index] { get; set; }
        public extern Uint16Array slice(int start = 0, int? end = null);
    }
}