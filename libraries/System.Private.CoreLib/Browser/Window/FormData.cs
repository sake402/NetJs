using System;

namespace Window
{
    /// <summary>
    /// FormData collection.
    /// </summary>
    [NetJs.External]
    public class FormData
    {
        public extern FormData(object? form = null);
        public extern void append(string name, object value, string? fileName = null);
        public extern void delete(string name);
        public extern string? get(string name);
        public extern object? getAll(string name);
        public extern bool has(string name);
        public extern void set(string name, object value, string? fileName = null);
        public extern void forEach(Action<object?, string> callback);
    }
}