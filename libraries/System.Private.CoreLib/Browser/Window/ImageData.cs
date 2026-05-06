using System;

namespace Window
{
    /// <summary>
    /// Image data container.
    /// </summary>
    [NetJs.External]
    public class ImageData
    {
        public extern int width { get; }
        public extern int height { get; }
        public extern Uint8ClampedArray data { get; }
        public extern ImageData(Uint8ClampedArray data, int width, int height);
    }
}