namespace NetJs.Translator.CSharpToJavascript
{
    public class SymbolExport
    {
        bool _isDynamic;
        public void IsDynamic()
        {
            _isDynamic = true;
        }
        public string Signature { get; set; } = default!;
        ulong? _handle;
        public ulong? Handle
        {
            get => _handle;
            set
            {
                //if (value == 0 || (value & ReflectionHandleExtension.AssemblyMask) == 0 || ((value & ReflectionHandleExtension.TypeMask) == 0 && !_isDynamic) /*|| (value & ReflectionHandleExtension.MemberMask) == 0*/)
                    //throw new InvalidOperationException();
                _handle = value;
            }
        }
        public List<ulong>? Dependencies { get; set; }
        public List<ulong>? WeakDependencies { get; set; }
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
        public Dictionary<string, SymbolExport> Assemblies { get; set; } = new();
        public Dictionary<string, SymbolExport> Namespaces { get; set; } = new();
        public Dictionary<string, SymbolExport> Types { get; set; } = new();
        public Dictionary<string, Dictionary<string, SymbolExport>> Members { get; set; } = new();
        public List<ILLinkerAssembly> LinkerSubstitutions { get; set; } = new();
        public override string ToString()
        {
            return AssemblySlug ?? base.ToString()!;
        }
    }
}