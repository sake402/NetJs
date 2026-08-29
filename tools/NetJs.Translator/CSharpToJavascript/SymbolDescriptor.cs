namespace NetJs.Translator.CSharpToJavascript
{
    public class SymbolValue
    {
        public string Signature { get; set; } = default!;
        public ulong? Handle { get; set; }
        public override string ToString()
        {
            return Signature;
        }
    }

    public struct SymbolDescriptor
    {
        public SymbolDescriptor()
        {
        }
        public string? GlobalNamespace { get; set; }
        public string? AssemblySlug { get; set; }
        public Dictionary<string, SymbolValue> Namespaces { get; set; } = new();
        public Dictionary<string, SymbolValue> Types { get; set; } = new();
        public Dictionary<string, Dictionary<string, SymbolValue>> Members { get; set; } = new();
        public List<ILLinkerAssembly> LinkerSubstitutions { get; set; } = new();
        public override string ToString()
        {
            return AssemblySlug ?? base.ToString()!;
        }
    }
}