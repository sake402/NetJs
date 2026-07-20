using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// JS Promise wrapper (minimal).
    /// </summary>
    [NetJs.External]
    public class Promise<T>
    {
        public extern Promise(NativeAction<NativeAction<T>, NativeAction<object?>> executor);
        public extern Promise<TResult> then<TResult>(NativeFunction<T, TResult> onFulfilled);
        public extern Promise then(NativeAction<T> onFulfilled);
        public extern Promise<T> @catch(NativeFunction<object?, T> onRejected);
        public static extern Promise<T> resolve(T value);
        public static extern Promise<T> reject(object? reason);
        public extern Awaiter GetAwaiter();
        [NetJs.External]
        public struct Awaiter : INotifyCompletion
        {
            public extern bool IsCompleted { get; }
            public extern T GetResult();
            public extern void OnCompleted(Action continuation);
        }
    }
}