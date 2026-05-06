using System;

namespace Window
{
    /// <summary>
    /// Minimal ReadableStream wrapper.
    /// </summary>
    [NetJs.External]
    public class ReadableStream<T>
    {
        public extern ReadableStream(object? underlyingSource = null, object? queuingStrategy = null);
        public extern Promise<ReadableStreamDefaultReader<T>> getReader();
        public extern ReadableStream<T> pipeThrough<TResult>(TransformStream<T, TResult> transform);
        public extern ReadableStream<T> pipeTo(WritableStream<T> dest);
    }

    [NetJs.External]
    public class ReadableStreamDefaultReader<T>
    {
        public extern Promise<object> read();
        public extern Promise<object> releaseLock();
    }
}