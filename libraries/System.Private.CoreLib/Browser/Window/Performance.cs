using System;

namespace Window
{
    /// <summary>
    /// Performance timing API.
    /// </summary>
    [NetJs.External]
    public class Performance
    {
        public extern double now();
        public extern void mark(string name);
        public extern void clearMarks(string? name = null);
        public extern void measure(string name, string? startMark = null, string? endMark = null);
        public extern void clearMeasures(string? name = null);
    }
}