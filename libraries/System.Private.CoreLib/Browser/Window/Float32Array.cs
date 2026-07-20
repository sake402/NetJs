using System;

namespace Window
{
    [NetJs.External]
    public class Float32Array
    {
        public extern Float32Array(int length);
        public extern Float32Array(Union<Array, ArrayBuffer> buffer);
        public extern Float32Array(Union<Array, ArrayBuffer> buffer, int byteOffset);
        public extern Float32Array(Union<Array, ArrayBuffer> buffer, int byteOffset, int length);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern float this[int index] { get; set; }
        public extern Float32Array slice(int start = 0, int? end = null);
    }
}