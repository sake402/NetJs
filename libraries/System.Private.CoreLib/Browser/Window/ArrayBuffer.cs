using System;
using System.Runtime.CompilerServices;

namespace Window
{
    [NetJs.External]
    public class ArrayBuffer
    {
        public extern ArrayBuffer(int length);
        public extern ArrayBuffer(int length, object? options = null);
        // Properties
        public extern int byteLength { get; }
        public extern bool detached { get; }
        public extern int maxByteLength { get; }
        public extern bool resizable { get; }

        // Static Methods
        public static extern bool isView(object arg);

        // Instance Methods
        public extern void resize(int newLength);

        public extern ArrayBuffer slice(int begin, int end = 0);

        public extern ArrayBuffer transfer(int newByteLength = -1);

        public extern ArrayBuffer transferToFixedLength(int newByteLength = -1);
    }

}