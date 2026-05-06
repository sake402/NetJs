using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Window
{
    /// <summary>
    /// Navigator / user agent wrapper.
    /// </summary>
    [NetJs.External]
    public class Navigator
    {
        public extern string? userAgent { get; }
        public extern string? platform { get; }
        public extern bool onLine { get; }
        public extern string? language { get; }
        public extern string[]? languages { get; }

        public extern object? clipboard { get; }
        public extern USB? usb { get; }
    }
}