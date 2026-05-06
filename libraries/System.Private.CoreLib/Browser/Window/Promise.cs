using System;

namespace Window
{
    /// <summary>
    /// JS Promise wrapper (minimal).
    /// </summary>
    [NetJs.External]
    public class Promise<T>
    {
        public extern Promise(Action<Action<T>, Action<object?>> executor);
        public extern Promise<TResult> then<TResult>(Func<T, TResult> onFulfilled);
        public extern Promise<T> @catch(Func<object?, T> onRejected);
        public static extern Promise<T> resolve(T value);
        public static extern Promise<T> reject(object? reason);
    }
}