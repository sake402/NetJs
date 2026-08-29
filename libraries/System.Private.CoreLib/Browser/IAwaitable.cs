using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NetJs
{
    //[NetJs.External]
    //public interface IGetAwaiter
    //{
    //    [Name("GetAwaiter")]
    //    ICriticalNotifyCompletion GetAwaiter();
    //}

    public static class AsyncResolver
    {
        //Make sure we always return task from a method that has Task return by wrapping the native promise into Task object
        public static TTaskType Async<TTaskType, TResult>(NativeFunction<Task> asyncCode)
        {
            var result = asyncCode();
            if (NetJs.Script.Write<bool>("result instanceof Promise"))
            {
                TaskCompletionSource? vResult = typeof(TResult) == typeof(void) ? new() : null;
                TaskCompletionSource<TResult>? tResult = typeof(TResult) != typeof(void) ? new() : null;
                result.As<IPromise>()
                   .Then((t) =>
                   {
                       vResult?.SetResult();
                       tResult?.SetResult(t.As<TResult>());
                   })
                   .Catch((e) =>
                   {
                       vResult?.SetException(e.As<Exception>());
                       tResult?.SetException(e.As<Exception>());
                   });
                if (typeof(TTaskType) == typeof(ValueTask))
                {
                    return new ValueTask(vResult!.Task).As<TTaskType>();
                }
                else if (typeof(TTaskType).As<RuntimeType>()._prototype.GenericArguments == 1 && typeof(TTaskType).As<RuntimeType>()._prototype.FullName.NativeStartsWith("System.Threading.Tasks.ValueTask<"))
                {
                    return new ValueTask<TResult>(tResult!.Task).As<TTaskType>();
                }
                else
                {
                    return (vResult?.Task ?? tResult!.Task).As<TTaskType>();
                }
            }
            return result.As<TTaskType>();
        }
    }

    /// <summary>
    /// The compiler will plug this interface automatically into any awaitable object.
    /// Js simply requires the "then" method to make await work
    /// </summary>
    public interface IAwaitable //: IGetAwaiter
    {
        [Name("then")]
        void Then(NativeAction<object> continuation, NativeAction<object?> onRejected)
        {
            static void CollectMethods(TypePrototype prototype, MethodModel[] allMethods)
            {
                var metadata = prototype.Metadata;
                if (NetJs.Script.IsDefined(metadata) && NetJs.Script.IsDefined(metadata!.Methods))
                {
                    allMethods.Push(metadata!.Methods!);
                }
                var basePrototype = prototype.Base;
                if (NetJs.Script.IsDefined(basePrototype) && !basePrototype!.Equals(Object.Prototype))
                {
                    CollectMethods(basePrototype, allMethods);
                }
            }
            var task = this.As<Task<object>>();
            var taskPrototype = Object.GetClassPrototypeOf(task);
            MethodModel[] allMethods = NetJs.Script.NewArray<MethodModel>();
            CollectMethods(taskPrototype, allMethods);
            var taskGetAwaiter = allMethods.Filter(m => m.Name.NativeEquals("GetAwaiter")).ArrayFirst();
            var taskGetResult = allMethods!.Filter(m => m.Name.NativeEquals("GetResult")).ArrayFirstOrDefault();
            var taskAwaiter = task[taskGetAwaiter.GetOutputName()].As<NativeFunction<ICriticalNotifyCompletion>>()();
            var taskAwaiterPrototype = Object.GetClassPrototypeOf(taskAwaiter);
            MethodModel[] allMethods2 = NetJs.Script.NewArray<MethodModel>();
            CollectMethods(taskAwaiterPrototype, allMethods2);
            var taskAwaiterGetResult = allMethods2.Filter(m => m.Name.NativeEquals("GetResult")).ArrayFirst();
            //var awaiter = NetJs.Script.IsDefined(awt) ? NetJs.Script.Write<ICriticalNotifyCompletion>("awt.call(this)") : this.As<IGetAwaiter>().GetAwaiter();
            taskAwaiter.OnCompleted(() =>
            {
                //Not all awaitable objects has results and Exception
                //We check if there is such properties,
                //if there isn't, we attempt to access result anyway
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
                        if (taskGetResult != null && NetJs.Script.IsDefined(task[taskGetResult.GetOutputName()]))
                            result = task[taskGetResult.GetOutputName()].As<NativeFunction<object>>()();
                        else if (NetJs.Script.IsDefined(taskAwaiter[taskAwaiterGetResult.GetOutputName()]))
                            result = taskAwaiter[taskAwaiterGetResult.GetOutputName()].As<NativeFunction<object>>()();
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