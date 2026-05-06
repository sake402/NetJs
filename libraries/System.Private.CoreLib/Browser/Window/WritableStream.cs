using System;

namespace Window
{
    [NetJs.External]
    public class WritableStream<T>
    {
        public extern WritableStream(object? underlyingSink = null, object? strategy = null);
        public extern Promise<object> getWriter();
        public extern Promise<object> abort(object? reason = null);
        public extern Promise<object> close();
    }

    [NetJs.External]
    public class WritableStreamDefaultWriter
    {
        public extern Promise<object> write(object chunk);
        public extern Promise<object> close();
        public extern Promise<object> releaseLock();
    }
}