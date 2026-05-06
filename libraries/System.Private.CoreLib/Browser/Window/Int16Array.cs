using System;

namespace Window
{
    [NetJs.External]
    public class Int16Array
    {
        public extern Int16Array(int length);
        public extern Int16Array(ArrayBuffer buffer, int byteOffset = 0, int? length = null);
        public extern int length { get; }
        public extern short this[int index] { get; set; }
        public extern Int16Array slice(int start = 0, int? end = null);
    }
}