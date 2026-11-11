namespace NetJs.Translator.CSharpToJavascript
{
    public struct WeakDependency
    {
        public ulong Handle { get; set; }
        public KeepDependencyFlags Condition { get; set; }
    }

    public class SymbolExport
    {
        bool _isDynamic;
        public void IsDynamic()
        {
            _isDynamic = true;
        }
        string? ovName;
        public string OverloadName
        {
            get => ovName ?? "Null"; //out yaml serializer deserializes Null string into null
            set => ovName = value;
        }
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
        public List<WeakDependency>? WeakDependencies { get; set; }
        public override string ToString()
        {
            return OverloadName;
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