using System;

namespace Window
{
    [NetJs.External]
    public class Float32Array
    {
        public extern Float32Array(int length);
        public extern Float32Array(Array buffer, int byteOffset = 0, int? length = null);
        public extern Float32Array(ArrayBuffer buffer, int byteOffset = 0, int? length = null);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern float this[int index] { get; set; }
        public extern Float32Array slice(int start = 0, int? end = null);
    }
}