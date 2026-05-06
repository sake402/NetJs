using System;

namespace Window
{
    /// <summary>
    /// URL wrapper.
    /// </summary>
    [NetJs.External]
    public class URL
    {
        public extern URL(string url, string? baseUrl = null);
        public extern string href { get; set; }
        public extern string protocol { get; set; }
        public extern string host { get; set; }
        public extern string hostname { get; set; }
        public extern string pathname { get; set; }
        public extern string search { get; set; }
        public extern string hash { get; set; }
        public extern string origin { get; }
        public extern string toString();
        public extern URLSearchParams searchParams { get; }
    }
}