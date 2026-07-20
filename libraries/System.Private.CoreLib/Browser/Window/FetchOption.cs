using System;
using System.Threading.Tasks;

namespace Window
{
    [NetJs.External]
    [NetJs.ObjectLiteral]
    public class FetchOption
    {
        public string? method { get; set; }
        public Union<string?, ArrayBuffer?, TypedArray?, DataView?, Blob?, File?, URLSearchParams?, FormData?, ReadableStream<object>?>? body { get; set; }
        public Union<object, Headers, SimpleDictionary<object>>? headers { get; set; }
    }
}