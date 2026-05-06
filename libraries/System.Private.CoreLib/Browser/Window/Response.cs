using System;

namespace Window
{
    /// <summary>
    /// Represents a Fetch API Response.
    /// </summary>
    [NetJs.External]
    public class Response : Body
    {
        public extern Response(object? body = null, object? init = null);
        public extern int status { get; }
        public extern string? statusText { get; }
        public extern bool ok { get; }
        public extern Headers headers { get; }
        public extern string? type { get; }
        public extern string? url { get; }
        public extern Response clone();
        public static extern Response error();
        public static extern Response redirect(string url, int status);
    }
}