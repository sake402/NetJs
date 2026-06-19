using NetJs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Window
{
    [NetJs.External]
    public class Set
    {
        public extern Set();
        public extern bool has([Box(false)] object key);
        public extern void add([Box(false)] object key);
    }
}
