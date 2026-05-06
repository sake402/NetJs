using System;

namespace Window
{
    [NetJs.External]
    public class Float64Array
    {
        public extern Float64Array(int length);
        public extern Float64Array(ArrayBuffer buffer, int byteOffset = 0, int? length = null);
        public extern int length { get; }
        public extern double this[int index] { get; set; }
        public extern Float64Array slice(int start = 0, int? end = null);
    }
}