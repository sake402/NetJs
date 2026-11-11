using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    [NetJs.ObjectLiteral]
    class AwaitableCache
    {
        //internal MethodModel[] allMethods = default!;
        internal MethodModel taskGetAwaiter = default!;
        internal MethodModel taskGetResult = default!;
        //internal ICriticalNotifyCompletion taskAwaiter;
        internal MethodModel taskAwaiterGetResult = default!;
        internal PropertyModel taskAwaiterIsCompleted = default!;
    }

    public static class AsyncResolver
    {
        [NetJs.ObjectLiteral]
        internal class AsyncContext
        {
            public int AwaitCalled;
            public bool ResultAvailable;
            public object? Result;

            //public void Reset()
            //{
            //    WasCalled = false;
            //    Result = null;
            //}
        }

        static Window.Map<TypePrototype, AwaitableCache> _awaitableObjectCache = new();
        //const int SynchronousResultsBufferLenght = 256;
        //private static readonly SynchronouslyFulfilledPromiseResult[] _synchronousResultsBuffer = new SynchronouslyFulfilledPromiseResult[SynchronousResultsBufferLenght];
        //private static int _synchronousResultsBufferIndex = -1;

        //static AsyncResolver()
        //{
        //    unchecked
        //    {
        //        for (int i = 0; i < _synchronousResultsBuffer.Length; i++)
        //        {
        //            _synchronousResultsBuffer[i] = new SynchronouslyFulfilledPromiseResult();
        //        }
        //    }
        //}

        //class SynchronouslyFulfilledPromise
        //{
        //    internal readonly static SynchronouslyFulfilledPromise Instance = new SynchronouslyFulfilledPromise(null);
        //    object[] ResultStacks = NetJs.Script.NewArray<object>(1024);
        //    int nextResultStack;
        //    public SynchronouslyFulfilledPromise(object? result)
        //    {
        //        //Result = result;
        //    }
        //    [Name("$IsSynchronouslyFulfilledPromise")]
        //    internal bool ___FastCheckIsFulfilledPromise___ = true;
        //    //internal bool HasResult { get; set; }
        //    //internal object? Result { get; set; }
        //    //[Name("then")]
        //    //[TrimmerKeep]
        //    //void Then(NativeAction<object> continuation, NativeAction<object?> onRejected)
        //    //{
        //    //    NetJs.Script.Write("{0}.then = undefined;", this);
        //    //    continuation(this);
        //    //    NetJs.Script.Write("delete {0}.then;", this);
        //    //}
        //}


        //internal static SynchronouslyFulfilledPromiseResult PushSynchContext()
        //{
        //    unchecked
        //    {
        //        _synchronousResultsBufferIndex++;
        //        // Safety guard against deep recursion / stack overflow
        //        if (_synchronousResultsBufferIndex >= SynchronousResultsBufferLenght) _synchronousResultsBufferIndex = 0;

        //        //_synchronousResultsBuffer[_synchronousResultsBufferIndex].Reset();
        //        var context = _synchronousResultsBuffer[_synchronousResultsBufferIndex];
        //        context.ResultAvailable = false;
        //        context.Result = null;
        //        return context;
        //    }
        //}

        //internal static SynchronouslyFulfilledPromiseResult PopSynchContext()
        //{
        //    unchecked
        //    {
        //        var frame = _synchronousResultsBuffer[_synchronousResultsBufferIndex];
        //        _synchronousResultsBufferIndex--;
        //        return frame;

        //    }
        //}


        //Make sure we always return task from a method that has Task return by wrapping the native promise into Task object
        public static TTaskType Async<TTaskType, TResult>(NativeFunction<object, Task> asyncCode)
        {
            var asyncResult = new AsyncContext()
            {
                AwaitCalled = 0,
                //ResultAvailable = false,
                //Result = null
            };//  AsyncResolver.PushSynchContext();
            var result = asyncCode(asyncResult);
            //AsyncResolver.PopSynchContext();

            TaskCompletionSource? vResult = typeof(TResult) == typeof(void) ? new() : null;
            TaskCompletionSource<TResult>? tResult = typeof(TResult) != typeof(void) ? new() : null;
            if (asyncResult.ResultAvailable || asyncResult.AwaitCalled == 0) //have result syncronously already, ignore the js promise, return completed task
            {
                if (typeof(TResult) != typeof(void) &&
                    typeof(TResult).As<RuntimeType>()._prototype.Name != "Nullable$$" &&
                    typeof(TResult).IsValueType &&
                    (asyncResult.Result ?? null) == null)
                {
                    Debugger.Break();
                }
                if (tResult != null && NetJs.Script.IsUndefined(asyncResult.Result))
                {
                    Debugger.Break();
                }
                vResult?.SetResult();
                tResult?.SetResult(asyncResult.Result.As<TResult>());
            }
            //if (result["$IsSynchronouslyFulfilledPromise"].As<bool>() == true)
            //{
            //    vResult?.SetResult();
            //    tResult?.SetResult(result.As<SynchronouslyFulfilledPromise>().Result.As<TResult>());
            //}
            else if (NetJs.Script.Write<bool>("result instanceof Promise"))
            {
                var promise = result.As<Promise>();
                result.As<IPromise>()
                   .Then((t) =>
                   {
                       if (typeof(TResult) != typeof(void) &&
                           typeof(TResult).As<RuntimeType>()._prototype.Name != "Nullable$$" &&
                           typeof(TResult).IsValueType &&
                           (t ?? null) == null)
                       {
                           Debugger.Break();
                       }
                       if (tResult != null && NetJs.Script.IsUndefined(t))
                       {
                           Debugger.Break();
                       }
                       vResult?.SetResult();
                       tResult?.SetResult(t.As<TResult>());
                   })
                   .Catch((e) =>
                   {
                       Window.Window.console.error(e);
                       var ex = e;
                       if (ex is not Exception)
                       {
                           if (ex is string s)
                               ex = new Exception(s);
                           else if (ex is Error ee)
                           {
                               ex = new Exception(ee.message);
                           }
                       }
                       vResult?.SetException(ex.As<Exception>());
                       tResult?.SetException(ex.As<Exception>());
                   });
            }
            else
            {
                return result.As<TTaskType>();
            }
            if (typeof(TTaskType) == typeof(ValueTask))
            {
                return new ValueTask(vResult!.Task).As<TTaskType>();
            }
            else if (typeof(TTaskType).As<RuntimeType>()._prototype.GenericArguments == 1 &&
                typeof(TTaskType).As<RuntimeType>()._prototype.FullName.NativeStartsWith("System.Threading.Tasks.ValueTask<"))
            {
                return new ValueTask<TResult>(tResult!.Task).As<TTaskType>();
            }
            else
            {
                return (vResult?.Task ?? tResult!.Task).As<TTaskType>();
            }
        }

        /// <summary>
        /// An async return marker promise that is already fulfilled
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [TrimmerKeep]
        public static object? Result(object asyncResult, [NetJs.Box(false)] object? value)
        {
            unchecked
            {
                if (!asyncResult.As<AsyncContext>().ResultAvailable)
                {
                    asyncResult.As<AsyncContext>().ResultAvailable = true;
                    asyncResult.As<AsyncContext>().Result = value;
                }
                //if (_synchronousResultsBufferIndex >= 0)
                //{
                //    var currentFrame = _synchronousResultsBuffer[_synchronousResultsBufferIndex];
                //    currentFrame.WasCalled = true;
                //    currentFrame.Result = value;
                //}
                return value;
            }
            //SynchronouslyFulfilledPromise.Instance.Result = value;
            //SynchronouslyFulfilledPromise.Instance.HasResult = true;
            //return SynchronouslyFulfilledPromise.Instance;
            //return new SynchronouslyFulfilledPromise(value);
        }
        /// <summary>
        /// An async work is about to be awaited
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        [TrimmerKeep]
        public static object Awaiting(object asyncResult, [NetJs.Box(false)] object task)
        {
            unchecked
            {
                asyncResult.As<AsyncContext>().AwaitCalled++;
                return task;
            }
        }

        internal static AwaitableCache GetInfo(object taskLike)
        {
            var taskPrototype = Object.GetClassPrototypeOf(taskLike);
            if (_awaitableObjectCache.has(taskPrototype))
                return _awaitableObjectCache.get(taskPrototype);
            MethodModel[] allMethods = NetJs.Script.NewArray<MethodModel>();
            taskPrototype.CollectMethods(allMethods);
            var taskGetAwaiter = allMethods.filter(m => m.Name.NativeEquals("GetAwaiter")).ArrayFirst();
            var taskGetResult = allMethods!.filter(m => m.Name.NativeEquals("GetResult")).ArrayFirstOrDefault();
            var taskAwaiter = taskLike[taskGetAwaiter.GetOutputName()].As<NativeFunction<ICriticalNotifyCompletion>>()();
            var taskAwaiterPrototype = Object.GetClassPrototypeOf(taskAwaiter);
            MethodModel[] allMethods2 = NetJs.Script.NewArray<MethodModel>();
            taskAwaiterPrototype.CollectMethods(allMethods2);
            PropertyModel[] allProperty2 = NetJs.Script.NewArray<PropertyModel>();
            taskAwaiterPrototype.CollectProperties(allProperty2);
            var taskAwaiterIsCompleted = allProperty2.filter(m => m.Name.NativeEquals("IsCompleted")).ArrayFirst();
            var taskAwaiterGetResult = allMethods2.filter(m => m.Name.NativeEquals("GetResult")).ArrayFirst();
            var cache = new AwaitableCache
            {
                //allMethods = allMethods,
                taskGetAwaiter = taskGetAwaiter,
                taskGetResult = taskAwaiterGetResult,
                taskAwaiterGetResult = taskAwaiterGetResult,
                taskAwaiterIsCompleted = taskAwaiterIsCompleted
            };
            _awaitableObjectCache.set(taskPrototype, cache);
            return cache;
        }

        public static bool IsCompleted(object task, out object? result)
        {
            var cache = AsyncResolver.GetInfo(task);
            var taskAwaiter = task[cache.taskGetAwaiter.GetOutputName()].As<NativeFunction<ICriticalNotifyCompletion>>()();
            var isCompleted = taskAwaiter[cache.taskAwaiterIsCompleted.GetOutputName()].As<bool>();
            if (isCompleted)
            {
                if (cache.taskGetResult != null && NetJs.Script.IsDefined(task[cache.taskGetResult.GetOutputName()]))
                    result = task[cache.taskGetResult.GetOutputName()].As<NativeFunction<object>>()();
                else if (NetJs.Script.IsDefined(taskAwaiter[cache.taskAwaiterGetResult.GetOutputName()]))
                    result = taskAwaiter[cache.taskAwaiterGetResult.GetOutputName()].As<NativeFunction<object>>()();
                else
                    result = null;
                //if ( NetJs.Script.IsUndefined(result))
                //{
                //    Debugger.Break();
                //}
                return true;
            }
            result = null;
            return false;
        }

    }

    /// <summary>
    /// The compiler will plug this interface automatically into any awaitable object.
    /// Js simply requires the "then" method to make await work
    /// </summary>
    public interface IAwaitable //: IGetAwaiter
    {
        [Name("then")]
        [TrimmerKeep]
        void Then(NativeAction<object> continuation, NativeAction<object?> onRejected)
        {
            var task = this.As<Task<object>>();
            var cache = AsyncResolver.GetInfo(this);
            var taskAwaiter = task[cache.taskGetAwaiter.GetOutputName()].As<NativeFunction<ICriticalNotifyCompletion>>()();
            void onCompleted()
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
                    object result;
                    try
                    {
                        if (cache.taskGetResult != null && NetJs.Script.IsDefined(task[cache.taskGetResult.GetOutputName()]))
                            result = task[cache.taskGetResult.GetOutputName()].As<NativeFunction<object>>()();
                        else if (NetJs.Script.IsDefined(taskAwaiter[cache.taskAwaiterGetResult.GetOutputName()]))
                            result = taskAwaiter[cache.taskAwaiterGetResult.GetOutputName()].As<NativeFunction<object>>()();
                        else
                            result = task.Result;
                        var taskPrototype = Object.GetClassPrototypeOf(task);
                        if (NetJs.Script.IsDefined(taskPrototype.Arguments) && taskPrototype.Arguments![0].Flags.TypeHasFlag(TypeFlagsModel.IsValueType) && result == null)
                        {
                            Debugger.Break();
                        }
                    }
                    catch (Exception e)
                    {
                        onRejected(e);
                        return;
                    }
                    object? originalThen = NetJs.Script.IsDefined(result) ? result["then"] : null;
                    var isNestedTask = NetJs.Script.TypeOf(originalThen).NativeEquals("function");
                    if (isNestedTask)
                    {
                        // Temporarily hide the 'then' method on this specific instance 
                        // so the JS promise resolver treats it as a normal object.
                        result["then"] = NetJs.Script.Undefined;
                    }
                    continuation(result);
                    if (isNestedTask)
                    {
                        // Restore the 'then' method right after so it remains awaitable elsewhere
                        NetJs.Script.Delete(result, "then");
                    }
                }
            }
            if (AsyncResolver.IsCompleted(this, out _))
            {
                onCompleted();
            }
            else
            {
                taskAwaiter.OnCompleted(onCompleted);
            }
        }
    }
}