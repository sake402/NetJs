using System;

namespace Window
{
    [NetJs.External]
    public class CacheStorage
    {
        public extern Promise<Cache> open(string cacheName);
        public extern Promise<bool> has(string cacheName);
        public extern Promise<bool> delete(string cacheName);
        public extern Promise<string[]> keys();
        public extern Promise<Cache?> match(string request);
    }

    [NetJs.External]
    public class Cache
    {
        public extern Promise<object> match(string request);
        public extern Promise<object> matchAll(string request);
        public extern Promise<object> add(string request);
        public extern Promise<object> addAll(string[] requests);
        public extern Promise<object> put(string request, Response response);
        public extern Promise<object> delete(string request);
        public extern Promise<string[]> keys();
    }
}