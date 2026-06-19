using System;
using System.Collections.Generic;
using System.Text;

namespace Window
{
    [NetJs.External]
    [NetJs.IgnoreGeneric]
    public class WeakRef<T>
    {
        public extern WeakRef(T target);
        public extern T deref();
        public extern T value
        {
            [NetJs.Template("{this}.deref()")]
             get;
        }
    }
}
