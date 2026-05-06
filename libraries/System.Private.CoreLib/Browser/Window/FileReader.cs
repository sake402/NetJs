using System;

namespace Window
{
    /// <summary>
    /// FileReader wrapper.
    /// </summary>
    [NetJs.External]
    public class FileReader : EventTarget
    {
        public extern short readyState { get; }
        public extern object? result { get; }
        public extern Exception? error { get; }

        public extern void readAsArrayBuffer(Blob blob);
        public extern void readAsText(Blob blob, string? encoding = null);
        public extern void readAsDataURL(Blob blob);
        public extern void abort();
    }
}