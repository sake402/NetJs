using System;

namespace Window
{
    [NetJs.External]
    public class Int32Array
    {
        public extern Int32Array(int length);
        public extern Int32Array(Union<Array, ArrayBuffer> buffer);
        public extern Int32Array(Union<Array, ArrayBuffer> buffer, int byteOffset);
        public extern Int32Array(Union<Array, ArrayBuffer> buffer, int byteOffset, int length);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern int this[int index] { get; set; }
        public extern Int32Array slice(int start = 0, int? end = null);
    }
}