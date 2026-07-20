using System;

namespace Window
{
    [NetJs.External]
    public class Int16Array
    {
        public extern Int16Array(int length);
        public extern Int16Array(Union<Array, ArrayBuffer> buffer);
        public extern Int16Array(Union<Array, ArrayBuffer> buffer, int byteOffset);
        public extern Int16Array(Union<Array, ArrayBuffer> buffer, int byteOffset, int length);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern short this[int index] { get; set; }
        public extern Int16Array slice(int start = 0, int? end = null);
    }
}