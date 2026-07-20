using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text;


namespace Microsoft.JSInterop.Implementation;

public partial class JSInProcessObjectReference 
{
    private static extern partial void DisposeJSObjectReferenceById(long id);
}
