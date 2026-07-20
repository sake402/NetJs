using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text;


namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

/// <summary>
/// Provides a service for loading assemblies at runtime in a browser context.
///
/// Supports finding pre-loaded assemblies in a server or pre-rendering context.
/// </summary>
public sealed partial class LazyAssemblyLoader
{
    private partial class LazyAssemblyLoaderInterop
    {
        public static extern partial Task<bool> LoadLazyAssembly(string assemblyToLoad);
    }
}