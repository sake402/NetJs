using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// Represents the global window object and Browser API surface.
    /// </summary>
    [NetJs.External]
    [NetJs.Name("window")]
    public class Window
    {
        public static extern Window Instance
        {
            [NetJs.Template("window")]
            get;
        }
        public static extern Document document { get; }
        public static extern Console console { get; }
        public static extern Navigator navigator { get; }
        public static extern Location location { get; }
        public static extern History history { get; }

        public static extern double innerWidth { get; }
        public static extern double innerHeight { get; }

        public static extern int setTimeout(Action handler, int timeout);
        public static extern int setInterval(Action handler, int timeout);
        public static extern void clearTimeout(int id);
        public static extern void clearInterval(int id);

        public static extern void alert(string message);
        public static extern bool confirm(string message);
        public static extern string? prompt(string message, string? defaultValue = null);

        public static extern void addEventListener(string type, object listener, object? options = null);
        public static extern void removeEventListener(string type, object listener, object? options = null);
        public static extern bool dispatchEvent(Event evt);

        public static extern Promise<Response> fetch(string input, FetchOption? init = null);
        public static extern Promise<Response> fetch(Request request);
    }
}