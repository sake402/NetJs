using System;

namespace Window
{
    /// <summary>
    /// HTML canvas element.
    /// </summary>
    [NetJs.External]
    public class HTMLCanvasElement : HTMLElement
    {
        public extern int width { get; set; }
        public extern int height { get; set; }
        public extern CanvasRenderingContext2D? getContext(string contextId, object? options = null);
        public extern Blob toBlob(Action<Blob> callback, string? type = null, double? quality = null);
        public extern string toDataURL(string? type = null, double? quality = null);
    }
}