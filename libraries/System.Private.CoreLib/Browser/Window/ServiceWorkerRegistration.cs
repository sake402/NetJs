using System;

namespace Window
{
    /// <summary>
    /// Service worker registration.
    /// </summary>
    [NetJs.External]
    public class ServiceWorkerRegistration
    {
        public extern ServiceWorker? installing { get; }
        public extern ServiceWorker? waiting { get; }
        public extern ServiceWorker? active { get; }
        public extern void unregister();
        public extern Promise<object> update();
    }
}