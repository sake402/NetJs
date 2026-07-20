using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

internal sealed partial class WebAssemblyRenderer 
{
    private static extern unsafe partial void RenderBatch(int id, void* batch);
}
