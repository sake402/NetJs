using System;
using System.Collections.Generic;
using System.Text;

namespace Window
{
    [NetJs.External]
    public static class Reflect
    {
        public static extern string[] ownKeys(object source);
        public static extern object? get(object source, string property);
        public static extern object? get(object source, string property, object receiver);
        public static extern bool set(object source, string property, object? value);
        public static extern bool set(object source, string property, object? value, object receiver);
    }
}
