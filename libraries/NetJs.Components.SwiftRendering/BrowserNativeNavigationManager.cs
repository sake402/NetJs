using System;
using System.Linq;
using static H5.Core.dom;
using Microsoft.AspNetCore.Components.Routing;
using System.Threading.Tasks;
using BlazorJs.Core;
using Microsoft.AspNetCore.Components;

namespace BlazorJs.Core
{
    internal partial class BrowserNativeNavigationManager : NavigationManager, INavigationInterception
    {
        public BrowserNativeNavigationManager()
        {
            Initialize("", window.location.pathname + window.location.search + window.location.hash);
            //we are on the client, always enable navigation interception
            EnableNavigationInterceptionAsync().FireAndForget();
        }

        void LinkClicked(Event _event)
        {
            var href = ((HTMLElement)_event.currentTarget).getAttribute("href");
            _event.preventDefault();
            var currentHRef = window.location.href + window.location.search;
            if (!currentHRef.Equals(href, StringComparison.InvariantCultureIgnoreCase))
            {
                NavigateToCore(href, new NavigationOptions { }, isIntercepted: true)
                    .FireAndForget();
                //NavigateTo(href, isIntercepted: true, reload: false);
            }
        }

        void LinkMutationCallback(MutationRecord[] mutations, MutationObserver observer)
        {
            for (int i = 0; i < mutations.Length; i++)
            {
                foreach (var item in mutations[i].addedNodes)
                {
                    if (item.nodeName.Equals("A", StringComparison.InvariantCultureIgnoreCase))
                        item.addEventListener("click", LinkClicked);
                }
            }
        }

        bool enabledNavigationInterception;
        public Task EnableNavigationInterceptionAsync()
        {
            if (!enabledNavigationInterception)
            {
                enabledNavigationInterception = true;
                var observer = new MutationObserver(LinkMutationCallback);
                observer.observe(document.body, new MutationObserverInit
                {
                    childList = true,
                    attributeFilter = new string[] { "href" },
                    subtree = true
                });
                var links = document.querySelectorAll("*[href]");
                links.FirstOrDefault((link) =>
                {
                    link.addEventListener("click", LinkClicked);
                    return false;
                });
                window.addEventListener("popstate", (Event _event) =>
                {
                    var popEvent = (PopStateEvent)_event;
                    NavigateToCore(window.location.pathname + window.location.search + window.location.hash, new NavigationOptions { HistoryEntryState = popEvent.state }, isIntercepted: false)
                    .FireAndForget();
                });
                //window.addEventListener("beforeunload", (Event _event) =>
                //{
                //    ((BeforeUnloadEvent)_event).returnValue = false;
                //});
            }
            return Task.CompletedTask;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigateToCore(uri, options, isIntercepted: false)
                .FireAndForget();
        }

        async Task NavigateToCore(string uri, NavigationOptions options, bool isIntercepted)
        {
            var _continue = await NotifyLocationChangingAsync(uri, options.HistoryEntryState, isIntercepted);
            if (_continue)
            {
                if (options.ForceLoad)
                {
                    window.location.href = uri;
                }
                else
                {
                    if (options.ReplaceHistoryEntry)
                    {
                        window.history.replaceState(null, "", uri);
                    }
                    else
                    {
                        window.history.pushState(null, "", uri);
                    }
                }
                Uri = uri;
                NotifyLocationChanged(isIntercepted);
            }
        }

        bool navigationLocked;
        protected override void SetNavigationLockState(bool value)
        {
            navigationLocked = value;
        }
    }
}
