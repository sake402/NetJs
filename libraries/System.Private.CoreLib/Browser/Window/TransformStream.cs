using System;

namespace Window
{
    [NetJs.External]
    public class TransformStream<TInput, TOutput>
    {
        public extern TransformStream(object? transformer = null, object? writableStrategy = null, object? readableStrategy = null);
        public extern WritableStream<TInput> writable { get; }
        public extern ReadableStream<TOutput> readable { get; }
    }
}