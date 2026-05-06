using NetJs;

namespace System
{
    public interface IPromise
    {
        [Name("then")]
        IPromise Then([NativeDelegate] Action<object?> continuation);
        [Name("then")]
        IPromise Then([NativeDelegate] Action<object?> continuation, [NativeDelegate] Action<object?> onRejected);
        [Name("catch")]
        IPromise Catch([NativeDelegate] Action<object?> continuation);
    }

    public interface IPromise<T> : IPromise
    {
        [Name("then")]
        Promise<T> Then([NativeDelegate] Action<T> continuation);
        [Name("then")]
        Promise<T> Then([NativeDelegate] Action<T> continuation, [NativeDelegate] Action<object?> onRejected);
    }

    [Name("Promise")]
    [IgnoreGeneric]
    [External]
    public class Promise
    {

    }

    [Name("Promise")]
    [IgnoreGeneric]
    [External]
    public class Promise<T> : Promise, IPromise<T>
    {
        [NativeDelegate]
        public delegate void Resolver(Union<T, Promise<T>> value);
        [NativeDelegate]
        public delegate void Rejector(object? readon);
        [NativeDelegate]
        public delegate void Executor(Resolver resolve, Rejector reject);
        public extern Promise();
        public extern Promise(Executor executor);
        [Name("then")]
        public extern Promise<T> Then([NativeDelegate] Action<T> onFullfilled);
        [Name("then")]
        public extern Promise<T> Then([NativeDelegate] Action<T> onFullfilled, [NativeDelegate] Action<object?> onRejected);
        [Name("catch")]
        public extern Promise<T> Catch([NativeDelegate] Action<object?> continuation);
        extern IPromise IPromise.Then([NativeDelegate] Action<object?> continuation);
        extern IPromise IPromise.Then([NativeDelegate] Action<object?> continuation, [NativeDelegate] Action<object?> onRejected);
        extern IPromise IPromise.Catch([NativeDelegate] Action<object?> continuation);


        [Name("all")]
        public static extern Promise<T?> All(params IPromise[] promises);
        [Name("all")]
        public static extern Promise<TResult[]> All<TResult>(params IPromise<TResult>[] promises);
        [Name("allSettled")]
        public static extern Promise<T?> AllSettled(params IPromise[] promises);
        [Name("any")]
        public static extern Promise<T?> Any(params IPromise[] promises);
        [Name("race")]
        public static extern Promise<T?> Race(params IPromise[] promises);
        [Name("reject")]
        public static extern Promise<T?> Reject(object? reason);
        [Name("resolve")]
        public static extern Promise<T?> Resolve(object? reason);

    }
}