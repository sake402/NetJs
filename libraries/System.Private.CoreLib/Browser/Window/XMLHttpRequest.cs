using System;

namespace Window
{
    /// <summary>
    /// XMLHttpRequest wrapper.
    /// </summary>
    [NetJs.External]
    public class XMLHttpRequest : EventTarget
    {
        public extern XMLHttpRequest();
        public extern string? responseType { get; set; }
        public extern int readyState { get; }
        public extern int status { get; }
        public extern string? statusText { get; }
        public extern object? response { get; }
        public extern string? responseText { get; }
        public extern void open(string method, string url, bool async = true, string? username = null, string? password = null);
        public extern void send(object? data = null);
        public extern void abort();
        public extern void setRequestHeader(string header, string value);
        public extern string? getResponseHeader(string header);
        public extern string[] getAllResponseHeaders();
    }
}