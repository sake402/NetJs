
using System;
using System.Threading.Tasks;

namespace NetJs.Tests.Baremetal
{
    public static class Program
    {
        public static async Task Main()
        {
            int i = 0;
            while (true)
            {
                await Task.Delay(1000);
                Console.WriteLine($"{DateTime.Now}: Hello World {i++}, {Random.Shared.Next()}, {Guid.NewGuid()}");
            }
        }
    }
}
