using NetJs.Translator.CSharpToJavascript;
using YamlDotNet.Serialization;

namespace NetJs.Translator
{
    [YamlStaticContext]
    [YamlSerializable(typeof(SymbolDescriptor))]
    [YamlSerializable(typeof(SymbolExport))]
    [YamlSerializable(typeof(WeakDependency))]
    [YamlSerializable(typeof(ILLinkerAssembly))]
    [YamlSerializable(typeof(ILLinkerAssembly.Type))]
    [YamlSerializable(typeof(ILLinkerAssembly.Member))]
    [YamlSerializable(typeof(PackageModel))]
    [YamlSerializable(typeof((ulong, KeepDependencyFlags)))]
    //[YamlSerializable(typeof(List<(ulong, KeepDependencyFlags)>))]
    public partial class YamlSerializationContext : StaticContext { }
}
