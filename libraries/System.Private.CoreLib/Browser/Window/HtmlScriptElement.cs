namespace Window
{
    [NetJs.External]
    public class HtmlScriptElement : HTMLElement
    {
        public extern string? src { get; set; }
        public extern string? type { get; set; }
        public extern bool async { get; set; }
        public extern bool defer { get; set; }
        public extern string? text { get; set; }
        public extern NativeAction<Event> onload { get; set; }
        public extern NativeAction<Event> onerror { get; set; }
    }
}