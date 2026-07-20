using System;
using System.Collections.Generic;

namespace Window
{
    /// <summary>
    /// Represents a Fetch API Request.
    /// </summary>
    [NetJs.External]
    public class Request : Body
    {
        public extern Request(string input, FetchOption? init = null);
        public extern string? method { get; }
        public extern Headers headers { get; }
        public extern string? mode { get; }
        public extern string? credentials { get; }
        public extern string? cache { get; }
        public extern string? redirect { get; }
        public extern string? referrer { get; }
        public extern string? integrity { get; }
        public extern bool keepalive { get; }
        public extern string? destination { get; }
        public extern Request clone();
    }
}