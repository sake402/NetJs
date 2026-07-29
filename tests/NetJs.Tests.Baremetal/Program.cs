
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace NetJs.Tests.Baremetal
{
    public static class Program
    {
        public static string A(this string a)
        {
            return a;
        }
        public static string B(this string b)
        {
            return b;
        }
        public static string C(this string b)
        {
            return b;
        }
        public static string D(this string b)
        {
            return b;
        }
        public static string E(this string b)
        {
            return b;
        }
        public static async Task Main()
        {
            var len = "".A()?.B()?.C().Trim().D()?.E().Length.ToString().A()?.B()?.C()?.D()?.E();
            //var builder = WebAssemblyHostBuilder.CreateDefault(args);
            //builder.RootComponents.Add<App>("#app");
            //builder.RootComponents.Add<HeadOutlet>("head::after");

            //builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            //await builder.Build().RunAsync();

            int i = 0;
            while (true)
            {
                await Task.Delay(1000);
                Console.WriteLine($"{DateTime.Now}: Hello World {i++} again, {Random.Shared.Next()}, {Guid.NewGuid()}");
            }
        }
    }
}
