namespace Window
{
    [NetJs.External]
    public class HTMLInputElement : HTMLElement
    {
        public extern string type { get; }
        public extern bool @checked { get; }
        public extern string? value { get; }
    }
}