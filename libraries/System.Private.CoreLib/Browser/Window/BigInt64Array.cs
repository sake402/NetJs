using System;

namespace Window
{
    [NetJs.External]
    public class BigInt64Array
    {
        public extern BigInt64Array(long length);
        public extern BigInt64Array(Union<Array, ArrayBuffer> buffer);
        public extern BigInt64Array(Union<Array, ArrayBuffer> buffer, int byteOffset);
        public extern BigInt64Array(Union<Array, ArrayBuffer> buffer, int byteOffset, int length);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern long this[int index] { get; set; }
        public extern BigInt64Array slice(int start = 0, int? end = null);
    }
}