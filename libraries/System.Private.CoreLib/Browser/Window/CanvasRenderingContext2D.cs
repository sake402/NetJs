using System;

namespace Window
{
    /// <summary>
    /// 2D drawing context.
    /// </summary>
    [NetJs.External]
    public class CanvasRenderingContext2D
    {
        public extern string fillStyle { get; set; }
        public extern string strokeStyle { get; set; }
        public extern double lineWidth { get; set; }

        public extern void fillRect(double x, double y, double w, double h);
        public extern void clearRect(double x, double y, double w, double h);
        public extern void strokeRect(double x, double y, double w, double h);
        public extern void beginPath();
        public extern void closePath();
        public extern void moveTo(double x, double y);
        public extern void lineTo(double x, double y);
        public extern void stroke();
        public extern void fill();
        public extern ImageData createImageData(int width, int height);
        public extern ImageData getImageData(int sx, int sy, int sw, int sh);
        public extern void putImageData(ImageData imageData, double dx, double dy);
    }
}