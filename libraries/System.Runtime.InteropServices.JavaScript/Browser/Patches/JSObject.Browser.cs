using System;
using System.Collections.Generic;
using System.Text;


namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JSObject
    {
        public JSObject() : this(IntPtr.Zero, JSProxyContext.MainThreadContext)
        {

        }
    }
}
