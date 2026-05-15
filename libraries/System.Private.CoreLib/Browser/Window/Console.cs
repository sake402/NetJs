using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// Browser console APIs.
    /// </summary>
    [NetJs.External]
    public class Console
    {
        public extern void log([NetJs.Spread] params object?[] args);
        public extern void info([NetJs.Spread] params object?[] args);
        public extern void warn([NetJs.Spread] params object?[] args);
        public extern void error([NetJs.Spread] params object?[] args);
        public extern void debug([NetJs.Spread] params object?[] args);
        public extern void assert(bool condition, [NetJs.Spread] params object?[] args);
        public extern void clear();
        public extern void dir(object? obj);
        public extern void trace();
        public extern void time(string label);
        public extern void timeEnd(string label);
    }
}