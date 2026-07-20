using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace BlazorJs.Core
{
    //Browser is ingle threaded, no syncronization context
    internal partial class BrowserNativeDispatcher : Dispatcher
    {
        public override bool CheckAccess()
        {
            return true;
        }

        public override Task InvokeAsync(Action workItem)
        {
            workItem();
            return Task.CompletedTask;
        }

        public override Task InvokeAsync(Func<Task> workItem)
        {
            return workItem();
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            var t = workItem();
            return Task.FromResult(t);
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            return workItem();
        }
    }
}
