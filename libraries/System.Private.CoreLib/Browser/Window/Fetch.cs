using System;
using System.Threading.Tasks;

namespace Window
{
    /// <summary>
    /// Fetch helper (global fetch function).
    /// </summary>
    [NetJs.External]
    public static class Fetch
    {
        public static extern Promise<Response> fetch(string input, object? init = null);
    }
}