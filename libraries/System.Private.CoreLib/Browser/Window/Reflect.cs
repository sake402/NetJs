using System;
using System.Collections.Generic;
using System.Text;

namespace Window
{
    [NetJs.External]
    public static class Reflect
    {
        public static extern string[] ownKeys(object source);
    }
}
