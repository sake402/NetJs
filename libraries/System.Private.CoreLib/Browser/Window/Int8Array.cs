using System;

namespace Window
{
    /// <summary>
    /// Int8Array typed array.
    /// </summary>
    [NetJs.External]
    public class Int8Array
    {
        public extern Int8Array(int length);
        public extern Int8Array(Array buffer, int byteOffset = 0, int? length = null);
        public extern Int8Array(ArrayBuffer buffer, int byteOffset = 0, int? length = null);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern sbyte this[int index] { get; set; }
        public extern Int8Array slice(int start = 0, int? end = null);
        public static extern bool isView(object arg);
    }
}