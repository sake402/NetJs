using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using BlazorJs.ServiceProvider;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using H5;

namespace BlazorJs.Core
{
    [External]
    public partial class BrowserApplicationBuilder
    {
        internal BrowserServiceProvider Services { get; }
        internal DefaultComponentActivator ComponentActivator { get; }
        internal BrowserNativeRenderer Renderer { get; }
        internal BrowserNativeNavigationManager NavigationManager { get; }
        internal BrowserNativeErrorBoundaryLogger ErrorBoundaryLogger { get; }
        internal BrowserJavascriptRuntime JavascriptRuntime { get; }
        public HttpClient Http { get; }
        public BrowserApplicationBuilder()
        {
            Services = new BrowserServiceProvider();
            NavigationManager = new BrowserNativeNavigationManager();
            Http = new HttpClient();
            ErrorBoundaryLogger = new BrowserNativeErrorBoundaryLogger();
            JavascriptRuntime = new BrowserJavascriptRuntime();
            Services.AddSingleton<IServiceProvider>(Services);
            Services.AddSingleton(NavigationManager)
                .AddSingleton<NavigationManager>(NavigationManager)
                .AddSingleton<INavigationInterception>(NavigationManager);
            Services.AddSingleton<IErrorBoundaryLogger>(ErrorBoundaryLogger);
            Services.AddSingleton<IJSRuntime>(JavascriptRuntime);
            Services.AddSingleton(Http);
            ComponentActivator = new DefaultComponentActivator(Services);
            Renderer = new BrowserNativeRenderer(Services, ComponentActivator);
        }

        public static BrowserApplicationBuilder Create(Action<BrowserApplicationBuilder> build = null)
        {
            var app = new BrowserApplicationBuilder();
            build?.Invoke(app);
            return app;
        }
        public static BrowserApplicationBuilder Create<TRootComponent>(Action<BrowserApplicationBuilder> build = null, Action<TRootComponent> buildComponent = null) where TRootComponent : IComponent
        {
            var app = new BrowserApplicationBuilder();
            app.Renderer.Add<TRootComponent>(buildComponent);
            build?.Invoke(app);
            return app;
        }
    }
}
