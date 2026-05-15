using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        Dictionary<string, string> aliasNamespace = new Dictionary<string, string>();
        List<string> importedNamespace = new List<string>();
        public IEnumerable<string> ImportedNamespace => importedNamespace;
        public IReadOnlyDictionary<string, string> AliasNamespace => aliasNamespace;
        //List<string> importedNamespace = new List<string>();
        Dictionary<string, List<string>> imports = new();

        List<string> alreadyTriedImport = new List<string>();
        void EnsureImported(string? typeName)
        {
            if (!_global.OutputMode.HasFlag(OutputMode.Module))
                return;
            if (Utilities.IsPredefinedTypeName(typeName))
                return;
            if (typeName == "dynamic")
                return;
            if (typeName?.EndsWith("[]") ?? false)
            {
                typeName = typeName.Substring(0, typeName.Length - 2);
            }
            if (_global.OutputMode.HasFlag(OutputMode.Module))
            {
                if (typeName != null)
                {
                    if (alreadyTriedImport.Contains(typeName))
                        return;
                    //find in syntaxtree list firrt
                    var targetType = _global.GetTypeDeclaration(typeName, this);
                    var name = typeName.Split('.').Last();
                    if (targetType != null)
                    {
                        if (!imports.TryGetValue(targetType.SyntaxTree.FilePath, out var list))
                        {
                            list = new List<string>();
                            imports[targetType.SyntaxTree.FilePath] = list;
                        }
                        if (!list.Contains(name))
                            list.Add(name);
                    }
                    else //find by symbols in other importted modules
                    {
                        var targetType2 = _global.TryGetSymbol(typeName, this/*, out _, out _*/);
                        if (targetType2 != null)
                        {
                            //var nm =$"{targetType.ContainingModule.Name}";
                            if (!imports.TryGetValue(targetType2.ContainingModule.Name, out var list))
                            {
                                list = new List<string>();
                                imports[targetType2.ContainingModule.Name] = list;
                            }
                            if (!list.Contains(name))
                                list.Add(name);
                        }
                    }
                    alreadyTriedImport.Add(typeName);
                }
            }
        }

        void EnsureImported(TypeSyntax? type)
        {
            if (type != null && _global.OutputMode.HasFlag(OutputMode.Module))
            {
                var symbol = _global.TryGetSymbol(type, this);
                if (symbol != null && !_global.ShouldExportType(symbol, this))
                    return;
                if (type.SyntaxTree != _tree)
                {
                    if (type is ArrayTypeSyntax arr)
                    {
                        EnsureImported(arr.ElementType);
                    }
                    else
                    {
                        var typeName = type.ToString().Trim();
                        EnsureImported(typeName);
                    }
                }
            }
        }

        void EnsureImported(ITypeSymbol? type)
        {
            if (type != null && _global.OutputMode.HasFlag(OutputMode.Module))
            {
                if (!_global.ShouldExportType(type, this))
                {
                    var typeName = type.Name.Trim();
                    EnsureImported(typeName);
                }
            }
        }
    }
}
