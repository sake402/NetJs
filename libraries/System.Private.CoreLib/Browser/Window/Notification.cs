using System;

namespace Window
{
    /// <summary>
    /// Web Notification API.
    /// </summary>
    [NetJs.External]
    public class Notification
    {
        public extern Notification(string title, object? options = null);
        public extern string? title { get; }
        public extern void close();
        public static extern string permission { get; }
        public static extern Promise<string> requestPermission();
    }
}