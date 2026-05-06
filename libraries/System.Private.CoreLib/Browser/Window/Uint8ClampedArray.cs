using System;

namespace Window
{
    /// <summary>
    /// Typed array wrapper used by ImageData.
    /// </summary>
    [NetJs.External]
    public class Uint8ClampedArray
    {
        public extern int length { get; }
        public extern byte this[int index] { get; set; }
        public extern Uint8ClampedArray(int length);
    }
}