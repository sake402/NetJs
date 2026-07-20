using System;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace BlazorJs.Core
{
    public class BrowserJavascriptRuntime : JSRuntime
    {
        protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            throw new NotImplementedException();
        }

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            throw new NotImplementedException();
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            throw new NotImplementedException();
        }
    }
}
