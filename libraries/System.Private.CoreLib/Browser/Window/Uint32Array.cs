using System;

namespace Window
{
    [NetJs.External]
    public class Uint32Array
    {
        public extern Uint32Array(int length);
        public extern Uint32Array(Union<Array, ArrayBuffer> buffer);
        public extern Uint32Array(Union<Array, ArrayBuffer> buffer, int byteOffset);
        public extern Uint32Array(Union<Array, ArrayBuffer> buffer, int byteOffset, int length);
        public extern int length { get; }
        public extern ArrayBuffer buffer { get; }
        public extern uint this[int index] { get; set; }
        public extern Uint32Array slice(int start = 0, int? end = null);
    }
}