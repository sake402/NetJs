using System;
using System.Collections.Generic;

namespace Window
{
    /// <summary>
    /// URLSearchParams wrapper.
    /// </summary>
    [NetJs.External]
    public class URLSearchParams
    {
        public extern URLSearchParams(string? init = null);
        public extern void append(string name, string value);
        public extern void delete(string name);
        public extern string? get(string name);
        public extern IEnumerable<string> getAll(string name);
        public extern bool has(string name);
        public extern void set(string name, string value);
        public extern void forEach(Action<string, string> callback);
        public extern string toString();
    }
}