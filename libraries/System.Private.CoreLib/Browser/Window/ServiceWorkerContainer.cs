using System;

namespace Window
{
    /// <summary>
    /// Navigator.serviceWorker container.
    /// </summary>
    [NetJs.External]
    public class ServiceWorkerContainer : EventTarget
    {
        public extern Promise<ServiceWorkerRegistration> register(string scriptURL, object? options = null);
        public extern Promise<object> getRegistration(string scope = null);
        public extern ServiceWorker? controller { get; }
        public extern Promise<ServiceWorkerRegistration[]> getRegistrations();
    }
}