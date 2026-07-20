using System;

namespace Window
{
    /// <summary>
    /// Represents Fetch Headers.
    /// </summary>
    [NetJs.External]
    public class Headers
    {
        public extern Headers();
        public extern Headers(object? init = null);
        public extern void append(string name, string value);
        public extern void delete(string name);
        public extern string? get(string name);
        public extern bool has(string name);
        public extern void set(string name, string value);
        public extern HeadersIterator entries();
    }

    [NetJs.External]
    public class HeadersIterator
    {
        public extern void forEach(NativeAction<string[], int> callback);
    }
}