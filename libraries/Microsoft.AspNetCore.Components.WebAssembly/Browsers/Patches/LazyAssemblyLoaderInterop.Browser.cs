using NetJs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Window;


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
        [NetJs.NoJSImport]
        public static partial Task<bool> LoadLazyAssembly(string assemblyToLoad)
        {
            var promise = NetJs.Script.Write<IPromise<Assembly[]>>("{global.}$lazyLoad(assemblyToLoad)");
            var tcs = new TaskCompletionSource<bool>();
            promise.Then((asm) =>
            {
                tcs.SetResult(true);
            }).Catch((e) =>
            {
                tcs.SetResult(false);
            });
            return tcs.Task;
        }
    }
}