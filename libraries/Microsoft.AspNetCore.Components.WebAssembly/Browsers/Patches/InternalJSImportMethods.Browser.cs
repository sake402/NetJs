
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal partial class InternalJSImportMethods
{
    //[NetJs.Template("null")]
    private static extern partial string GetPersistedStateCore();

    private static extern partial Task<string> GetInitialUpdateCore();

    //[NetJs.Template("\"Development\"", "Debug")]
    //[NetJs.Template("\"Production\"", "Release")]
    [NetJs.NoJSImport]
    private static partial string GetApplicationEnvironmentCore()
    {
        var env = Window.Window.localStorage.getItem("__environment__");
        if (env is not null)
        {
            return env;
        }
        if (Window.Window.location.hostname.NativeEquals("localhost"))
            return "Development";
        return "Production";
    }

    private static extern partial void AttachRootComponentToElementCore(string domElementSelector, int componentId, int rendererId);

    private static extern partial void EndUpdateRootComponentsCore(long batchId);

    private static extern partial void NavigationManager_EnableNavigationInterceptionCore(int rendererId);

    private static extern partial void NavigationManager_ScrollToElementCore(string id);

    private static extern partial string NavigationManager_GetLocationHrefCore();

    private static extern partial string NavigationManager_GetBaseUriCore();

    private static extern partial void NavigationManager_SetHasLocationChangingListenersCore(int rendererId, bool value);

    [NetJs.Template("0")]
    private static extern partial int RegisteredComponents_GetRegisteredComponentsCountCore();

    private static extern partial string RegisteredComponents_GetAssemblyCore(int id);

    private static extern partial string RegisteredComponents_GetTypeNameCore(int id);

    private static extern partial string RegisteredComponents_GetParameterDefinitionsCore(int id);

    private static extern partial string RegisteredComponents_GetParameterValuesCore(int id);

}