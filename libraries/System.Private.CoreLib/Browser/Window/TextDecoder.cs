namespace Window
{
    [NetJs.External]
    public class TextDecoder
    {
        public extern TextDecoder(string encoding);
        public extern string decode(Union<Uint8Array, Uint16Array> array);
    }
}