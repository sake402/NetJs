using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    /// Js simply requires the "then" method to make await work
    /// </summary>
    public interface IAwaitable : IGetAwaiter
    {
        [Name("then")]
        void Then(NativeAction<object> continuation, NativeAction<object?> onRejected)
        {
            var task = this.As<Task<object>>();
            var awt = this["GetAwaiter$1"].As<NativeFunction<object?>>();
            var awaiter = NetJs.Script.IsDefined(awt) ? NetJs.Script.Write<ICriticalNotifyCompletion>("awt.call(this)") : this.As<IGetAwaiter>().GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                //Not all wawaitable objects has results and Exception
                //We check if there is such properties,
                //if there isn't, we attept to access result anyway
                //And if it throws, we reject the promise
                var exception = task.Exception;
                if (!NetJs.Script.IsUndefinedOrNull(exception))
                {
                    onRejected(exception);
                }
                else
                {
                    //var isCompleted = task.IsCompleted;
                    //if (NetJs.Script.IsUndefined(isCompleted))
                    //{
                    //    isCompleted = true;
                    //}
                    //if (isCompleted)
                    //{
                    object result;
                    try
                    {
                        if (NetJs.Script.IsDefined(task["GetResult"]))
                            result = task["GetResult"].As<NativeFunction<object>>()();
                        else if (NetJs.Script.IsDefined(awaiter["GetResult"]))
                            result = awaiter["GetResult"].As<NativeFunction<object>>()();
                        else
                            result = task.Result;
                    }
                    catch (Exception e)
                    {
                        onRejected(e);
                        return;
                    }
                    continuation(result);
                    //}
                    //else
                    //    onRejected(task.Exception);
                }
            });
        }
    }
}