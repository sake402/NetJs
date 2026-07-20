using System;

namespace Window
{
    [NetJs.External]
    public class BigUint64Array
    {
        public extern BigUint64Array(ulong length);
        public extern BigUint64Array(Union<Array, ArrayBuffer> buffer);
        public extern BigUint64Array(Union<Array, ArrayBuffer> buffer, int byteOffset);
        public extern BigUint64Array(Union<Array, ArrayBuffer> buffer, int byteOffset, int length);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern ulong this[int index] { get; set; }
        public extern BigUint64Array slice(int start = 0, int? end = null);
    }
}