using System;
using System.Threading.Tasks;

namespace Window
{
    /// <summary>
    /// Navigator.clipboard
    /// </summary>
    [NetJs.External]
    public class Clipboard
    {
        public extern Task<string> readText();
        public extern Task writeText(string data);
        public extern Task<object> read();
        public extern Task write(object data);
    }
}