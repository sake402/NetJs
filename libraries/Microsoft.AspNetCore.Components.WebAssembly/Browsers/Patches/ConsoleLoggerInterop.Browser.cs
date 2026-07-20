using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text;


namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal static partial class ConsoleLoggerInterop
{
    public static extern partial void ConsoleDebug(string message);
    public static extern partial void ConsoleInfo(string message);
    public static extern partial void ConsoleWarn(string message);
    public static extern partial void ConsoleError(string message);
    public static extern partial void DotNetCriticalError(string message);
}
