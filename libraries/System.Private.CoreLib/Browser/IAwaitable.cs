using System;
using System.Runtime.CompilerServices;

namespace NetJs
{
    [NetJs.External]
    public interface IGetAwaiter
    {
        [Name("GetAwaiter")]
        ICriticalNotifyCompletion GetAwaiter();
    }

    /// <summary>
    /// The compiler will plug this interface automatically into any awaitable object.
    /// Js simply required the then method to make await work
    /// </summary>
    public interface IAwaitable : IGetAwaiter
    {
        [Name("then")]
        void Then([NativeDelegate] Action continuation, [NativeDelegate] Action<object?> onRejected)
        {
            var awaiter = this.As<IGetAwaiter>().GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                //var ex
                continuation();
            });
        }
    }
}