using System;

namespace Window
{
    /// <summary>
    /// Minimal WebGL rendering context.
    /// </summary>
    [NetJs.External]
    public class WebGLRenderingContext
    {
        public extern uint createBuffer();
        public extern void bindBuffer(uint target, uint buffer);
        public extern void bufferData(uint target, ArrayBuffer data, uint usage);
        public extern uint createTexture();
        public extern void bindTexture(uint target, uint texture);
        public extern void texImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, ArrayBuffer? pixels);
        public extern void viewport(int x, int y, int width, int height);
        public extern void clear(uint mask);
        public extern void clearColor(float r, float g, float b, float a);
    }
}