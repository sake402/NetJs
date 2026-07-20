using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace BlazorJs.Core
{
    public class BrowserNativeErrorBoundaryLogger : IErrorBoundaryLogger
    {
        public Task LogErrorAsync(Exception exception)
        {
            Console.Write(exception.Message);
            return Task.CompletedTask;
        }
    }
}
