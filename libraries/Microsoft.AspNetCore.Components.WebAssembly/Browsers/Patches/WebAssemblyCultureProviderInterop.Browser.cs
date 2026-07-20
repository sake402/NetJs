using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal partial class WebAssemblyCultureProvider
{
    private partial class WebAssemblyCultureProviderInterop
    {
        [NetJs.NoJSImport]
        public static /*extern*/ partial Task LoadSatelliteAssemblies(string[] culturesToLoad) => Task.CompletedTask;
    }
}