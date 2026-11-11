using System;
using System.Runtime.CompilerServices;

namespace Window
{
    /// <summary>
    /// Represents an HTML element.
    /// </summary>
    [NetJs.External]
    public class HTMLElement : Element
    {
        public extern string? innerHTML { get; set; }
        public extern string? outerHTML { get; set; }
        public extern string? innerText { get; set; }
        //public extern string? textContent { get; set; }

        public extern bool hidden { get; set; }
        public extern string? title { get; set; }
        public extern string? lang { get; set; }
        public extern Dataset dataset { get; }
        public extern void click();
        public extern void scrollIntoView(object? options = null);
    }

    [NetJs.External]
    public class Dataset
    {
        [NetJs.Template("{this}[{key}] = {data}")]
        public extern void set(string key, string data);
        [NetJs.Template("{this}[{key}]")]
        public extern string get(string key);
    }
}