using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using NetJs.Translator.OneOf.Types;
using Newtonsoft.Json.Linq;
using NuGet.ContentModel;
using NuGet.Packaging;
using NuGet.Packaging.Signing;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Sources;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.NodeTypeResolvers;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace NetJs.Translator.CSharpToJavascript
{

    public partial record class GlobalCompilationVisitor
    {
        public CSharpCompilation Compilation { get; internal set; }
        public ConcurrentDictionary<ISymbol, (string WithTypeParameter, string WithoutTypeParameter)> SignatureCache { get; } = new(SymbolEqualityComparer.Default);
        public IProject Project { get; }
        public IProjectOutputProvider Output { get; }
        public List<ITypeSymbol> ProcessedTypeNodes { get; } = new List<ITypeSymbol>();
        public Dictionary<SyntaxTree, TranslatorSyntaxVisitor> Visitors { get; } = new Dictionary<SyntaxTree, TranslatorSyntaxVisitor>();
        public Dictionary<INamedTypeSymbol, TranslatorSyntaxVisitor> TypeVisitors { get; } = new Dictionary<INamedTypeSymbol, TranslatorSyntaxVisitor>(SymbolEqualityComparer.Default);
        public Dictionary<INamedTypeSymbol, ScriptWriter> TypeWriters { get; } = new Dictionary<INamedTypeSymbol, ScriptWriter>(SymbolEqualityComparer.Default);
        public SymbolDescriptor Symbols { get; private set; } = new();
        public SymbolDescriptor ImportedSymbols { get; }

        bool ready;
        //public List<SyntaxNode> AllNodes { get; }
        //public struct TypeSyntaxCache
        //{
        //    public string FullName { get; set; }
        //    public IEnumerable<MemberDeclarationSyntax> Syntax { get; set; }
        //}
        //public Dictionary<string, TypeSyntaxCache> TypeNodes { get; }
        //public Dictionary<MemberDeclarationSyntax, TypeSyntaxCache> ReversedTypeNodes { get; }
        //public struct NamedSymbolCache
        //{
        //    /// <summary>
        //    /// Namespace type, method, property or field
        //    /// </summary>
        //    public ISymbol Symbol { get; set; }
        //}
        public Dictionary<string, SymbolMetadata> AllSymbols { get; private set; }
        //public Dictionary<string, NamedSymbolCache> TypeSymbols { get; }
        public Dictionary<ISymbol, SymbolMetadata> SymbolMetadatas { get; private set; }
        public Dictionary<SyntaxNode, IEnumerable<SymbolMetadata>> SyntaxSymbols { get; private set; }
        public Dictionary<string, IEnumerable<IMethodSymbol>> ExtensionMethods { get; private set; }
        //public Dictionary<string, IEnumerable<ITypeSymbol>> Delegates { get; private set; }
        public NetJsBuildFlags BuildFlags { get; }
        public string GlobalName { get; }
        public ReflectionMetadataBuilder Reflection { get; set; } = default!;
        bool _isSystemPrivateCoreLib;


        public List<string> ModuleInitializers = new();
        public void RegisterModuleInitializer(string methodCall)
        {
            ModuleInitializers.Add(methodCall);
        }

        public SymbolMetadata? GetMetadata(ISymbol symbol)
        {
            //Detect primary constructor parameter
            if (symbol.Kind == SymbolKind.Parameter &&
                symbol.ContainingSymbol.Kind == SymbolKind.Method &&
                symbol.ContainingSymbol is IMethodSymbol mmethod &&
                mmethod.IsPrimaryConstructor(this))
            {
                return new SymbolMetadata(this, null)
                {
                    Symbol = symbol,
                    OriginalOverloadName = symbol.Name,
                    OverloadName = symbol.Name
                };
            }
            var metadata = SymbolMetadatas.GetValueOrDefault(symbol);
            if (metadata != null)
                return metadata;
            metadata = SymbolMetadatas.GetValueOrDefault(symbol.OriginalDefinition);
            if (metadata == null && symbol.Kind == SymbolKind.Property && symbol is IPropertySymbol property && property.PartialDefinitionPart != null)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(property.PartialDefinitionPart.OriginalDefinition);
                if (metadata != null)
                    return metadata;
            }
            if (metadata == null && symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol method && method.PartialDefinitionPart != null)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(method.PartialDefinitionPart.OriginalDefinition);
                if (metadata != null)
                    return metadata;
            }
            if (metadata == null && symbol.Kind == SymbolKind.Event && symbol is IEventSymbol evt && evt.PartialDefinitionPart != null)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(evt.PartialDefinitionPart.OriginalDefinition);
                if (metadata != null)
                    return metadata;
            }
            if (metadata == null && symbol.Kind == SymbolKind.NamedType && symbol is INamedTypeSymbol type && type.ConstructedFrom != null)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(type.ConstructedFrom);
            }
            if (metadata == null && symbol.Kind == SymbolKind.DynamicType)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(SystemDynamic);
            }
            if (metadata == null && symbol.Kind == SymbolKind.Field && symbol is IFieldSymbol field && symbol.ContainingType.IsTupleType)
            {
                ////A named tuple field need to map back to the original Item1, Item2 name
                metadata = SymbolMetadatas.GetValueOrDefault(field.CorrespondingTupleField!.OriginalDefinition);
                if (metadata == null)
                {
                    var tuple = symbol.ContainingType.ConstructedFrom;
                    var item = tuple.GetMembers(field.Name).FirstOrDefault();
                    if (item != null)
                    {
                        metadata = SymbolMetadatas.GetValueOrDefault(item.OriginalDefinition);
                    }
                }
            }
            if (metadata != null)
            {
                return metadata with { Symbol = symbol };
                //if (symbol.Name.Contains("EqualityComparer"))
                //{

                //}
                ////if (symbol is INamedTypeSymbol tps && tps.Arity > 0 && tps.TypeArguments.Any(t => t is ITypeParameterSymbol))
                ////{
                ////    var invocationName = ComputeInvocatioNameForType(tps, metadata.OverloadName);
                ////    return metadata with { InvocationName = invocationName };
                ////}
                //if (symbol is INamedTypeSymbol ts && ts.Arity > 0 && ts.TypeArguments.Any(t => t is INamedTypeSymbol))
                //{
                //    var invocationName = ComputeInvocatioNameForType(ts, metadata.OverloadName);
                //    return metadata with { InvocationName = invocationName };
                //}
                ////if (symbol.ContainingType is INamedTypeSymbol ns &&
                ////        (
                ////            (ns.ContainingType is INamedTypeSymbol ns1 && ns1.Arity > 0 && ns1.TypeArguments.Any(t => t is INamedTypeSymbol)) ||
                ////            (ns.Arity > 0 && ns.TypeArguments.Any(t => t is INamedTypeSymbol))
                ////        )
                //// )
                ////{
                ////    var containigMetadata = GetRequiredMetadata(ns);
                ////    var invocationName = ComputeInvocatioNameForType(ns, containigMetadata.OverloadName);
                ////    return metadata with { InvocationName = invocationName };
                ////}
                //if (symbol is IMethodSymbol ms &&
                //        (
                //            (ms.ContainingType is INamedTypeSymbol nt2 && nt2.Arity > 0 && nt2.TypeArguments.Any(t => t is INamedTypeSymbol)) ||
                //            (ms.Arity > 0 && ms.TypeArguments.Any(t => t is INamedTypeSymbol))
                //        )
                // )
                //{
                //    var invocationName = ComputeInvocatioNameForMethod(ms, metadata.OverloadName);
                //    return metadata with { InvocationName = invocationName };
                //}
                //if (symbol is IFieldSymbol fs &&
                //        (
                //            (fs.ContainingType is INamedTypeSymbol nt3 && nt3.Arity > 0 && nt3.TypeArguments.Any(t => t is INamedTypeSymbol))
                //        //||
                //        //(fs.Arity > 0 && fs.TypeArguments.Any(t => t is INamedTypeSymbol))
                //        )
                // )
                //{
                //    var invocationName = ComputeInvocationNameForField(fs, metadata.OverloadName);
                //    return metadata with { InvocationName = invocationName };
                //}
                //if (symbol is IPropertySymbol ps &&
                //        (
                //            (ps.ContainingType is INamedTypeSymbol nt4 && nt4.Arity > 0 && nt4.TypeArguments.Any(t => t is INamedTypeSymbol))
                //        //||
                //        //(fs.Arity > 0 && fs.TypeArguments.Any(t => t is INamedTypeSymbol))
                //        )
                // )
                //{
                //    var invocationName = ComputeInvocatioNameForProperty(ps, metadata.OverloadName);
                //    return metadata with { InvocationName = invocationName };
                //}
            }
            return metadata;
        }

        public SymbolMetadata GetRequiredMetadata(ISymbol symbol)
        {
            return GetMetadata(symbol) ?? throw new InvalidOperationException();
        }

        //public GlobalCompilationVisitor(CSharpCompilation compilation, GlobalCompilationVisitor original)
        //{
        //}
        public GlobalCompilationVisitor(
            CSharpCompilation compilation,
            IProject project,
            IProjectOutputProvider output,
            bool isSystemPrivateCoreLib,
            IEnumerable<SymbolDescriptor> importedSymbols,
            bool compilationAssemblyIsEmpty = false)
        {
            Compilation = compilation;
            Project = project;
            Output = output;
            _isSystemPrivateCoreLib = isSystemPrivateCoreLib;
            GlobalName = Evaluate("Global", null) ?? "$";
            AllSymbols = new Dictionary<string, SymbolMetadata>();
            SymbolMetadatas = new Dictionary<ISymbol, SymbolMetadata>(SymbolEqualityComparer.Default);
            ExtensionMethods = new Dictionary<string, IEnumerable<IMethodSymbol>>();
            //Delegates = new Dictionary<string, IEnumerable<ITypeSymbol>>();
            Symbols = Symbols with
            {
                GlobalNamespace = GlobalName,
                //AssemblySlug = GetAssemblyGlobalSlug(compilation.Assembly)
            };
            ImportedSymbols = new SymbolDescriptor();
            $"Merging imported symbols".Profile(() =>
            {
                foreach (var m in importedSymbols)
                {
                    foreach (var kv in m.Assemblies)
                    {
                        //if (kv.Value.Signature == null)
                        //{
                        //    kv.Value.Signature = "Null";/*The yaml deserializer deserializes Null value as null*/
                        //}
                        ImportedSymbols.Assemblies.TryAdd(kv.Key, kv.Value);
                    }
                    foreach (var kv in m.Namespaces)
                    {
                        //if (kv.Value.Signature == null)
                        //{
                        //    kv.Value.Signature = "Null";/*The yaml deserializer deserializes Null value as null*/
                        //}
                        ImportedSymbols.Namespaces.TryAdd(kv.Key, kv.Value);
                    }
                    foreach (var kv in m.Types)
                    {
                        //if (kv.Value.Signature == null)
                        //{
                        //    kv.Value.Signature = "Null";/*The yaml deserializer deserializes Null value as null*/
                        //}
                        if (!ImportedSymbols.Types.TryAdd(kv.Key, kv.Value))
                            Console.WriteLine($"Warning: conflicting symbol type key \"{kv.Key}\"");
                    }
                    foreach (var kv in m.Members)
                    {
                        foreach (var kvv in kv.Value)
                        {
                            //if (kvv.Value.Signature == null)
                            //{
                            //    kvv.Value.Signature = "Null";/*The yaml deserializer deserializes Null value as null*/
                            //}
                        }
                        if (!ImportedSymbols.Members.TryAdd(kv.Key, kv.Value))
                            Console.WriteLine($"Warning: conflicting symbol member key \"{kv.Key}\"");
                    }
                    foreach (var kv in m.LinkerSubstitutions)
                    {
                        ImportedSymbols.LinkerSubstitutions.Add(kv);
                    }
                }
            });
            BuildFlags = project.GetBuildFlags();

            var resultList = new ConcurrentDictionary<ISymbol, bool>(-1, 100000, SymbolEqualityComparer.Default);
            ConcurrentDictionary<string, int> usedKeys = new();
            var allSymbols = new ConcurrentDictionary<string, SymbolMetadata>(-1, 100000);
            bool ProcessSymbol(ISymbol symbol)
            {
                //lock (resultList)
                {
                    if (!resultList.TryAdd(symbol, true))
                        return false;
                }
                var signatureWithTypeParameter = symbol.CreateSignature(this, withTypeParameterNames: true, withGlobalNamespace: true);
                var signatureWithoutTypeParameter = symbol.CreateSignature(this, withTypeParameterNames: false, withGlobalNamespace: true);

                var uniqueFullName = signatureWithoutTypeParameter;
                //lock (usedKeys)
                {
                    var i = usedKeys.GetValueOrDefault(signatureWithoutTypeParameter);
                    if (i > 0)
                    {
                        i++;
                        uniqueFullName = $"{uniqueFullName}${i}";// uniqueFullName + "$$" + i;
                    }
                    usedKeys[signatureWithoutTypeParameter] = i + 1;
                }

                string signature;
                if (symbol.Kind == SymbolKind.NamedType || symbol.Kind == SymbolKind.Namespace)
                {
                    signature = signatureWithTypeParameter;
                }
                else
                {
                    int lastDot = -1;
                    if (symbol.Kind == SymbolKind.Method || (symbol.Kind == SymbolKind.Property && Unsafe.As<IPropertySymbol>(symbol).IsIndexer))
                    {
                        var braceIndex = signatureWithTypeParameter.LastIndexOf('(');
                        if (braceIndex > 0)
                        {
                            lastDot = signatureWithTypeParameter.AsSpan()[..braceIndex].LastIndexOf('.');
                        }
                    }

                    if (lastDot < 0)
                        lastDot = signatureWithTypeParameter.LastIndexOf('.');

                    if (lastDot > 0)
                    {
                        if (signatureWithTypeParameter[lastDot - 1] == '.') // ..ctor
                            lastDot--;
                        signature = signatureWithTypeParameter.Substring(lastDot + 1);
                    }
                    else
                    {
                        signature = signatureWithTypeParameter;
                    }

                    //To make implicit/explicit methods signature unique, prepend the return type
                    if (symbol.Kind == SymbolKind.Method && (symbol.Name == "op_Implicit" || symbol.Name == "op_Explicit" || symbol.Name == "op_CheckedExplicit"))
                    {
                        var returnType = ((IMethodSymbol)symbol).ReturnType;
                        var returns = SymbolEqualityComparer.Default.Equals(symbol.ContainingType, returnType) ? "this" :
                            returnType.Kind == SymbolKind.TypeParameter ? returnType.Name :
                            returnType.CreateSignature(this, withTypeParameterNames: true, withGlobalNamespace: false, withAssemblySlugNamespace: false);

                        signature = $"{returns} {signature}";// returns + " " + signature;
                    }
                }

                SymbolExport? imported = null;
                switch (symbol.Kind)
                {
                    case SymbolKind.Assembly:
                        ImportedSymbols.Assemblies.TryGetValue(signature, out imported);
                        break;
                    case SymbolKind.NamedType:
                        ImportedSymbols.Types.TryGetValue(signature, out imported);
                        break;
                    case SymbolKind.Namespace:
                        ImportedSymbols.Namespaces.TryGetValue(signature, out imported);
                        break;
                    default:
                        var declaringSignature = symbol.ContainingType.CreateSignature(this, withTypeParameterNames: true, withAssemblySlugNamespace: true);
                        if (ImportedSymbols.Members.TryGetValue(declaringSignature, out var memberSymbols))
                        {
                            memberSymbols.TryGetValue(signature, out imported);
                        }
                        break;
                }

                var metadata = new SymbolMetadata(this, imported)
                {
                    Symbol = symbol,
                    OriginalFullName = signatureWithoutTypeParameter,
                    UniqueFullName = uniqueFullName,
                    Signature = signature,
                    DeclaringReferences = symbol.DeclaringSyntaxReferences
                };
                //lock (allSymbols)
                allSymbols[uniqueFullName] = metadata;
                return true;
            }
            $"Indexing symbols".Profile(() =>
            {
                if (!compilationAssemblyIsEmpty)
                    ProcessSymbol(compilation.Assembly);
                compilation.CollectSymbols(ProcessSymbol);
            });
            AllSymbols = allSymbols.ToDictionary(e => e.Key, e => e.Value);
            $"Collecting symbol metadatas".Profile(() =>
            {
                SymbolMetadatas = AllSymbols
                    //Some local methods with same signatures are clashing
                    //.Where(e => e.Value.Symbol.Kind != SymbolKind.Method || ((IMethodSymbol)(e.Value.Symbol)).MethodKind != MethodKind.LocalFunction)
                    //.GroupBy(e => e.Value.Symbol, SymbolEqualityComparer.Default)
                    .ToDictionary(e => e.Value.Symbol, e => e.Value, SymbolEqualityComparer.Default);
            });
            string ToNotation(string s, Notation n)
            {
                if (string.IsNullOrEmpty(s))
                    return s;

                switch (n)
                {
                    case Notation.LowerCase:
                        return s.ToLower();

                    case Notation.UpperCase:
                        return s.ToUpper();

                    case Notation.CamelCase:
                        // Fast Path: If the first char is already lowercase, return the original string pointer directly
                        if (char.IsLower(s[0]))
                            return s;

                        char[] camelChars = s.ToCharArray();
                        camelChars[0] = char.ToLower(camelChars[0]);
                        return new string(camelChars);

                    case Notation.PascalCase:
                        // Fast Path: If the first char is already uppercase, return the original string pointer directly
                        if (char.IsUpper(s[0]))
                            return s;

                        char[] pascalChars = s.ToCharArray();
                        pascalChars[0] = char.ToUpper(pascalChars[0]);
                        return new string(pascalChars);
                }

                return s;
            }


            //string ToNotation(string s, Notation n)
            //{
            //    switch (n)
            //    {
            //        case Notation.LowerCase:
            //            return s.ToLower();
            //        case Notation.UpperCase:
            //            return s.ToUpper();
            //        case Notation.CamelCase:
            //            return new string(s.ToCharArray().Select((ss, i) => i == 0 ? char.ToLower(ss) : ss).ToArray());
            //        case Notation.PascalCase:
            //            return new string(s.ToCharArray().Select((ss, i) => i == 0 ? char.ToUpper(ss) : ss).ToArray());
            //    }
            //    return s;
            //}

            int Hierarchy(INamedTypeSymbol t)
            {
                if (t == null) return 0;

                int depth = 0;

                if (t.ContainingType != null)
                    depth += Hierarchy(t.ContainingType);

                if (t.TypeKind == TypeKind.Interface)
                {
                    return depth + 1;
                }

                // Target is a class/struct/enum: add baseline + walk its base classes
                depth += 2;

                var currentBase = t.BaseType;
                while (currentBase != null)
                {
                    depth++;
                    currentBase = currentBase.BaseType;
                }

                return depth;
            }



            var assemblyNamespace = GetAssemblyGlobalSlug(Compilation.Assembly);
            Dictionary<ITypeSymbol, Dictionary<string, HashSet<string>>> usedMemberNames = new(SymbolEqualityComparer.Default);
            //var reversMemberNames = ImportedNames.Members.ToDictionary(e => e.Key, e => e.Value.ToDictionary(ee => ee.Value.Signature, ee => ee));

            void MarkMemberNameUsed(ITypeSymbol type, string overloadName, string nameUsed)
            {
                //lock (usedMemberNames)
                {
                    if (!usedMemberNames.TryGetValue(type, out var typeUsedMemberNames))
                    {
                        typeUsedMemberNames = new();
                        usedMemberNames[type] = typeUsedMemberNames;
                    }
                    if (!typeUsedMemberNames.TryGetValue(overloadName, out var overloadUsedNames))
                    {
                        overloadUsedNames = new HashSet<string>();
                        typeUsedMemberNames[overloadName] = overloadUsedNames;
                    }
                    overloadUsedNames.Add(nameUsed);
                }
            }

            bool IsMemberNameUsed(ITypeSymbol type, string overloadName, string tryUseName, out HashSet<string>? overloadUsedNames, bool create = true)
            {
                //lock (usedMemberNames)
                {
                    if (!usedMemberNames.TryGetValue(type, out var typeUsedMemberNames))
                    {
                        if (create)
                        {
                            typeUsedMemberNames = new();
                            usedMemberNames[type] = typeUsedMemberNames;
                        }
                    }
                    HashSet<string>? localOverloadUsedNames = null;
                    if (overloadName != null && typeUsedMemberNames != null && !typeUsedMemberNames.TryGetValue(overloadName, out localOverloadUsedNames))
                    {
                        if (create)
                        {
                            overloadUsedNames = localOverloadUsedNames = new HashSet<string>();
                            typeUsedMemberNames[overloadName] = overloadUsedNames;
                        }
                        else
                        {
                            overloadUsedNames = null!;
                        }
                    }
                    else
                    {
                        overloadUsedNames = localOverloadUsedNames!;
                    }
                    if (localOverloadUsedNames != null && localOverloadUsedNames.Contains(tryUseName))
                    {
                        return true;
                    }
                    if (typeUsedMemberNames != null)
                    {
                        foreach (var hs in typeUsedMemberNames.Values)
                        {
                            if (hs.Contains(tryUseName))
                            {
                                overloadUsedNames = hs;
                                return true;
                            }
                        }
                    }
                    if (type.BaseType != null)
                    {
                        return IsMemberNameUsed(type.BaseType.OriginalDefinition,
                            null!/*if we are checking base type, the original overload name doesnt matter anymore, we just need to be sure the name is used or not*/,
                            tryUseName, out _, false);
                    }
                    return false;
                }
            }

            string MinifyNamespace(string fullName)
            {
                if (string.IsNullOrEmpty(fullName))
                {
                    return fullName;
                }

                var builder = new StringBuilder(fullName.Length * 2);
                //builder.Append("/*");
                //builder.Append(fullName);
                //builder.Append("*/");
                int i = 0;
                while (i < fullName.Length)
                {
                    char c = fullName[i];
                    if (c == '$')
                    {
                        // If we hit a dollar-sign flag separator, append everything up to the next dot
                        builder.Append(c);
                        i++;

                        while (i < fullName.Length)
                        {
                            char nextChar = fullName[i];
                            builder.Append(nextChar);
                            i++;
                            if (nextChar == '.')
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        builder.Append(c);
                        i++;

                        while (i < fullName.Length)
                        {
                            if (fullName[i] == '.')
                            {
                                i++;
                                break;
                            }
                            i++;
                        }
                    }
                }
                return builder.ToString();
            }

            char[] shortNameHaystack = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToArray();
            string MinifyMemberLastNameSegment(ISymbol member, string memberName)
            {
                var originalMemberName = memberName;
                ITypeSymbol? containingType = member.ContainingType;
                if (memberName.Length == 0)
                    return memberName;
                string? suffix = null;
                if (memberName.EndsWith("$"))
                {
                    int ix = memberName.LastIndexOf('$');
                    while (ix >= 0 && memberName[ix] == '$')
                    {
                        ix--;
                    }
                    suffix = memberName.Substring(ix + 1);
                    memberName = memberName.Substring(0, ix + 1);
                }
                var nameSegmentStart = memberName.LastIndexOf('.') + 1;
                var prefix = memberName.Substring(0, nameSegmentStart);
                var toShortenNameLength = memberName.Length - nameSegmentStart;
                var toShortenName = memberName.Substring(nameSegmentStart);
                //if (toShortenName[0] == '$')
                //    return memberName;
                string shortName;
                if (toShortenNameLength <= 2 || toShortenName[0] == '$')
                {
                    shortName = memberName;
                }
                else
                {
                    shortName = "";
                    if (toShortenName.Contains('$')) //interface members names like SC$1$IE$1$Reset(), we want to minify eventually into something like SC$1$IE$1$R()
                    {
                        int ix = memberName.LastIndexOf('$');
                        shortName += toShortenName.Substring(0, ix - nameSegmentStart + 1);
                        nameSegmentStart += ix + 1;
                    }
                    bool NameAvailable(string name)
                    {
                        if (containingType != null)
                        {
                            var tryShortName = name + suffix;
                            if (!IsMemberNameUsed(containingType, toShortenName, tryShortName, out _) &&
                                !Symbols.Types.TryGetValue(tryShortName, out _) &&  //we already have a unique name, ignore the remaining camel
                                !Symbols.Namespaces.TryGetValue(tryShortName, out _))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            var tryShortName = prefix + name + suffix;
                            if (!Symbols.Types.TryGetValue(tryShortName, out _) &&  //we already have a unique name, ignore the remaining camel
                                !Symbols.Namespaces.TryGetValue(tryShortName, out _)) //make sure type and namespace names dont collide
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                    if (!BuildFlags.HasFlag(NetJsBuildFlags.ShortNamesCreateFromCamelCase))
                    {
                        bool breakOuterLoop = false;
                        for (int shortLen = 1; shortLen < 4; shortLen++)
                        {
                            for (int i = 0; i < shortNameHaystack.Length; i++)
                            {
                                string sNameHayStack = shortNameHaystack[i].ToString();
                                if (shortLen > 1)
                                {
                                    for (int j = 1; j < shortLen; j++)
                                    {
                                        sNameHayStack += shortNameHaystack[i].ToString();
                                    }
                                }
                                if (NameAvailable(sNameHayStack))
                                {
                                    shortName = sNameHayStack;
                                    breakOuterLoop = true;
                                    break;
                                }
                            }
                            if (breakOuterLoop)
                                break;
                        }
                    }
                    else
                    {
                        for (int i = nameSegmentStart; i < memberName.Length; i++)
                        {
                            bool shortNameChanged = false;
                            if (memberName[i] == '$')
                            {
                                while (i < memberName.Length) //keep generic argument $ marker
                                {
                                    shortName += memberName[i];
                                    i++;
                                    shortNameChanged = true;
                                }
                            }
                            else if (i == nameSegmentStart)
                            {
                                shortName += memberName[i];
                                shortNameChanged = true;
                            }
                            else if (i > 0 && (char.IsUpper(memberName[i]) || memberName[i] == '_' || memberName[i - 1] == '_') && (char.IsLower(memberName[i - 1]) || memberName[i - 1] == '_' || i == nameSegmentStart + 1))
                            {
                                shortName += memberName[i];
                                shortNameChanged = true;
                            }
                            if (shortNameChanged)
                            {
                                if (NameAvailable(shortName))
                                    break;
                            }
                        }
                    }
                }
                int nextTry = 1;
                string tryName;
                if (containingType != null)
                {
                    tryName = shortName + suffix;
                    var tryGlobalName = prefix + tryName;
                    while (IsMemberNameUsed(containingType, toShortenName, tryName, out _) ||
                            Symbols.Types.TryGetValue(tryGlobalName, out _) ||  //we already have a unique name, ignore the remaining camel
                            Symbols.Namespaces.TryGetValue(tryGlobalName, out _))
                    {
                        tryName = shortName + "$" + nextTry + suffix;
                        tryGlobalName = prefix + tryName;
                        nextTry++;
                    }
                    MarkMemberNameUsed(containingType, toShortenName, tryName);
                    tryName = prefix + tryName;
                }
                else
                {
                    tryName = prefix + shortName + suffix;
                    while (Symbols.Types.TryGetValue(tryName, out _) ||
                        Symbols.Namespaces.TryGetValue(tryName, out _)) //make sure type and namespace names dont collide
                    {
                        tryName = prefix + shortName + "$" + nextTry + suffix;
                        nextTry++;
                    }
                }
                return
                    //"/*" + originalMemberName.Replace("/*", "").Replace("*/", "") + "*/" +
                    tryName;
            }

            $"Preprocessing namespace symbols".Profile(() =>
            {
                Dictionary<IAssemblySymbol, HashSet<string>> usedNames = new(SymbolEqualityComparer.Default);
                //var reversedNamespaceNames = ImportedNames.Namespaces.ToDictionary(e => e.Value.Signature, e => e.Key);
                foreach (var name in AllSymbols!.Values.Where(e => e.Symbol.Kind == SymbolKind.Namespace)
                .OrderBy(e => e.Signature) //order determinititically to ensure we keep getting the same minified name result across various run
                )
                {
                    var nname = Unsafe.As<INamespaceSymbol>(name.Symbol);
                    bool isImportedType = !SymbolEqualityComparer.Default.Equals(nname.ContainingAssembly, compilation.Assembly);
                    if (isImportedType && ImportedSymbols.Namespaces.TryGetValue(name.Signature, out var nsEx))
                    {
                        var ovName = nsEx.OverloadName;
                        var overloadNames = ovName.Split('|');
                        if (overloadNames?.Length == 2)
                        {
                            name.OriginalOverloadName = overloadNames[0];
                            name.OverloadName = overloadNames[1];
                        }
                        else
                        {
                            name.OriginalOverloadName = ovName;
                            name.OverloadName = ovName;
                        }
                    }
                    else
                    {
                        string overloadedName = name.Symbol.Name;
                        if (name.Symbol.ContainingNamespace != null)
                        {
                            var ns = name.Symbol.ContainingNamespace;
                            while (ns != null)
                            {
                                overloadedName = ns.Name + (ns.Name.Length > 0 && overloadedName.Length > 0 ? "." : "") + overloadedName;
                                ns = ns.ContainingNamespace;
                            }
                        }
                        overloadedName = GlobalName + "." + (!isImportedType ? assemblyNamespace : GetAssemblyGlobalSlug(nname.ContainingAssembly)) + (overloadedName.Length > 0 ? "." : "") + overloadedName;
                        var originalOverloadName = overloadedName;
                        if (BuildFlags.HasFlag(NetJsBuildFlags.MinifyNamespaces))
                        {
                            overloadedName = MinifyNamespace(overloadedName);
                        }
                        if (!usedNames.TryGetValue(nname.ContainingAssembly, out var usedName))
                        {
                            usedName = new HashSet<string>();
                            usedNames[nname.ContainingAssembly] = usedName;
                        }
                        if (!usedName.Add(overloadedName))
                        {
                            int iNameSlug = 1;
                            var tryName = overloadedName + "$" + iNameSlug;
                            while (!usedName.Add(tryName))
                            {
                                iNameSlug++;
                                tryName = overloadedName + "$" + iNameSlug;
                            }
                            overloadedName = tryName;
                        }
                        name.OriginalOverloadName = originalOverloadName;
                        name.OverloadName = overloadedName;
                        if (!isImportedType)
                        {
                            name.Export.OverloadName = overloadedName;
                            Symbols.Namespaces.Add(name.Signature, name.Export);
                        }
                        //minifiedName = SymbolMetadata.ShortName(this, name.Signature, minifiedName, Symbols.Namespaces);
                    }
                }
            });

            //Dictionary<string, string> usedNamespaceName = new();
            //foreach (var ns in AllSymbols.Values.Where(e => e.Symbol.Kind == SymbolKind.Namespace))
            //{
            //    var name = GlobalName + (string.IsNullOrEmpty(ns.FullName) ? "" : "." + ns.FullName);
            //    ns.OverloadName = SymbolMetadata.ShortName(this, null, null, ns.FullName, name, usedNamespaceName);
            //}

            void ProcessAssembly(SymbolMetadata assembly)
            {
                assembly.OverloadName = assembly.Signature;
                bool isImported = !SymbolEqualityComparer.Default.Equals(compilation.Assembly, assembly.Symbol);
                if (!isImported)
                {
                    assembly.Export.OverloadName = assembly.OverloadName;
                    Symbols.Assemblies.Add(assembly.Signature, assembly.Export);
                }
                //else
                //    Symbols.Assemblies.Add(assembly.Signature, assembly.Export);
            }

            $"Preprocessing assembly symbols".Profile(() =>
            {
                foreach (var assembly in AllSymbols!.Values.Where(e => e.Symbol.Kind == SymbolKind.Assembly))
                {
                    ProcessAssembly(assembly);
                }
            });
            Dictionary<string, HashSet<string>> usedTypeNames = new();
            //var reversedTypeNames = ImportedNames.Types.ToDictionary(e => e.Value.Signature, e => e);
            void ProcessType(SymbolMetadata type)
            {
                if (type.Preprocessed)
                    return;
                type.Preprocessed = true;
                var ttype = Unsafe.As<INamedTypeSymbol>(type.Symbol);
                //trying to detect compiler generated types
                if (ttype.IsImplicitlyDeclared)
                    return;
                if (ttype.IsAnonymousType)
                    return;
                //if (ttype.IsGenericType && ttype.Arity == 0)
                //    continue;
                if (ttype.ContainingType?.Name.Contains("<>") ?? false)
                    return;
                if (ttype.Name.Contains("<>"))
                    return;
                if (ttype.Name.StartsWith("<"))
                    return;
                if (ttype.Name.Contains("="))
                    return;
                bool isImportedType = !SymbolEqualityComparer.Default.Equals(ttype.ContainingAssembly, compilation.Assembly);
                if (isImportedType && ImportedSymbols.Types.TryGetValue(type.Signature, out var kv))
                {
                    var importedSymbolValue = kv;
                    //Debug.Assert(importedSymbolValue.Signature == type.Signature);
                    type.Export = importedSymbolValue;

                    var ovName = importedSymbolValue.OverloadName;
                    var overloadNames = ovName.Split('|');
                    if (overloadNames?.Length == 2)
                    {
                        type.OriginalOverloadName = overloadNames[0];
                        type.OverloadName = overloadNames[1];
                    }
                    else
                    {
                        type.OriginalOverloadName = ovName;
                        type.OverloadName = ovName;
                    }
                }
                else
                {
                    bool isExtern = IsExtern(type.Symbol);
                    string overloadedName = ttype.Name;
                    if (HasAttribute(type.Symbol, typeof(ExternalAttribute).FullName!, null, false, out _))
                    {
                        var name = type.Symbol.Name;
                        if (HasAttribute(type.Symbol, typeof(NameAttribute).FullName!, null, false, out var args))
                        {
                            name = (string)args![0]!;
                        }
                        type.OriginalOverloadName = name;
                        type.OverloadName = name;
                    }
                    else
                    {
                        var originalName = ttype.Name;
                        bool hasNameAttribute = HasAttribute(type.Symbol, typeof(NameAttribute).FullName!, null, false, out var args);
                        if (hasNameAttribute)
                        {
                            originalName = (string)args![0]!;
                        }
                        if (ttype.Arity > 0)
                        {
                            var genericNames = "$" + string.Join("$", Enumerable.Range(1, ttype.Arity).Select(s => "")) + "$";
                            originalName += genericNames;
                        }
                        string originalOverloadName;
                        if (ttype.ContainingType != null)
                        {
                            var typeMeta = GetRequiredMetadata(ttype.ContainingType);
                            ProcessType(typeMeta);
                            Debug.Assert(typeMeta.OriginalOverloadName != null && typeMeta.OverloadName != null);
                            if (ttype.IsExtension)
                            {
                                originalOverloadName = typeMeta.OriginalOverloadName + ".$";
                                overloadedName = typeMeta.OverloadName + ".$";
                            }
                            else
                            {
                                originalOverloadName = typeMeta.OriginalOverloadName + "." + originalName;
                                overloadedName = typeMeta.OverloadName + "." + originalName;
                            }
                        }
                        else if (ttype.ContainingNamespace != null)
                        {
                            var nameMeta = GetRequiredMetadata(ttype.ContainingNamespace);
                            Debug.Assert(nameMeta.OriginalOverloadName != null && nameMeta.OverloadName != null);
                            originalOverloadName = nameMeta.OriginalOverloadName + "." + originalName;
                            overloadedName = nameMeta.OverloadName + "." + originalName;
                        }
                        else
                        {
                            var nameMeta = GetRequiredMetadata(ttype.ContainingAssembly.GlobalNamespace);
                            Debug.Assert(nameMeta.OriginalOverloadName != null && nameMeta.OverloadName != null);
                            originalOverloadName = nameMeta.OriginalOverloadName + "." + originalName;
                            overloadedName = nameMeta.OverloadName + "." + originalName;
                        }
                        if (!hasNameAttribute && BuildFlags.HasFlag(NetJsBuildFlags.MinifyTypeNames))
                        {
                            overloadedName = MinifyMemberLastNameSegment(ttype, overloadedName);
                            type.OriginalOverloadName = originalOverloadName;
                            type.OverloadName = overloadedName;
                        }
                        else
                        {
                            //var prefix = GlobalName + "." + (!isImportedType ? assemblyNamespace : GetAssemblyGlobalSlug(ttype.ContainingAssembly)) + ".";
                            //var fn = type.UniqueFullName.Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                            //overloadedName = (fn.StartsWith(prefix) ? null : prefix) + fn;
                            if (!hasNameAttribute && !isExtern)
                            {
                                if (!usedTypeNames.TryGetValue(overloadedName, out var usedName))
                                {
                                    usedName = new HashSet<string>();
                                    usedTypeNames[overloadedName] = usedName;
                                }
                                if (!usedName.Add(overloadedName))
                                {
                                    int iNameSlug = 1;
                                    var tryName = overloadedName + "$" + iNameSlug;
                                    while (!usedName.Add(tryName))
                                    {
                                        iNameSlug++;
                                        tryName = overloadedName + "$" + iNameSlug;
                                    }
                                    overloadedName = tryName;
                                }
                            }
                            type.OriginalOverloadName = originalOverloadName;
                            type.OverloadName = overloadedName;
                        }
                        if (/*!isExtern &&*/ !isImportedType && !ttype.IsExtension/*we can have multiple extension type with same signature, don keu them*/)
                        {
                            type.Export.OverloadName = overloadedName;
                            Symbols.Types.Add(type.Signature, type.Export);
                        }
                        //overloadedName = SymbolMetadata.ShortName(this, /*shortPrefixOverloadName, originalPrefixOverloadName,*/ type.Signature, overloadedName, !isExtern ? Symbols.Types : new Dictionary<string, SymbolValue>(), generate: !isExtern, export: !hasNameAttribute && !isExtern && !isImportedType);
                    }
                }
            }

            $"Preprocessing type symbols".Profile(() =>
            {
                foreach (var type in AllSymbols!.Values.Where(e => e.Symbol.Kind == SymbolKind.NamedType && e.Symbol.ContainingType == null)
                .OrderBy(e => Hierarchy((INamedTypeSymbol)e.Symbol))
                .ThenBy(e => e.Signature) //order determinititically to ensure we keep getting the same minified name result across various run
                )
                {
                    ProcessType(type);
                }
            });

            void ProcessField(SymbolMetadata field)
            {
                if (field.Preprocessed)
                    return;
                field.Preprocessed = true;
                var ffield = Unsafe.As<IFieldSymbol>(field.Symbol);
                //skip compiler generated backing fields. We dont need it
                if (ffield.IsImplicitlyDeclared)
                    return;
                if (ffield.Name.Contains("<>"))
                    return;
                if (ffield.Name.StartsWith("<"))
                    return;
                if (ffield.Name.Contains("="))
                    return;
                if (ffield.ContainingType.IsImplicitlyDeclared)
                    return;
                if (ffield.ContainingType.Name.Contains("<>"))
                    return;
                if (ffield.ContainingType.Name.StartsWith("<"))
                    return;
                if (ffield.ContainingType.Name.Contains("="))
                    return;
                bool isImportedType = !SymbolEqualityComparer.Default.Equals(ffield.ContainingAssembly, compilation.Assembly);
                var declaringType = field.Symbol.ContainingType;
                var declaringTypeMetadata = SymbolMetadatas![declaringType.OriginalDefinition];
                if (isImportedType && ImportedSymbols.Members.TryGetValue(declaringTypeMetadata.Signature, out var m) && m.TryGetValue(field.Signature, out var kv))
                {
                    var importedSymbolValue = kv;
                    //Debug.Assert(importedSymbolValue.Signature == field.Signature);
                    field.Export = importedSymbolValue;

                    var ovName = importedSymbolValue.OverloadName;
                    var overloadNames = ovName.Split('|');
                    if (overloadNames.Length == 2)
                    {
                        field.OriginalOverloadName = overloadNames[0];
                        field.OverloadName = overloadNames[1];
                        MarkMemberNameUsed(ffield.ContainingType, ffield.Name, overloadNames[0]);
                    }
                    else
                    {
                        field.OriginalOverloadName = overloadNames[0];
                        field.OverloadName = overloadNames[0];
                        MarkMemberNameUsed(ffield.ContainingType, ffield.Name, overloadNames[0]);
                    }
                }
                else
                {
                    string overloadedName = ffield.Name;
                    bool isExtern = IsExtern(field.Symbol);
                    bool hasNameAttribute = HasAttribute(field.Symbol, typeof(NameAttribute).FullName!, null, false, out var args);
                    bool export = /*!isExtern &&*/ !isImportedType;
                    Dictionary<string, SymbolExport> memberNames;
                    if (!export)
                    {
                        memberNames = new Dictionary<string, SymbolExport>();
                    }
                    else if (!Symbols.Members.TryGetValue(declaringTypeMetadata.Signature, out memberNames))
                    {
                        memberNames = new Dictionary<string, SymbolExport>();
                        Symbols.Members.Add(declaringTypeMetadata.Signature, memberNames);
                    }
                    if (hasNameAttribute)
                    {
                        overloadedName = (string)args![0]!;
                        field.OriginalOverloadName = overloadedName;
                        field.OverloadName = overloadedName;
                        MarkMemberNameUsed(ffield.ContainingType, ffield.Name, overloadedName);
                    }
                    else
                    {
                        var name = ffield.Name.ResolveIdentifierName().RemoveGenericParameterNames(out _);
                        if (!name.Contains(".")) //doesnt apply to explicit interface implementations
                        {
                            var convention = field.Symbol.GetConvention(this);
                            if (convention?.Member == ConventionMember.All || (convention?.Member.HasFlag(ConventionMember.Property) ?? false))
                            {
                                name = ToNotation(name, convention.Notation);
                            }
                        }
                        overloadedName = name;
                        if (!isExtern)
                        {
                            if (declaringType.TypeKind == TypeKind.Interface)
                            {
                                //if (!overloadedName.Contains("."))
                                //{
                                overloadedName = declaringTypeMetadata.OriginalFullName + "." + overloadedName;
                                var assemblySlug = GetAssemblyGlobalSlug(declaringType.ContainingAssembly);
                                if (overloadedName.StartsWith(GlobalName + "." + assemblySlug + "."))
                                {
                                    overloadedName = overloadedName.Substring(GlobalName.Length + 1 + assemblySlug.Length + 1);
                                }
                                //}
                            }
                        }
                        overloadedName = overloadedName.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                        string originalOverloadName = overloadedName;
                        if (BuildFlags.HasFlag(NetJsBuildFlags.MinifyFieldNames) &&
                                !HasAnyAttribute(ffield, null, false, "System.Runtime.InteropServices.JavaScript.JSExportAttribute") &&
                                !HasAnyAttribute(ffield.ContainingType, null, false, typeof(KeepMemberNamesAttribute).FullName!))
                        {
                            if (!hasNameAttribute && !isExtern)
                                overloadedName = MinifyMemberLastNameSegment(ffield, overloadedName);
                            field.OriginalOverloadName = originalOverloadName;
                            field.OverloadName = overloadedName;
                        }
                        else
                        {
                            if (!hasNameAttribute && !isExtern)
                            {
                                int iNameSlug = 1;
                                string tryName = overloadedName;
                                HashSet<string>? usedNames = null;
                                while (IsMemberNameUsed(ffield.ContainingType, overloadedName, tryName, out usedNames))
                                {
                                    tryName = $"{overloadedName}${iNameSlug}";
                                    iNameSlug++;
                                }
                                overloadedName = tryName;
                                usedNames?.Add(tryName);
                            }
                            field.OriginalOverloadName = originalOverloadName;
                            field.OverloadName = overloadedName;
                        }
                    }
                    if (export)
                    {
                        field.Export.OverloadName = overloadedName;
                        memberNames.Add(field.Signature, field.Export);
                    }
                }
            }

            void ProcessEvent(SymbolMetadata @event)
            {
                if (@event.Preprocessed)
                    return;
                @event.Preprocessed = true;
                var eevent = Unsafe.As<IEventSymbol>(@event.Symbol);
                //skip compiler generated backing fields. We dont need it
                if (eevent.IsImplicitlyDeclared)
                    return;
                if (eevent.Name.Contains("<>"))
                    return;
                if (eevent.Name.StartsWith("<"))
                    return;
                if (eevent.Name.Contains("="))
                    return;
                if (eevent.ContainingType.IsImplicitlyDeclared)
                    return;
                if (eevent.ContainingType.Name.Contains("<>"))
                    return;
                if (eevent.ContainingType.Name.StartsWith("<"))
                    return;
                if (eevent.ContainingType.Name.Contains("="))
                    return;
                bool isImportedType = !SymbolEqualityComparer.Default.Equals(eevent.ContainingAssembly, compilation.Assembly);
                var declaringType = @event.Symbol.ContainingType;
                var declaringTypeMetadata = SymbolMetadatas![declaringType.OriginalDefinition];
                if (isImportedType && ImportedSymbols.Members.TryGetValue(declaringTypeMetadata.Signature, out var m) && m.TryGetValue(@event.Signature, out var kv))
                {
                    var importedSymbolValue = kv;
                    //Debug.Assert(importedSymbolValue.Signature == @event.Signature);
                    @event.Export = importedSymbolValue;

                    var ovName = importedSymbolValue.OverloadName;
                    var overloadNames = ovName.Split('|');
                    if (overloadNames.Length == 2)
                    {
                        @event.OriginalOverloadName = overloadNames[0];
                        @event.OverloadName = overloadNames[1];
                        MarkMemberNameUsed(eevent.ContainingType, eevent.Name, overloadNames[0]);
                    }
                    else
                    {
                        @event.OriginalOverloadName = overloadNames[0];
                        @event.OverloadName = overloadNames[0];
                        MarkMemberNameUsed(eevent.ContainingType, eevent.Name, overloadNames[0]);
                    }
                }
                else
                {
                    string overloadedName = eevent.Name;
                    bool isExplicitImplementation = false;
                    bool isExtern = IsExtern(@event.Symbol);
                    bool hasNameAttribute = HasAttribute(@event.Symbol, typeof(NameAttribute).FullName!, null, false, out var args);
                    bool export = /*!isExtern &&*/ !isImportedType;
                    Dictionary<string, SymbolExport> memberNames;
                    if (!export)
                    {
                        memberNames = new Dictionary<string, SymbolExport>();
                    }
                    else if (!Symbols.Members.TryGetValue(declaringTypeMetadata.Signature, out memberNames))
                    {
                        memberNames = new Dictionary<string, SymbolExport>();
                        Symbols.Members.Add(declaringTypeMetadata.Signature, memberNames);
                    }
                    if (hasNameAttribute)
                    {
                        overloadedName = (string)args![0]!;
                        @event.OriginalOverloadName = overloadedName;
                        @event.OverloadName = overloadedName;
                        MarkMemberNameUsed(@eevent.ContainingType, @eevent.Name, overloadedName);
                    }
                    else
                    {
                        if (eevent.IsOverride) //an event that overrides a base event must use exactly the same overload name as its base
                        {
                            var baseEvent = eevent.OverriddenEvent!.OriginalDefinition;
                            while (baseEvent.IsOverride)
                            {
                                baseEvent = baseEvent.OverriddenEvent!;
                            }

                            var overriddenMetadata = GetRequiredMetadata(baseEvent);
                            ProcessEvent(overriddenMetadata);
                            @event.OriginalOverloadName = overriddenMetadata.GivenOriginalOverloadName;
                            overloadedName = @event.OverloadName = overriddenMetadata.GivenOverloadName!;
                            MarkMemberNameUsed(@eevent.ContainingType, @eevent.Name, overloadedName);
                        }
                        else if (eevent.ExplicitInterfaceImplementations.Length > 0/* && eevent.ContainingType.TypeKind != TypeKind.Interface*/)
                        {
                            isExplicitImplementation = true;
                            var originalInterfaceEvent = eevent.ExplicitInterfaceImplementations.First();
                            var declaringInterfaceMetadata = SymbolMetadatas[originalInterfaceEvent.OriginalDefinition];
                            ProcessEvent(declaringInterfaceMetadata);
                            @event.OriginalOverloadName = declaringInterfaceMetadata.GivenOriginalOverloadName;
                            overloadedName = @event.OverloadName = declaringInterfaceMetadata.GivenOverloadName!;
                            MarkMemberNameUsed(@eevent.ContainingType, @eevent.Name, overloadedName);
                        }
                        else
                        {
                            var name = eevent.Name.ResolveIdentifierName().RemoveGenericParameterNames(out _);
                            if (!name.Contains(".")) //doesnt apply to explicit interface implementations
                            {
                                var convention = @event.Symbol.GetConvention(this);
                                if (convention?.Member == ConventionMember.All || (convention?.Member.HasFlag(ConventionMember.Property) ?? false))
                                {
                                    name = ToNotation(name, convention.Notation);
                                }
                            }
                            overloadedName = name;
                            if (!isExtern)
                            {
                                if (declaringType.TypeKind == TypeKind.Interface)
                                {
                                    overloadedName = declaringTypeMetadata.OriginalFullName + "." + overloadedName;
                                    var assemblySlug = GetAssemblyGlobalSlug(declaringType.ContainingAssembly);
                                    if (overloadedName.StartsWith(GlobalName + "." + assemblySlug + "."))
                                    {
                                        overloadedName = overloadedName.Substring(GlobalName.Length + 1 + assemblySlug.Length + 1);
                                    }
                                }
                            }
                            overloadedName = overloadedName.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                            string originalOverloadName = overloadedName;
                            if (BuildFlags.HasFlag(NetJsBuildFlags.MinifyEventNames) &&
                                !HasAnyAttribute(eevent, null, false, "System.Runtime.InteropServices.JavaScript.JSExportAttribute") &&
                                !HasAnyAttribute(eevent.ContainingType, null, false, typeof(KeepMemberNamesAttribute).FullName!))
                            {
                                if (!hasNameAttribute && !isExtern)
                                    overloadedName = MinifyMemberLastNameSegment(eevent, overloadedName);
                                @event.OriginalOverloadName = originalOverloadName;
                                @event.OverloadName = overloadedName;
                            }
                            else
                            {
                                if (!hasNameAttribute && !isExtern)
                                {
                                    int iNameSlug = 1;
                                    string tryName = overloadedName;
                                    HashSet<string>? usedNames = null;
                                    while (IsMemberNameUsed(eevent.ContainingType, overloadedName, tryName, out usedNames))
                                    {
                                        tryName = $"{overloadedName}${iNameSlug}";
                                        iNameSlug++;
                                    }
                                    overloadedName = tryName;
                                    usedNames?.Add(tryName);
                                }
                                @event.OriginalOverloadName = overloadedName;
                                @event.OverloadName = overloadedName;
                            }
                        }
                    }
                    if (export)
                    {
                        if (isExplicitImplementation && memberNames.TryGetValue(overloadedName, out var existingExport))
                        {
                            //TODO: A class implement eg IEnumerable<int> and IEnumerable<string> member explicitly
                            //They both have the same overload name
                        }
                        else
                        {
                            @event.Export.OverloadName = overloadedName;
                            memberNames.Add(@event.Signature, @event.Export);
                        }
                    }
                }
            }

            void ProcessProperty(SymbolMetadata property)
            {
                if (property.Preprocessed)
                    return;
                property.Preprocessed = true;
                var pproperty = Unsafe.As<IPropertySymbol>(property.Symbol);
                bool isImportedType = !SymbolEqualityComparer.Default.Equals(pproperty.ContainingAssembly, compilation.Assembly);
                var declaringType = property.Symbol.ContainingType;
                var declaringTypeMetadata = SymbolMetadatas![declaringType.OriginalDefinition];
                if (isImportedType && ImportedSymbols.Members.TryGetValue(declaringTypeMetadata.Signature, out var m) && m.TryGetValue(property.Signature, out var kv))
                {
                    var importedSymbolValue = kv;
                    //Debug.Assert(importedSymbolValue.Signature == property.Signature);
                    property.Export = importedSymbolValue;

                    var ovName = importedSymbolValue.OverloadName;
                    var overloadNames = ovName.Split('|');
                    if (overloadNames.Length == 2)
                    {
                        property.OriginalOverloadName = overloadNames[0];
                        property.OverloadName = overloadNames[1];
                        MarkMemberNameUsed(pproperty.ContainingType, pproperty.Name, overloadNames[0]);
                    }
                    else
                    {
                        property.OriginalOverloadName = overloadNames[0];
                        property.OverloadName = overloadNames[0];
                        MarkMemberNameUsed(pproperty.ContainingType, pproperty.Name, overloadNames[0]);
                    }
                }
                else
                {
                    bool isExtern = IsExtern(property.Symbol);
                    bool hasNameAttribute = HasAttribute(property.Symbol, typeof(NameAttribute).FullName!, null, false, out var args);
                    bool export = /*!isExtern && */!isImportedType;
                    string overloadedName = pproperty.Name;
                    bool isExplicitImplementation = false;
                    Dictionary<string, SymbolExport> memberNames;
                    if (!export)
                    {
                        memberNames = new Dictionary<string, SymbolExport>();
                    }
                    else if (!Symbols.Members.TryGetValue(declaringTypeMetadata.Signature, out memberNames))
                    {
                        memberNames = new Dictionary<string, SymbolExport>();
                        Symbols.Members.Add(declaringTypeMetadata.Signature, memberNames);
                    }
                    if (hasNameAttribute)
                    {
                        overloadedName = (string)args![0]!;
                        property.OriginalOverloadName = overloadedName;
                        property.OverloadName = overloadedName;
                        MarkMemberNameUsed(pproperty.ContainingType, pproperty.Name, overloadedName);
                    }
                    else
                    {
                        if (pproperty.IsOverride) //a property that overrides a base property must use exactly the same overload name as its base
                        {
                            var baseProperty = pproperty.OverriddenProperty!.OriginalDefinition;

                            while (baseProperty.IsOverride)
                            {
                                baseProperty = baseProperty.OverriddenProperty!;
                            }

                            var overriddenMetadata = GetRequiredMetadata(baseProperty);
                            ProcessProperty(overriddenMetadata);
                            property.OriginalOverloadName = overriddenMetadata.GivenOriginalOverloadName;
                            overloadedName = property.OverloadName = overriddenMetadata.GivenOverloadName!;
                            MarkMemberNameUsed(pproperty.ContainingType, pproperty.Name, overloadedName);
                        }
                        else if (pproperty.ExplicitInterfaceImplementations.Length > 0/* && pproperty.ContainingType.TypeKind != TypeKind.Interface*/)
                        {
                            isExplicitImplementation = true;
                            var originalInterfaceProperty = pproperty.ExplicitInterfaceImplementations.First();
                            var declaringInterfaceMetadata = SymbolMetadatas[originalInterfaceProperty.OriginalDefinition];
                            ProcessProperty(declaringInterfaceMetadata);
                            property.OriginalOverloadName = declaringInterfaceMetadata.GivenOriginalOverloadName;
                            overloadedName = property.OverloadName = declaringInterfaceMetadata.GivenOverloadName!;
                            MarkMemberNameUsed(pproperty.ContainingType, pproperty.Name, overloadedName);
                        }
                        else
                        {
                            string name = pproperty.Name.RemoveGenericParameterNames(out _);
                            if (name == "this[]")
                            {
                                name = "Item";
                            }
                            if (!name.Contains(".")) //doesnt apply to explicit interface implementations
                            {
                                var convention = property.Symbol.GetConvention(this);
                                if (convention?.Member == ConventionMember.All || (convention?.Member.HasFlag(ConventionMember.Property) ?? false))
                                {
                                    name = ToNotation(name, convention.Notation);
                                }
                            }
                            overloadedName = name;
                            if (!isExtern)
                            {
                                if (declaringType.TypeKind == TypeKind.Interface)
                                {
                                    overloadedName = declaringTypeMetadata.OverloadName + "." + overloadedName;
                                    var assemblySlug = GetAssemblyGlobalSlug(declaringType.ContainingAssembly);
                                    if (overloadedName.StartsWith(GlobalName + "." + assemblySlug + "."))
                                    {
                                        overloadedName = overloadedName.Substring(GlobalName.Length + 1 + assemblySlug.Length + 1);
                                    }
                                }
                            }
                            overloadedName = overloadedName.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                            string originalOverloadName = overloadedName;
                            if (BuildFlags.HasFlag(NetJsBuildFlags.MinifyPropertyNames) &&
                                !HasAnyAttribute(pproperty, null, false, "System.Runtime.InteropServices.JavaScript.JSExportAttribute") &&
                                !HasAnyAttribute(pproperty.ContainingType, null, false, typeof(KeepMemberNamesAttribute).FullName!))
                            {
                                if (!hasNameAttribute && !isExtern)
                                    overloadedName = MinifyMemberLastNameSegment(pproperty, overloadedName);
                                property.OriginalOverloadName = originalOverloadName;
                                property.OverloadName = overloadedName;
                            }
                            else
                            {
                                if (!hasNameAttribute && !isExtern)
                                {
                                    int iNameSlug = 1;
                                    string tryName = overloadedName;
                                    HashSet<string>? usedNames = null;
                                    while (IsMemberNameUsed(pproperty.ContainingType, overloadedName, tryName, out usedNames))
                                    {
                                        tryName = $"{overloadedName}${iNameSlug}";
                                        iNameSlug++;
                                    }
                                    overloadedName = tryName;
                                    usedNames?.Add(tryName);
                                }
                                property.OriginalOverloadName = overloadedName;
                                property.OverloadName = overloadedName;
                            }
                        }
                    }
                    if (export)
                    {
                        if (isExplicitImplementation && memberNames.TryGetValue(overloadedName, out var existingExport))
                        {
                            //TODO: A class implement eg IEnumerable<int> and IEnumerable<string> member explicitly
                            //They both have the same overload name
                        }
                        else
                        {
                            property.Export.OverloadName = overloadedName;
                            memberNames.Add(property.Signature, property.Export);
                        }
                    }
                }
            }

            IEnumerable<string> GetConstructorNames(INamedTypeSymbol ts)
            {
                var ctors = ts.GetMembers(".ctor");
                foreach (var ctor in ctors)
                {
                    var meta = GetRequiredMetadata(ctor);
                    yield return meta.OverloadName!;
                }
                if (ts.BaseType != null)
                {
                    foreach (var name in GetConstructorNames(ts.BaseType))
                    {
                        yield return name;
                    }
                }
            }

            void ProcessMethod(SymbolMetadata method)
            {
                if (method.Preprocessed)
                    return;
                method.Preprocessed = true;
                var mmethod = Unsafe.As<IMethodSymbol>(method.Symbol);
                bool isImportedType = !SymbolEqualityComparer.Default.Equals(mmethod.ContainingAssembly, compilation.Assembly);
                var declaringType = method.Symbol.ContainingType;
                var declaringTypeMetadata = SymbolMetadatas![declaringType.OriginalDefinition];

                if (isImportedType && ImportedSymbols.Members.TryGetValue(declaringTypeMetadata.Signature, out var m) && m.TryGetValue(method.Signature, out var kv))
                {
                    var importedSymbolValue = kv;
                    //Debug.Assert(importedSymbolValue.Signature == method.Signature);
                    method.Export = importedSymbolValue;

                    var ovName = importedSymbolValue.OverloadName;
                    var overloadNames = ovName.Split('|');
                    if (overloadNames.Length == 2)
                    {
                        method.OriginalOverloadName = overloadNames[0];
                        method.OverloadName = overloadNames[1];
                        MarkMemberNameUsed(mmethod.ContainingType, mmethod.Name, overloadNames[1]);
                    }
                    else
                    {
                        method.OriginalOverloadName = overloadNames[0];
                        method.OverloadName = overloadNames[0];
                        MarkMemberNameUsed(mmethod.ContainingType, mmethod.Name, overloadNames[0]);
                    }
                }
                else
                {
                    bool isExtern = IsExtern(method.Symbol);
                    bool hasNameAttribute = HasAttribute(method.Symbol, typeof(NameAttribute).FullName!, null, false, out var args);
                    bool export = /*!isExtern && */!isImportedType && mmethod.MethodKind != MethodKind.LocalFunction;
                    Dictionary<string, SymbolExport> memberNames;
                    bool isExplicitImplementation = false;
                    if (!export)
                    {
                        memberNames = new Dictionary<string, SymbolExport>();
                    }
                    else if (!Symbols.Members.TryGetValue(declaringTypeMetadata.Signature, out memberNames))
                    {
                        memberNames = new Dictionary<string, SymbolExport>();
                        Symbols.Members.Add(declaringTypeMetadata.Signature, memberNames);
                    }
                    string overloadedName = mmethod.Name.RemoveGenericParameterNames(out _);
                    //if (mmethod.MethodKind == MethodKind.Constructor && mmethod.Parameters.Length == 0) //default constructor name must be predictable, this allow us to do new T()
                    //{
                    //    overloadedName = Constants.DefaultConstructorName;
                    //    method.OriginalOverloadName = Constants.DefaultConstructorName;
                    //    method.OverloadName = Constants.DefaultConstructorName;
                    //}
                    //else
                    {
                        if (mmethod.Name == ".ctor")
                        {
                            if (mmethod.ContainingType.BaseType != null)
                            {
                                MarkMemberNameUsed(mmethod.ContainingType, ".ctor", Constants.DefaultConstructorName);
                                if (!usedMemberNames.TryGetValue(mmethod.ContainingType, out var usedMethodNames))
                                {
                                    usedMethodNames = new();
                                    usedMemberNames[mmethod.ContainingType] = usedMethodNames;
                                }
                                if (!usedMethodNames.TryGetValue(Constants.DefaultConstructorName, out var usedName))
                                {
                                    usedName = new HashSet<string>();
                                    usedMethodNames[Constants.DefaultConstructorName] = usedName;
                                }
                                //All constructor of a class hierachy must have a unique name
                                //Constructors are not overloadable/overidable anyway.
                                //Load all lower class contructor names from lower hierachy class into this hashset to make sure we dont resuse the same name
                                var ctorNames = GetConstructorNames(mmethod.ContainingType.BaseType);
                                foreach (var name in ctorNames)
                                    usedName.Add(name);
                            }
                        }
                        if (hasNameAttribute)
                        {
                            overloadedName = (string)args![0]!;
                            method.OriginalOverloadName = overloadedName;
                            method.OverloadName = overloadedName;
                            MarkMemberNameUsed(mmethod.ContainingType, mmethod.Name, overloadedName);
                        }
                        else
                        {
                            if (mmethod.IsOverride) //a method that overrides a base method must use exactly the same overload name as its base
                            {
                                var baseMethod = mmethod.OverriddenMethod!.OriginalDefinition;
                                while (baseMethod.IsOverride)
                                {
                                    baseMethod = baseMethod.OverriddenMethod!;
                                }

                                var overriddenMetadata = GetRequiredMetadata(baseMethod);
                                ProcessMethod(overriddenMetadata);
                                method.OriginalOverloadName = overriddenMetadata.GivenOriginalOverloadName;
                                overloadedName = method.OverloadName = overriddenMetadata.GivenOverloadName!;
                                MarkMemberNameUsed(mmethod.ContainingType, mmethod.Name, overloadedName);
                            }
                            else if (mmethod.ExplicitInterfaceImplementations.Length > 0
                                //&&
                                //(!mmethod.ExplicitInterfaceImplementations[0].UseIndirectInterfaceMember(out var needRuntimeNameLookup) || SymbolEqualityComparer.Default.Equals(mmethod.ExplicitInterfaceImplementations[0].ContainingType, mmethod.ExplicitInterfaceImplementations[0].ContainingType.OriginalDefinition)) //implements a non generic interface
                                )
                            {
                                isExplicitImplementation = true;
                                var implementedInterfaceMethod = mmethod.ExplicitInterfaceImplementations[0];
                                var declaringInterfaceMetadata = SymbolMetadatas[implementedInterfaceMethod.OriginalDefinition];
                                ProcessMethod(declaringInterfaceMetadata);
                                method.OriginalOverloadName = declaringInterfaceMetadata.GivenOriginalOverloadName;
                                overloadedName = method.OverloadName = declaringInterfaceMetadata.GivenOverloadName!;
                                MarkMemberNameUsed(mmethod.ContainingType, mmethod.Name, overloadedName);
                            }
                            else
                            {
                                //if we have added an overload to Item accessor name eg Item$1, we want to make sure the same reflects in the methods get_Item$1
                                if (mmethod.AssociatedSymbol != null && mmethod.AssociatedSymbol is IPropertySymbol p && p.IsIndexer)
                                {
                                    var metadata = GetRequiredMetadata(mmethod.AssociatedSymbol);
                                    overloadedName = overloadedName.Replace("_Item", "_" + metadata.OverloadName);
                                }
                                overloadedName = mmethod.Name.RemoveGenericParameterNames(out _);
                                if (!overloadedName.Contains(".")) //doesnt apply to explicit interface implementations
                                {
                                    var convention = method.Symbol.GetConvention(this);
                                    if (convention?.Member == ConventionMember.All || (convention?.Member.HasFlag(ConventionMember.Method) ?? false))
                                    {
                                        overloadedName = ToNotation(overloadedName, convention.Notation);
                                    }
                                }
                                if (!isExtern)
                                {
                                    //if (mmethod.ExplicitInterfaceImplementations.Length > 0)
                                    //{
                                    //    var explicitDeclaringMetadata = GetRequiredMetadata(mmethod.ExplicitInterfaceImplementations[0].ContainingType);
                                    //    overloadedName = explicitDeclaringMetadata.OverloadName + "." + mmethod.ExplicitInterfaceImplementations[0].Name;
                                    //    var assemblySlug = GetAssemblyGlobalSlug(mmethod.ExplicitInterfaceImplementations[0].ContainingAssembly);
                                    //    if (overloadedName.StartsWith(GlobalName + "." + assemblySlug + "."))
                                    //    {
                                    //        overloadedName = overloadedName.Substring(GlobalName.Length + 1 + assemblySlug.Length + 1);
                                    //    }
                                    //}
                                    //else 
                                    if (declaringType.TypeKind == TypeKind.Interface)
                                    {
                                        overloadedName = declaringTypeMetadata.OverloadName + "." + overloadedName;
                                        var assemblySlug = GetAssemblyGlobalSlug(declaringType.ContainingAssembly);
                                        if (overloadedName.StartsWith(GlobalName + "." + assemblySlug + "."))
                                        {
                                            overloadedName = overloadedName.Substring(GlobalName.Length + 1 + assemblySlug.Length + 1);
                                        }
                                    }
                                }
                                overloadedName = overloadedName.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                                string originalOverloadName = overloadedName;
                                bool hasJsExport = HasAnyAttribute(mmethod, null, false, "System.Runtime.InteropServices.JavaScript.JSExportAttribute");
                                if (BuildFlags.HasFlag(NetJsBuildFlags.MinifyMethodNames) &&
                                    !hasJsExport &&
                                    !HasAnyAttribute(mmethod.ContainingType, null, false, typeof(KeepMemberNamesAttribute).FullName!))
                                {
                                    if (!hasNameAttribute && !isExtern && !hasJsExport)
                                        overloadedName = MinifyMemberLastNameSegment(mmethod, overloadedName);
                                    method.OriginalOverloadName = originalOverloadName;
                                    method.OverloadName = overloadedName;
                                }
                                else
                                {
                                    if (!hasNameAttribute && !isExtern && !hasJsExport)
                                    {
                                        int iNameSlug = 1;
                                        string tryName = overloadedName;
                                        HashSet<string>? usedNames = null;
                                        while (IsMemberNameUsed(mmethod.ContainingType, overloadedName, tryName, out usedNames))
                                        {
                                            tryName = $"{overloadedName}${iNameSlug}";
                                            iNameSlug++;
                                        }
                                        overloadedName = tryName;
                                        usedNames?.Add(tryName);
                                    }
                                    method.OriginalOverloadName = overloadedName;
                                    method.OverloadName = overloadedName;
                                }
                            }
                        }
                    }
                    if (export)
                    {
                        if (isExplicitImplementation && memberNames.TryGetValue(overloadedName, out var existingExport))
                        {
                            //TODO: A class implement eg IEnumerable<int> and IEnumerable<string> member explicitly
                            //They both have the same overload name
                        }
                        else
                        {
                            method.Export.OverloadName = overloadedName;
                            memberNames.Add(method.Signature, method.Export);
                        }
                    }
                }
            }

            $"Preprocessing member symbols".Profile(() =>
            {
                var members = AllSymbols!.Values
                    .Where(e => e.Symbol.Kind == SymbolKind.Field || e.Symbol.Kind == SymbolKind.Event || e.Symbol.Kind == SymbolKind.Property || e.Symbol.Kind == SymbolKind.Method || (e.Symbol.Kind == SymbolKind.NamedType && e.Symbol.ContainingType != null))
                    .OrderBy(m => Hierarchy(m.Symbol.ContainingType)) //make sure we process base/virtual property before overrides once, since we want to use the base overload name for all
                    .ThenBy(m => m.Signature); //order determinititically to ensure we keep getting the same minified name result across various run
                members
                //.AsParallel()
                .ForAll(member =>
                {
                    //if (member.Symbol.ToString() == "decimal.operator +(decimal, decimal)")
                    //{

                    //}
                    if (member.Symbol.Kind == SymbolKind.Field)
                        ProcessField(member);
                    else if (member.Symbol.Kind == SymbolKind.Event)
                        ProcessEvent(member);
                    else if (member.Symbol.Kind == SymbolKind.Property)
                        ProcessProperty(member);
                    else if (member.Symbol.Kind == SymbolKind.Method)
                        ProcessMethod(member);
                    else if (member.Symbol.Kind == SymbolKind.NamedType)
                        ProcessType(member);
                });
                //foreach (var member in members)
                //{
                //    if (member.Symbol.Kind == SymbolKind.Field)
                //        ProcessField(member);
                //    else if (member.Symbol.Kind == SymbolKind.Event)
                //        ProcessEvent(member);
                //    else if (member.Symbol.Kind == SymbolKind.Property)
                //        ProcessProperty(member);
                //    else if (member.Symbol.Kind == SymbolKind.Method)
                //        ProcessMethod(member);
                //    else if (member.Symbol.Kind == SymbolKind.NamedType)
                //        ProcessType(member);
                //}
            });


            SyntaxSymbols = AllSymbols.Where(e => e.Value.DeclaringReferences.Any())
                .SelectMany(e => e.Value.DeclaringReferences.Select(d => (e, d.GetSyntax())))
                .GroupBy(e => e.Item2)
                .ToDictionary(e => e.Key, e => (IEnumerable<SymbolMetadata>)e.Select(ee => ee.e.Value).OrderBy(e => e.Symbol.Kind == SymbolKind.NamedType ? 1 : 2).ToList());

            $"Collecting extension methods".Profile(() =>
            {
                ExtensionMethods = AllSymbols.Where(e => e.Value.Symbol is IMethodSymbol m && m.IsStatic && m.IsExtensionMethod)
                    .Select(e => (IMethodSymbol)e.Value.Symbol)
                    .GroupBy(e => e.Name)
                    .ToDictionary(e => e.Key, e => (IEnumerable<IMethodSymbol>)e.ToList());
            });

            //$"Collecting delegates".Profile(() =>
            //{
            //    Delegates = AllSymbols.Where(e => e.Value.Symbol is ITypeSymbol t && t.TypeKind == TypeKind.Delegate)
            //        .Select(e => (ITypeSymbol)e.Value.Symbol)
            //        .GroupBy(e => e.Name)
            //        .ToDictionary(e => e.Key, e => (IEnumerable<ITypeSymbol>)e.ToList());
            //});


            $"Collecting linker symbols".Profile(() =>
            {
                var linkerFiles = project.GetLinkerFiles();
                string? GetValue(XElement node, string key)
                {
                    return node.Attribute(key.ToLower())?.Value;
                }
                foreach (var file in linkerFiles)
                {
                    var manifest = new AssemblyManifestModel();
                    manifest.Name = Path.GetFileNameWithoutExtension(file);
                    var xml = File.ReadAllText(file);
                    var doc = XElement.Parse(xml);
                    var assemblies = doc.Elements("assembly")
                        .Select(node =>
                        {
                            return new ILLinkerAssembly
                            {
                                FullName = GetValue(node, nameof(ILLinkerAssembly.FullName))?.Replace("/", ".") ?? throw new InvalidOperationException("FullName is required"),
                                Feature = GetValue(node, nameof(ILLinkerAssembly.Feature)),
                                FeatureValue = GetValue(node, nameof(ILLinkerAssembly.FeatureValue)),
                                FeatureDefault = GetValue(node, nameof(ILLinkerAssembly.FeatureDefault)),
                                Types = node.Elements("type")
                                .Select(node =>
                                {
                                    return new ILLinkerAssembly.Type
                                    {
                                        FullName = GetValue(node, nameof(ILLinkerAssembly.Type.FullName))?.Replace("/", ".") ?? throw new InvalidOperationException("FullName is required"),
                                        Preserve = GetValue(node, nameof(ILLinkerAssembly.Type.Preserve)),
                                        Fields = node.Elements("field")
                                        .Select(node =>
                                        {
                                            return new ILLinkerAssembly.Member
                                            {
                                                MemberType = ILLinkerAssembly.MemberType.Field,
                                                Name = GetValue(node, nameof(ILLinkerAssembly.Member.Name)),
                                                Signature = GetValue(node, nameof(ILLinkerAssembly.Member.Signature)),
                                                Body = GetValue(node, nameof(ILLinkerAssembly.Member.Body)),
                                                Value = GetValue(node, nameof(ILLinkerAssembly.Member.Value)),
                                            };
                                        }).ToList(),
                                        Properties = node.Elements("property")
                                        .Select(node =>
                                        {
                                            return new ILLinkerAssembly.Member
                                            {
                                                MemberType = ILLinkerAssembly.MemberType.Property,
                                                Name = GetValue(node, nameof(ILLinkerAssembly.Member.Name)),
                                                Signature = GetValue(node, nameof(ILLinkerAssembly.Member.Signature)),
                                                Body = GetValue(node, nameof(ILLinkerAssembly.Member.Body)),
                                                Value = GetValue(node, nameof(ILLinkerAssembly.Member.Value)),
                                            };
                                        }).ToList(),
                                        Methods = node.Elements("method")
                                        .Select(node =>
                                        {
                                            return new ILLinkerAssembly.Member
                                            {
                                                MemberType = ILLinkerAssembly.MemberType.Method,
                                                Name = GetValue(node, nameof(ILLinkerAssembly.Member.Name)),
                                                Signature = GetValue(node, nameof(ILLinkerAssembly.Member.Signature)),
                                                Body = GetValue(node, nameof(ILLinkerAssembly.Member.Body)),
                                                Value = GetValue(node, nameof(ILLinkerAssembly.Member.Value)),
                                            };
                                        }).ToList()
                                    };
                                }).ToList()
                            };
                        }).ToList();
                    Symbols.LinkerSubstitutions.AddRange(assemblies);
                }
                var atts = Compilation
                .Assembly
                .GetAttributes().Where(a => a.AttributeClass?.ToString() == typeof(LinkerSubstitutionAttribute).FullName);
                var decoratedILLink = new ILLinkerAssembly
                {
                    FullName = Compilation.Assembly.Name,
                    Types = atts.Select(att =>
                    {
                        var fullName = (string)att.ConstructorArguments[0].Value!;
                        var value = (string)att.ConstructorArguments[1].Value!;
                        var split = fullName.Split('.');
                        var typeName = string.Join(".", split.Take(split.Length - 1));
                        var memberName = fullName.Split('.').Last();
                        return (fullName, typeName, memberName, value);
                    }).GroupBy(e =>
                    {
                        return e.typeName;
                    }).Select(e =>
                    {
                        return new ILLinkerAssembly.Type
                        {
                            FullName = e.Key,
                            Fields = e.Select(ee => new ILLinkerAssembly.Member
                            {
                                MemberType = ILLinkerAssembly.MemberType.Field,
                                Body = "stub",
                                Name = ee.memberName,
                                Signature = ee.memberName,
                                Value = ee.value
                            }).ToList()
                        };
                    }).ToList()
                };
                Symbols.LinkerSubstitutions.Add(decoratedILLink);
            });

            ready = true;
        }

        ConcurrentDictionary<BaseTypeDeclarationSyntax, string> _fullTypeNameCache = new();
        ConcurrentDictionary<BaseNamespaceDeclarationSyntax, string> _fullNamespaceCache = new();

        //const string ShortenedNameIdentitfier = "\\";
        //string ShortName(string? shortPrefix, string? longPrefix, string signature, string name, Dictionary<string, string> usedNames, bool generate = true)
        //{
        //    if (!generate || !OutputMode.HasFlag(OutputMode.ShortNames))
        //        return (shortPrefix != null ? shortPrefix + "." : "") + name;
        //    if (name.Length <= 3)
        //        return name;
        //    var shortenSegment = name;
        //    bool hasGlobal = false;
        //    if (name.StartsWith(GlobalName) && name[GlobalName.Length] == '.')
        //    {
        //        hasGlobal = true;
        //        shortenSegment = name.Substring(GlobalName.Length + 1);
        //    }
        //    //string? keepSuffix = null;
        //    //if (shortenSegment.EndsWith("$") || (char.IsDigit(shortenSegment[shortenSegment.Length - 1]) && shortenSegment.Contains('$')))
        //    //{
        //    //    int l = shortenSegment.Length;
        //    //    while (char.IsDigit(shortenSegment[l - 1]))
        //    //    {
        //    //        l--;
        //    //    }
        //    //    while (shortenSegment[l - 1] == '$')
        //    //    {
        //    //        l--;
        //    //    }
        //    //    keepSuffix = shortenSegment.Substring(l);
        //    //    shortenSegment = shortenSegment.Substring(0, l);
        //    //}
        //    string shortName = "";
        //    bool startSingleCharacterCapture = true;
        //    List<char> possibleCamelCaseNameOverloadVariations = new List<char>();
        //    for (int i = 0; i < shortenSegment.Length; i++)
        //    {
        //        if (OutputMode.HasFlag(OutputMode.ShortNamesTryUseCamelCase))
        //        {
        //            if (i > 0 && char.IsUpper(shortenSegment[i]) && char.IsLower(shortenSegment[i - 1]))
        //            {
        //                possibleCamelCaseNameOverloadVariations.Add(shortenSegment[i]);
        //            }
        //        }
        //        if (startSingleCharacterCapture)
        //        {
        //            if (shortenSegment[i] != ShortenedNameIdentitfier[0]) //this segment is already shortened, dont shorten it again
        //            {
        //                shortName += /*ShortenedNameIdentitfier +*/ shortenSegment[i];
        //            }
        //            startSingleCharacterCapture = false;
        //            possibleCamelCaseNameOverloadVariations.Clear();
        //        }
        //        else if (shortenSegment[i] == '.' || shortenSegment[i] == '_' || shortenSegment[i] == '$')
        //        {
        //            if (shortenSegment[i] != '_')
        //                shortName += /*ShortenedNameIdentitfier +*/ shortenSegment[i];
        //            if (i < shortenSegment.Length - 1 && shortenSegment[i + 1] == '$')
        //            {
        //                while (i < shortenSegment.Length - 1 && shortenSegment[i + 1] == '$') //keep generic argument $ marker
        //                {
        //                    shortName += '$';
        //                    i++;
        //                }
        //                //startSingleCharacterCapture = false;
        //            }
        //            //else
        //            startSingleCharacterCapture = true;
        //        }
        //    }
        //    //var splitted = shortenSegment.Split(['.','$'], StringSplitOptions.RemoveEmptyEntries);
        //    //var shortName = string.Join(".", splitted.Select(s => s[0]));
        //    //splitted = shortName.Split(['$'], StringSplitOptions.RemoveEmptyEntries);
        //    //shortName = string.Join("$", splitted.Select(s => s[0]));
        //    if (hasGlobal)
        //    {
        //        shortName = GlobalName + "." + shortName;
        //    }
        //    shortName = (shortPrefix != null ? shortPrefix + "." : "") + shortName;
        //    string? padded = null;
        //    //if we can form a unique name using its camel case pattern, the use that
        //    if (possibleCamelCaseNameOverloadVariations.Count > 0)
        //    {
        //        string sn = shortName;
        //        padded = "";
        //        int i = 0;
        //        while (usedNames.TryGetValue(sn, out _))
        //        {
        //            if (i >= possibleCamelCaseNameOverloadVariations.Count)
        //                break;
        //            sn += possibleCamelCaseNameOverloadVariations[i];
        //            i++;
        //        }
        //        if (!usedNames.TryGetValue(sn, out _))
        //        {
        //            shortName = sn;
        //        }
        //    }
        //    int nextTry = 1;
        //    padded = null;
        //    while (usedNames.TryGetValue(shortName, out _))
        //    {
        //        if (padded != null)
        //            shortName = shortName.Substring(0, shortName.Length - padded.Length);
        //        padded = nextTry.ToString();
        //        shortName += padded;
        //        nextTry++;
        //    }
        //    usedNames.Add(shortName, signature);
        //    //usedNames.Add(shortName, (longPrefix != null ? longPrefix + "." : "") + name + suffix);
        //    //shortName += keepSuffix;
        //    return shortName;
        //}

        //string ComputeInvocatioNameForType(ITypeSymbol type, string? overloadName)
        //{
        //    if (type is IArrayTypeSymbol arr)
        //    {
        //        return $"{GlobalName}.$TypeArray({ComputeInvocatioNameForType(arr.ElementType, null)})";
        //    }
        //    if (type is ITypeParameterSymbol tp)
        //        return tp.Name;
        //    if (overloadName == null)
        //    {
        //        var typeMeta = GetRequiredMetadata(type);
        //        overloadName = typeMeta.OverloadName ?? throw new InvalidOperationException("Containing type must be processed before contained type");
        //    }
        //    string invocationName = overloadName ?? type.Name;
        //    if (OutputMode.HasFlag(OutputMode.Global))
        //    {
        //        if (type.ContainingSymbol is INamedTypeSymbol container)
        //        {
        //            //inner type
        //            var containingType = GetRequiredMetadata(container);
        //            if (containingType.InvocationName == null)
        //                throw new InvalidOperationException("Containing type must be processed before contained type");
        //            invocationName = containingType.InvocationName + "." + (overloadName ?? type.Name);
        //        }
        //        else
        //        {
        //            invocationName = overloadName ?? type.Name;
        //        }
        //    }
        //    if (type is INamedTypeSymbol nt && nt.Arity > 0)
        //    {
        //        invocationName += "(";
        //        invocationName += string.Join(", ", nt.TypeArguments.Select(c => ComputeInvocatioNameForType(c, null)));
        //        invocationName += ")";
        //    }
        //    return invocationName;
        //}

        //string ComputeInvocationNameForField(IFieldSymbol field, string? overloadName)
        //{
        //    if (overloadName == null)
        //    {
        //        var typeMeta = GetRequiredMetadata(field);
        //        overloadName = typeMeta.OverloadName ?? throw new InvalidOperationException("Containing type must be processed before contained type");
        //    }
        //    var invocationName = overloadName;
        //    if (field.IsStatic)
        //    {
        //        var declaringType = field.ContainingType;
        //        var declaringTypeMetadata = GetRequiredMetadata(declaringType);
        //        invocationName = declaringTypeMetadata.InvocationName + "." + overloadName;
        //    }
        //    return invocationName;
        //}

        //string ComputeInvocatioNameForProperty(IPropertySymbol property, string? overloadName)
        //{
        //    if (overloadName == null)
        //    {
        //        var typeMeta = GetRequiredMetadata(property);
        //        overloadName = typeMeta.OverloadName ?? throw new InvalidOperationException("Containing type must be processed before contained type");
        //    }
        //    var invocationName = overloadName;
        //    if (property.IsStatic)
        //    {
        //        var declaringType = property.ContainingType;
        //        var declaringTypeMetadata = GetRequiredMetadata(declaringType);
        //        invocationName = declaringTypeMetadata.InvocationName + "." + invocationName;
        //    }
        //    return invocationName;
        //}

        //string ComputeInvocatioNameForMethod(IMethodSymbol method, string? overloadName)
        //{
        //    var invocationName = overloadName ?? method.Name;
        //    if (method.Arity > 0)
        //    {
        //        if (!HasAttribute(method, typeof(IgnoreGenericAttribute).FullName, null, false, out _))
        //        {
        //            invocationName += "(";
        //            invocationName += string.Join(",", method.TypeArguments.Select(c => ComputeInvocatioNameForType(c, null)));
        //            invocationName += ")";
        //        }
        //    }
        //    if (method.IsStatic)
        //    {
        //        var declaringType = method.ContainingType;
        //        var declaringTypeMetadata = GetRequiredMetadata(declaringType);
        //        invocationName = declaringTypeMetadata.InvocationName + "." + invocationName;
        //    }
        //    return invocationName;
        //}

        public string ResolveFullNamespace(FileScopedNamespaceDeclarationSyntax type, string nameSeparator = ".")
        {
            if (nameSeparator == "." && _fullNamespaceCache.TryGetValue(type, out var cached))
                return cached;
            var ret = type.Name.ToString().Trim();
            if (nameSeparator != ".")
                ret = ret.Replace(".", nameSeparator);
            if (nameSeparator == ".")
                _fullNamespaceCache[type] = ret;
            return ret;
        }

        public string ResolveFullNamespace(NamespaceDeclarationSyntax type, string nameSeparator = ".")
        {
            if (nameSeparator == "." && _fullNamespaceCache.TryGetValue(type, out var cached))
                return cached;
            string? parent = null;
            if (type.Parent is NamespaceDeclarationSyntax ns)
            {
                parent = ResolveFullNamespace(ns);
            }
            var ret = parent + type.Name.ToString().Trim();
            if (nameSeparator != ".")
                ret = ret.Replace(".", nameSeparator);
            if (nameSeparator == ".")
                _fullNamespaceCache[type] = ret;
            return ret;
        }

        public string? ResolveFullNamespace(BaseTypeDeclarationSyntax type)
        {
            NamespaceDeclarationSyntax? ns = null;
            var parent = type.Parent;
            if (parent == null)
                return null;
            while (parent is not NamespaceDeclarationSyntax)
                parent = parent?.Parent;
            if (parent == null)
                return null;
            ns = (NamespaceDeclarationSyntax)parent;
            var ret = ResolveFullNamespace(ns);
            return ret;
        }

        public string ResolveTypeName(BaseTypeDeclarationSyntax type)
        {
            var typeName = type.Identifier.ResolveIdentifierName();
            if (type is TypeDeclarationSyntax t && t.Arity > 0)
            {
                typeName = $"{typeName}${t.Arity}";
            }
            return typeName;
        }

        public string ResolveFullTypeName(BaseTypeDeclarationSyntax type)
        {
            if (_fullTypeNameCache.TryGetValue(type, out var cached))
                return cached;
            string? parent = null;
            if (type.Parent is TypeDeclarationSyntax ts)
            {
                parent = ResolveFullTypeName(ts) + ".";
            }
            else if (type.Parent is NamespaceDeclarationSyntax ns)
            {
                parent = ResolveFullNamespace(ns) + ".";
            }
            var ret = parent + type.Identifier.ResolveIdentifierName().TrimEnd('?');
            _fullTypeNameCache[type] = ret;
            return ret;
        }

        public MemberDeclarationSyntax? GetTypeDeclaration(string typeName, TranslatorSyntaxVisitor? visitor)
        {
            var value = AllSymbols.GetValueOrDefault(typeName);
            if (value?.DeclaringReferences?.Any() ?? false)
                return (MemberDeclarationSyntax)value.DeclaringReferences.First().GetSyntax();
            if (visitor?.CurrentTypeNamespace != null)
            {
                var splitted = visitor.CurrentTypeNamespace.Split('.');
                for (int i = 0; i < splitted.Length; i++)
                {
                    var ns = string.Join(".", splitted.Take(i + 1));
                    var im = $"{ns}.{typeName}";
                    value = AllSymbols.GetValueOrDefault(im);
                    if (value?.DeclaringReferences?.Any() ?? false)
                        return (MemberDeclarationSyntax)value.DeclaringReferences.First().GetSyntax();
                }
            }
            if (visitor?.ImportedNamespace != null)
            {
                foreach (var i in visitor.ImportedNamespace)
                {
                    var im = $"{i}.{typeName}";
                    value = AllSymbols.GetValueOrDefault(im);
                    if (value?.DeclaringReferences?.Any() ?? false)
                        return (MemberDeclarationSyntax)value.DeclaringReferences.First().GetSyntax();
                }
            }
            return null;
            //var tryFullNames = new string[] { currentNamespace ?? "" }.Concat(importedNamespace).Select(i => $"{i}.{typeName}").ToList();
            //var targetType = global.TypeNodes.FirstOrDefault(e => e.Parent.IsKind(SyntaxKind.NamespaceDeclaration) && tryFullNames.Contains(global.ResolveFullTypeName(e)));
            //return targetType;
        }

        public ISymbol GetSymbol(string typeName, TranslatorSyntaxVisitor? visitor)
        {
            var ret = TryGetSymbol(typeName, visitor);
            if (ret == null)
            {
                throw new InvalidOperationException($"Cannot find symbol {typeName}");
            }
            return ret;
        }

        public INamedTypeSymbol SystemVoid => field ??= (INamedTypeSymbol)GetSymbol("System.Void", null);
        public INamedTypeSymbol SystemObject => field ??= (INamedTypeSymbol)GetSymbol("System.Object", null);
        public INamedTypeSymbol SystemValueType => field ??= (INamedTypeSymbol)GetSymbol("System.ValueType", null);
        public INamedTypeSymbol SystemBoolean => field ??= (INamedTypeSymbol)GetSymbol("System.Boolean", null);
        public INamedTypeSymbol SystemChar => field ??= (INamedTypeSymbol)GetSymbol("System.Char", null);
        public INamedTypeSymbol SystemSByte => field ??= (INamedTypeSymbol)GetSymbol("System.SByte", null);
        public INamedTypeSymbol SystemByte => field ??= (INamedTypeSymbol)GetSymbol("System.Byte", null);
        public INamedTypeSymbol SystemString => field ??= (INamedTypeSymbol)GetSymbol("System.String", null);
        public INamedTypeSymbol SystemInt16 => field ??= (INamedTypeSymbol)GetSymbol("System.Int16", null);
        public INamedTypeSymbol SystemUInt16 => field ??= (INamedTypeSymbol)GetSymbol("System.UInt16", null);
        public INamedTypeSymbol SystemInt32 => field ??= (INamedTypeSymbol)GetSymbol("System.Int32", null);
        public INamedTypeSymbol SystemUInt32 => field ??= (INamedTypeSymbol)GetSymbol("System.UInt32", null);
        public INamedTypeSymbol SystemIntPtr => field ??= (INamedTypeSymbol)GetSymbol("System.IntPtr", null);
        public INamedTypeSymbol SystemUIntPtr => field ??= (INamedTypeSymbol)GetSymbol("System.UIntPtr", null);
        public INamedTypeSymbol SystemInt64 => field ??= (INamedTypeSymbol)GetSymbol("System.Int64", null);
        public INamedTypeSymbol SystemUInt64 => field ??= (INamedTypeSymbol)GetSymbol("System.UInt64", null);
        public INamedTypeSymbol SystemDouble => field ??= (INamedTypeSymbol)GetSymbol("System.Double", null);
        public INamedTypeSymbol SystemSingle => field ??= (INamedTypeSymbol)GetSymbol("System.Single", null);
        public INamedTypeSymbol SystemArray => field ??= (INamedTypeSymbol)GetSymbol("System.Array", null);
        public INamedTypeSymbol SystemEnum => field ??= (INamedTypeSymbol)GetSymbol("System.Enum", null);
        public INamedTypeSymbol SystemEnumT => field ??= (INamedTypeSymbol)GetSymbol("System.Enum<,>", null);
        public INamedTypeSymbol SystemType => field ??= (INamedTypeSymbol)GetSymbol("System.Type", null);
        public INamedTypeSymbol SystemReadOnlySpan => field ??= (INamedTypeSymbol)GetSymbol("System.ReadOnlySpan<>", null);
        public INamedTypeSymbol SystemSpan => field ??= (INamedTypeSymbol)GetSymbol("System.Span<>", null);
        public INamedTypeSymbol SystemRange => field ??= (INamedTypeSymbol)GetSymbol("System.Range", null);
        public INamedTypeSymbol SystemIndex => field ??= (INamedTypeSymbol)GetSymbol("System.Index", null);
        public INamedTypeSymbol SystemDelegate => field ??= (INamedTypeSymbol)GetSymbol("System.Delegate", null);
        public INamedTypeSymbol SystemMulticastDelegate => field ??= (INamedTypeSymbol)GetSymbol("System.MulticastDelegate", null);
        public INamedTypeSymbol SystemIEnumerable => field ??= (INamedTypeSymbol)GetSymbol("System.Collections.IEnumerable", null);
        public INamedTypeSymbol SystemIEnumerator => field ??= (INamedTypeSymbol)GetSymbol("System.Collections.IEnumerator", null);
        public INamedTypeSymbol SystemIEnumerableT => field ??= (INamedTypeSymbol)GetSymbol("System.Collections.Generic.IEnumerable<>", null);
        public INamedTypeSymbol SystemIAsyncEnumerableT => field ??= (INamedTypeSymbol)GetSymbol("System.Collections.Generic.IAsyncEnumerable<>", null);
        public INamedTypeSymbol SystemIEnumeratorT => field ??= (INamedTypeSymbol)GetSymbol("System.Collections.Generic.IEnumerator<>", null);
        public INamedTypeSymbol SystemIAsyncEnumeratorT => field ??= (INamedTypeSymbol)GetSymbol("System.Collections.Generic.IAsyncEnumerator<>", null);
        public INamedTypeSymbol SystemIDisposable => field ??= (INamedTypeSymbol)GetSymbol("System.IDisposable", null);
        public INamedTypeSymbol SystemIAsyncDisposable => field ??= (INamedTypeSymbol)GetSymbol("System.IAsyncDisposable", null);
        public INamedTypeSymbol SystemThreadingTask => field ??= (INamedTypeSymbol)GetSymbol("System.Threading.Tasks.Task", null);
        public INamedTypeSymbol SystemBaseRefOrPointer => field ??= (INamedTypeSymbol)GetSymbol("NetJs.RefOrPointer", null);
        public INamedTypeSymbol SystemRefOrPointer => field ??= (INamedTypeSymbol)GetSymbol("NetJs.RefOrPointer<>", null);
        public INamedTypeSymbol SystemReference => field ??= (INamedTypeSymbol)GetSymbol("NetJs.Ref<>", null);
        public INamedTypeSymbol SystemPointer => field ??= (INamedTypeSymbol)GetSymbol("NetJs.Pointer<>", null);
        public INamedTypeSymbol SystemNullable => field ??= (INamedTypeSymbol)GetSymbol("System.Nullable<>", null);
        public INamedTypeSymbol SystemDynamic => field ??= (INamedTypeSymbol)GetSymbol("NetJs.DynamicType", null);
        public INamedTypeSymbol SystemT1 => field ??= (INamedTypeSymbol)GetSymbol("T1", null);
        public INamedTypeSymbol SystemT2 => field ??= (INamedTypeSymbol)GetSymbol("T2", null);
        public INamedTypeSymbol SystemT3 => field ??= (INamedTypeSymbol)GetSymbol("T3", null);
        public INamedTypeSymbol SystemT4 => field ??= (INamedTypeSymbol)GetSymbol("T4", null);
        public INamedTypeSymbol SystemT5 => field ??= (INamedTypeSymbol)GetSymbol("T5", null);
        public INamedTypeSymbol SystemT6 => field ??= (INamedTypeSymbol)GetSymbol("T6", null);
        public INamedTypeSymbol SystemT7 => field ??= (INamedTypeSymbol)GetSymbol("T7", null);
        public INamedTypeSymbol SystemT8 => field ??= (INamedTypeSymbol)GetSymbol("T8", null);
        public INamedTypeSymbol SystemT9 => field ??= (INamedTypeSymbol)GetSymbol("T9", null);
        public INamedTypeSymbol SystemT10 => field ??= (INamedTypeSymbol)GetSymbol("T10", null);
        public INamedTypeSymbol SystemT11 => field ??= (INamedTypeSymbol)GetSymbol("T11", null);
        public INamedTypeSymbol SystemT12 => field ??= (INamedTypeSymbol)GetSymbol("T12", null);
        public INamedTypeSymbol SystemT13 => field ??= (INamedTypeSymbol)GetSymbol("T13", null);
        public INamedTypeSymbol SystemT14 => field ??= (INamedTypeSymbol)GetSymbol("T14", null);
        public INamedTypeSymbol SystemT15 => field ??= (INamedTypeSymbol)GetSymbol("T15", null);
        public INamedTypeSymbol SystemT16 => field ??= (INamedTypeSymbol)GetSymbol("T16", null);
        public INamedTypeSymbol SystemT17 => field ??= (INamedTypeSymbol)GetSymbol("T17", null);
        public INamedTypeSymbol SystemT18 => field ??= (INamedTypeSymbol)GetSymbol("T18", null);
        public INamedTypeSymbol SystemT19 => field ??= (INamedTypeSymbol)GetSymbol("T19", null);
        public INamedTypeSymbol SystemT20 => field ??= (INamedTypeSymbol)GetSymbol("T20", null);
        public INamedTypeSymbol SystemT21 => field ??= (INamedTypeSymbol)GetSymbol("T21", null);
        public INamedTypeSymbol SystemT22 => field ??= (INamedTypeSymbol)GetSymbol("T22", null);
        public INamedTypeSymbol SystemT23 => field ??= (INamedTypeSymbol)GetSymbol("T23", null);
        public INamedTypeSymbol SystemT24 => field ??= (INamedTypeSymbol)GetSymbol("T24", null);
        public INamedTypeSymbol SystemT25 => field ??= (INamedTypeSymbol)GetSymbol("T25", null);
        public INamedTypeSymbol SystemT26 => field ??= (INamedTypeSymbol)GetSymbol("T26", null);
        public INamedTypeSymbol SystemT27 => field ??= (INamedTypeSymbol)GetSymbol("T27", null);
        public INamedTypeSymbol SystemT28 => field ??= (INamedTypeSymbol)GetSymbol("T28", null);
        public INamedTypeSymbol SystemT29 => field ??= (INamedTypeSymbol)GetSymbol("T29", null);
        public INamedTypeSymbol SystemT30 => field ??= (INamedTypeSymbol)GetSymbol("T30", null);
        public INamedTypeSymbol SystemT31 => field ??= (INamedTypeSymbol)GetSymbol("T31", null);
        public INamedTypeSymbol SystemT32 => field ??= (INamedTypeSymbol)GetSymbol("T32", null);
        public INamedTypeSymbol AnonymousType => field ??= (INamedTypeSymbol)GetSymbol("AnonymousType<>", null);

        public INamedTypeSymbol DeletedObject => field ??= (INamedTypeSymbol)GetSymbol("DeletedObject", null);

        public INamedTypeSymbol AwaitableInterface => field ??= (INamedTypeSymbol)GetSymbol("NetJs.IAwaitable", null);
        public INamedTypeSymbol SystemValueTuple(int nItems) => (INamedTypeSymbol)GetSymbol($"System.ValueTuple<{string.Join(",", Enumerable.Range(1, nItems).Select(r => ""))}>", null);

        public ITypeSymbol Union(IEnumerable<ITypeSymbol> types, TranslatorSyntaxVisitor? visitor)
        {
            var n = types.Count();
            var u = (INamedTypeSymbol)GetSymbol($"dotnetJs.Union<{string.Join(",", Enumerable.Range(1, n).Select(r => ""))}>", visitor);
            return u.Construct(types.ToArray());
        }

        public ITypeSymbol AsDelegate(ISymbol symbol, TranslatorSyntaxVisitor? visitor)
        {
            if (symbol is IMethodSymbol method)
            {
                if (method.ReturnType == null)
                {
                    if (method.Parameters.Count() == 0)
                    {
                        return (INamedTypeSymbol)GetSymbol($"dotnetJs.Action", visitor);
                    }
                    else
                    {
                        var action = (INamedTypeSymbol)GetSymbol($"dotnetJs.Action<{string.Join(",", Enumerable.Range(1, method.Parameters.Count()).Select(r => ""))}>", visitor);
                        action = action.Construct(method.Parameters.Select(p => p.Type).ToArray());
                        return action;
                    }
                }
                else
                {
                    var function = (INamedTypeSymbol)GetSymbol($"dotnetJs.Function<{string.Join(",", Enumerable.Range(1, method.Parameters.Count() + 1).Select(r => ""))}>", visitor);
                    function = function.Construct(method.Parameters.Select(p => p.Type).Concat([method.ReturnType]).ToArray());
                    return function;
                }
            }
            var mfunction = (INamedTypeSymbol)GetSymbol($"dotnetJs.Function<>", visitor);
            mfunction = mfunction.Construct([(symbol as IPropertySymbol)?.Type ?? (symbol as IFieldSymbol)?.Type ?? throw new InvalidOperationException("Cannot determine delegate type")]);
            return mfunction;
        }

        public ISymbol AdjustConcreteArrayType(ISymbol symbol)
        {
            //I'd expect the roslyn api to handle this scenario
            //byte[] bb;
            //bb[1] should be bound to the System.Array [] operator, but it isn't. Obviously because the return type (object) isnt what it should be(byte)
            //But our generator needs to know that operator for correctness. We have defined a System.Array<T> stub type to handle this scenario
            if (symbol is ITypeSymbol t && t.IsArray(out var elementType)/* && elementType.SpecialType != SpecialType.System_Object*/)
            {
                var arrayT = (INamedTypeSymbol)GetSymbol("System.Array<>", null);
                arrayT = arrayT.Construct([elementType]);
                return arrayT;
            }
            return symbol;
        }

        public ISymbol? ResolveSymbol(CodeSymbol type, TranslatorSyntaxVisitor? visitor/*, out ISymbol? declaringType, out SymbolKind declaringKind*/)
        {
            if (type.TypeSyntaxOrSymbol == null)
            {
                //declaringType = null;
                //declaringKind = SymbolKind.ErrorType;
                return null;
            }
            if (type.TypeSyntaxOrSymbol is IPropertySymbol property)
            {
                //declaringType = property;
                //declaringKind = SymbolKind.Property;
                return property;
            }
            if (type.TypeSyntaxOrSymbol is IFieldSymbol field)
            {
                //declaringType = field;
                //declaringKind = SymbolKind.Field;
                return field;
            }
            if (type.TypeSyntaxOrSymbol is ILocalSymbol local)
            {
                //declaringType = local;
                //declaringKind = SymbolKind.Local;
                return local;
            }
            if (type.TypeSyntaxOrSymbol is IParameterSymbol parameter)
            {
                //declaringType = parameter;
                //declaringKind = SymbolKind.Parameter;
                return parameter;
            }
            if (type.TypeSyntaxOrSymbol is ITypeParameterSymbol tparameter)
            {
                //declaringType = tparameter.DeclaringType;
                //declaringKind = SymbolKind.TypeParameter;
                return tparameter;
            }
            if (type.TypeSyntaxOrSymbol is IMethodSymbol method)
            {
                //declaringType = method;
                //declaringKind = SymbolKind.Method;
                return method;
            }
            if (type.TypeSyntaxOrSymbol is MemberSymbolOverload ov)
            {
                //declaringType = null;
                //declaringKind = SymbolKind.Method;
                return Union(ov.Overloads.Select(e => AsDelegate(e, visitor)), visitor);
            }
            //declaringType = null;
            //declaringKind = SymbolKind.ErrorType;
            var symbol = type.TypeSyntaxOrSymbol as ISymbol ??
            (type.TypeSyntaxOrSymbol is TypeSyntax ts ? TryGetSymbol(ts, visitor) : type.TypeSyntaxOrSymbol is TypeParameterSyntax tps ? TryGetSymbol(tps.Identifier.ValueText, visitor) : type.TypeSyntaxOrSymbol is BaseTypeDeclarationSyntax typ ? GetSymbol(typ, visitor) : null);
            return symbol;
        }

        bool alreadyLookingInClosure;
        public ISymbol? TryGetSymbol(string typeName, TranslatorSyntaxVisitor? visitor, Func<ISymbol, bool>? filterCandidate = null)
        {
            if (!ready)
            {
                var s = Compilation.GetTypeByMetadataName(typeName);
                //declaringSymbol = null;
                //declaringSymbolKind = SymbolKind.ErrorType;
                return s;
            }
            bool isNullable = typeName.EndsWith("?");
            typeName = typeName.RemoveGenericParameterNames(out var genericTypes);
            ISymbol Return(ISymbol symbol)
            {
                if (visitor != null && symbol.Kind == SymbolKind.NamedType && symbol is INamedTypeSymbol ns)
                {
                    visitor.Dependencies.Add(ns);
                }
                if (genericTypes?.Length > 0 && genericTypes.All(e => e.Length > 0))
                {
                    var ps = genericTypes.Select((s, i) => TryGetSymbol(s, visitor, filterCandidate: (s) => s is ITypeSymbol) ?? ((INamedTypeSymbol)symbol).TypeArguments.ElementAt(i)).ToList();
                    if (ps.Any(p => p.Kind != SymbolKind.TypeParameter))
                        symbol = ((INamedTypeSymbol)symbol).Construct(ps.Cast<ITypeSymbol>().ToArray());
                }
                if (isNullable && ((ITypeSymbol)symbol).IsValueType)
                {
                    return SystemNullable.Construct([(ITypeSymbol)symbol]);
                }
                return symbol;
            }
            typeName = typeName.TrimEnd('?');
            if (typeName == "dynamic")
            {
                //declaringSymbol = null;// SymbolScope.Type;
                //declaringSymbolKind = SymbolKind.ErrorType;
                if (filterCandidate?.Invoke(Compilation.DynamicType) ?? true)
                    return Compilation.DynamicType;
            }
            if (typeName.StartsWith("(") && typeName.EndsWith(")"))
            {
                var tupleInnerTypes = typeName.ResolveTupleTypes();
                if (tupleInnerTypes != null)
                {
                    var tupleElementTypes = tupleInnerTypes.Value.Types.Select(s => (ITypeSymbol)GetSymbol(s, visitor)).ToArray();
                    //declaringSymbol = null;// SymbolScope.Type;
                    //declaringSymbolKind = SymbolKind.ErrorType;
                    var type = Compilation.CreateTupleTypeSymbol(ImmutableArray.Create(tupleElementTypes), tupleInnerTypes.Value.Names != null ? ImmutableArray.Create<string?>(tupleInnerTypes.Value.Names.ToArray()) : default);
                    if (filterCandidate?.Invoke(type) ?? true)
                        return Return(type);
                }
            }
            if (!alreadyLookingInClosure && visitor != null)
            {
                alreadyLookingInClosure = true;
                try
                {
                    foreach (var closure in visitor.Closures) //check the current closures, if it hold the type we are looking for
                    {
                        var type = closure.GetIdentifierType(typeName);
                        if (type.TypeSyntaxOrSymbol != null)
                        {
                            var ret = ResolveSymbol(type, visitor/*, out declaringSymbol, out declaringSymbolKind*/);
                            if (ret != null)
                            {
                                //if (type.Kind != SymbolKind.Alias && type.Kind != SymbolKind.ErrorType)
                                //{
                                //    declaringSymbolKind = type.Kind;
                                //}
                                //declaringSymbol ??= closure.Symbol;// SymbolScope.Member;
                                if (filterCandidate?.Invoke(ret) ?? true)
                                    return ret;
                            }
                        }
                    }
                }
                finally
                {
                    alreadyLookingInClosure = false;
                }
            }
            if (visitor?.AliasNamespace != null)
            {
                if (visitor.AliasNamespace.TryGetValue(typeName, out var tvalue))
                {
                    typeName = tvalue;
                    typeName = typeName.RemoveGenericParameterNames(out genericTypes);
                }
            }
            typeName = typeName.ResolvePredefinedTypeName();
            var value = AllSymbols.GetValueOrDefault(typeName);
            var currentAssembly = Compilation.Assembly;
            var slug = GetAssemblyGlobalSlug(currentAssembly);
            bool ForEachAssembly(Func<string, bool> iterate, string? prefixTypeName = null)
            {
                var n = slug + "." + (prefixTypeName != null ? prefixTypeName + "." : "") + typeName;
                var lTypeName = GlobalName + "." + n;
                var result = iterate(lTypeName);
                if (result)
                    return result;
                lTypeName = n;
                result = iterate(lTypeName);
                if (result)
                    return result;
                var dependencies = Compilation.SourceModule.ReferencedAssemblySymbols;
                foreach (var dep in dependencies)
                {
                    slug = GetAssemblyGlobalSlug(dep);
                    n = slug + "." + (prefixTypeName != null ? prefixTypeName + "." : "") + typeName;
                    lTypeName = GlobalName + "." + n;
                    result = iterate(lTypeName);
                    if (result)
                        return result;
                    lTypeName = n;
                    result = iterate(lTypeName);
                    if (result)
                        return result;
                }
                return false;
            }
            if (value == null)
            {
                ForEachAssembly(lTypeName =>
                {
                    value = AllSymbols.GetValueOrDefault(lTypeName);
                    return value != null;
                });
            }
            //source = SymbolScope.Type;
            if (value?.Symbol != null)
            {
                //declaringSymbol = value.Symbol.ContainingSymbol;
                //declaringSymbolKind = declaringSymbol.Kind;
                if (filterCandidate?.Invoke(value.Symbol) ?? true)
                    return Return(value.Symbol);
            }
            if (visitor?.CurrentTypeNamespace != null)
            {
                var splitted = visitor.CurrentTypeNamespace.Split('.');
                for (int i = 0; i < splitted.Length; i++)
                {
                    var ns = string.Join(".", splitted.Take(i + 1));
                    var im = $"{ns}.{typeName}";
                    value = AllSymbols.GetValueOrDefault(im);
                    if (value == null)
                    {
                        ForEachAssembly(lTypeName =>
                        {
                            value = AllSymbols.GetValueOrDefault(lTypeName);
                            if (value != null)
                            {
                                if (filterCandidate?.Invoke(value.Symbol) ?? true)
                                    return true;
                            }
                            return false;
                        }, ns);
                    }
                    if (value?.Symbol != null)
                    {
                        //declaringSymbol = value.Symbol.ContainingSymbol;
                        //declaringSymbolKind = declaringSymbol.Kind;
                        if (filterCandidate?.Invoke(value.Symbol) ?? true)
                            return Return(value.Symbol);
                    }
                }
            }
            //foreach (var i in lookInNamespace)
            //{
            //    var im = $"{i}.{typeName}";
            //    value = global.AllSymbols.GetValueOrDefault(im);
            //    if (value.Symbol != null)
            //        return value.Symbol;
            //}
            if (visitor != null)
            {
                foreach (var i in visitor.ImportedNamespace)
                {
                    var im = $"{i}.{typeName}";
                    value = AllSymbols.GetValueOrDefault(im);
                    if (value == null)
                    {
                        ForEachAssembly(lTypeName =>
                        {
                            value = AllSymbols.GetValueOrDefault(lTypeName);
                            if (value != null)
                            {
                                if (filterCandidate?.Invoke(value.Symbol) ?? true)
                                    return true;
                            }
                            return false;
                        }, $"{i}");
                    }
                    if (value?.Symbol != null)
                    {
                        //declaringSymbol = value.Symbol.ContainingSymbol;
                        //declaringSymbolKind = declaringSymbol.Kind;
                        if (filterCandidate?.Invoke(value.Symbol) ?? true)
                            return Return(value.Symbol);
                    }
                }
                //TODO: remove this, we only added it so we can match with primitives in short forms (eg int), define in H5.dll
                //targetType2 = global.TypeSymbols.FirstOrDefault(e => typeName.Equals(global.ResolveFullTypeName(e), StringComparison.InvariantCultureIgnoreCase));
                typeName = typeName.ToLower();
                foreach (var i in visitor.ImportedNamespace)
                {
                    var im = $"{i}.{typeName}";
                    value = AllSymbols.GetValueOrDefault(im);
                    if (value == null)
                    {
                        ForEachAssembly(lTypeName =>
                        {
                            value = AllSymbols.GetValueOrDefault(lTypeName);
                            if (value != null)
                            {
                                if (filterCandidate?.Invoke(value.Symbol) ?? true)
                                    return true;
                            }
                            return false;
                        }, $"{i}");
                    }
                    if (value?.Symbol != null)
                    {
                        //declaringSymbol = value.Symbol.ContainingSymbol;
                        //declaringSymbolKind = declaringSymbol.Kind;
                        if (filterCandidate?.Invoke(value.Symbol) ?? true)
                            return Return(value.Symbol);
                    }
                }
            }
            //declaringSymbol = null;
            //declaringSymbolKind = SymbolKind.ErrorType;
            return null;
        }


        //public ISymbol? GetTypeSymbol(IdentifierNameSyntax id, TranslatorSyntaxVisitor? visitor, out ISymbol? declaringSymbol, out SymbolKind declaringKind)
        //{
        //    return /*semanticModel.GetSymbolInfo(id).Symbol ??*/ TryGetTypeSymbol(id.Identifier.ValueText, visitor, out declaringSymbol, out declaringKind);
        //}

        //public ISymbol? TryGetTypeSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor)
        //{
        //    var ret = SyntaxSymbols.GetValueOrDefault(syntax)?.First().Symbol;
        //    if (ret == null && visitor != null)
        //    {
        //        foreach (var semanticModel in visitor.SemanticModels)
        //        {
        //            if (semanticModel.SyntaxTree == syntax.SyntaxTree)
        //            {
        //                ret = semanticModel.GetDeclaredSymbol(syntax);
        //                if (ret == null)
        //                    ret = semanticModel.GetSymbolInfo(syntax).Symbol;
        //                if (ret != null)
        //                    break;
        //            }
        //        }
        //    }
        //    return ret;
        //}

        public Optional<object?> EvaluateConstant(CSharpSyntaxNode expression, TranslatorSyntaxVisitor visitor)
        {
            if (expression.IsKind(SyntaxKind.Argument) && expression is ArgumentSyntax arg)
                expression = arg.Expression;
            foreach (var semanticModel in visitor.SemanticModels)
            {
                if (semanticModel.SyntaxTree == expression.SyntaxTree)
                {
                    var ret = semanticModel.GetConstantValue(expression);
                    if (ret.HasValue)
                        return ret;
                    break;
                }
            }
            if (expression.IsKind(SyntaxKind.IdentifierName) && expression is IdentifierNameSyntax id)
            {
                var value = visitor.GetIdentifierTypeInScope(id.Identifier.ValueText);
                if (value.TypeSyntaxOrSymbol is IFieldSymbol field && field.IsConst)
                {
                    return new Optional<object?>(field.ConstantValue);
                }
                if (value.TypeSyntaxOrSymbol is ILocalSymbol local && local.IsConst)
                {
                    return new Optional<object?>(local.ConstantValue);
                }
            }
            return default;
        }

        enum TypeWrapOperation
        {
            Array,
            Nullable,
            Pointer
        }

        ThreadLocal<List<SyntaxNode>> _gettingSyntax = new(() => new());
        public ISymbol? TryGetSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor/*, out ISymbol? declaringSymbol, out SymbolKind declaringKind*/)
        {
            while (syntax.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                if (((ParenthesizedExpressionSyntax)syntax).Expression.IsKind(SyntaxKind.SimpleAssignmentExpression))
                    break;
                syntax = ((ParenthesizedExpressionSyntax)syntax).Expression;
            }
            if (syntax.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                syntax = ((AssignmentExpressionSyntax)syntax).Left;
            }
            if (_gettingSyntax.Value!.Contains(syntax))
                return null;
            _gettingSyntax.Value.Add(syntax);
            try
            {
                var ret = SyntaxSymbols.GetValueOrDefault(syntax)?.First().Symbol;
                if (ret == null && visitor != null)
                {
                    foreach (var semanticModel in visitor.SemanticModels)
                    {
                        if (semanticModel.SyntaxTree == syntax.SyntaxTree)
                        {
                            ret = semanticModel.GetDeclaredSymbol(syntax);
                            if (ret == null
                                //&&
                                //(syntax.IsKind(SyntaxKind.IdentifierName) ||
                                //syntax.IsKind(SyntaxKind.QualifiedName) ||
                                //syntax.IsKind(SyntaxKind.PointerType) ||
                                //syntax.IsKind(SyntaxKind.NullableType) ||
                                //syntax.IsKind(SyntaxKind.ArrayType) ||
                                //syntax.IsKind(SyntaxKind.FunctionPointerType))
                                )
                                ret = semanticModel.GetSymbolInfo(syntax).Symbol;
                            if (ret != null)
                                break;
                            //only means of getting null type is checking what it is converted to
                            if (syntax.IsKind(SyntaxKind.NullLiteralExpression))
                            {
                                ret = (semanticModel.GetOperation(syntax)?.Parent as IConversionOperation)?.Type;
                                if (ret != null)
                                    break;
                            }
                        }
                    }
                }
                if (ret != null)
                {
                    if (visitor != null && ret is INamedTypeSymbol ns)
                    {
                        visitor.Dependencies.Add(ns);
                    }
                    //declaringSymbol = ret.ContainingSymbol;
                    //declaringKind = declaringSymbol?.Kind ?? SymbolKind.ErrorType;
                    return ret;
                }
                if (syntax is RefTypeSyntax rType)
                    syntax = rType.Type;
                else if (syntax is ScopedTypeSyntax sType)
                    syntax = sType.Type;
                if (syntax is TypeSyntax type)
                {
                    Stack<TypeWrapOperation> operations = new();
                    //int isArray = 0;
                    while (true)
                    {
                        if (type is ArrayTypeSyntax arr)
                        {
                            type = arr.ElementType;
                            operations.Push(TypeWrapOperation.Array);
                        }
                        else if (type is NullableTypeSyntax nt)
                        {
                            type = nt.ElementType;
                            operations.Push(TypeWrapOperation.Nullable);
                        }
                        else if (type is PointerTypeSyntax pt)
                        {
                            type = pt.ElementType;
                            operations.Push(TypeWrapOperation.Pointer);
                        }
                        else
                            break;
                    }
                    var typeName = type.ToString();
                    //GenericNameSyntax? genericName = null;
                    //typeName = type.SimplifyName(out genericName);
                    var symbol = TryGetSymbol(typeName, visitor, filterCandidate: (c) => c.Kind == SymbolKind.NamedType || c.Kind == SymbolKind.TypeParameter);
                    //if (symbol == null)
                    //return null;
                    Debug.Assert(symbol == null || symbol is ITypeSymbol);
                    //if (symbol is INamedTypeSymbol nt && nt.IsGenericType && genericName != null)
                    //{
                    //    var arguments = genericName.TypeArgumentList.Arguments.Select(a => TryGetTypeSymbol(a, visitor, out _, out _)).Cast<ITypeSymbol>().ToArray();
                    //    if (arguments.All(a => a != null && a is not ITypeParameterSymbol))
                    //    {
                    //        symbol = nt.Construct(arguments!);
                    //    }
                    //    else if (arguments.All(a => a == null))
                    //    {
                    //    }
                    //    else
                    //    {
                    //        //TODO: Need a way to construct partial generic type
                    //    }
                    //}
                    if (symbol != null)
                    {
                        while (operations.TryPop(out var op))
                        {
                            if (op == TypeWrapOperation.Array)
                                symbol = Compilation.CreateArrayTypeSymbol((ITypeSymbol)symbol);
                            else if (op == TypeWrapOperation.Nullable)
                                symbol = SystemNullable.Construct((ITypeSymbol)symbol);
                            else
                                symbol = Compilation.CreatePointerTypeSymbol((ITypeSymbol)symbol);
                        }
                        return symbol;
                    }
                }
                if (visitor != null && syntax is CSharpSyntaxNode cnode)
                {
                    var symbol = visitor.GetExpressionReturnSymbol(cnode);
                    if (symbol.TypeSyntaxOrSymbol is ISymbol msymbol)
                        return msymbol;
                    symbol = visitor.GetExpressionBoundTarget(cnode);
                    if (symbol.TypeSyntaxOrSymbol is ISymbol msymbol2)
                        return msymbol2;
                }
                //declaringSymbol = null;
                //declaringKind = SymbolKind.ErrorType;
                return null;
            }
            finally { _gettingSyntax.Value.Remove(syntax); }
        }

        public ITypeSymbol? TryGetTypeSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor/*, out ISymbol? declaringType, out SymbolKind declaringKind*/)
        {
            var ret = TryGetSymbol(syntax, visitor);
            if (ret != null)
            {
                return TryGetTypeSymbol(ret);
            }
            return null;
        }

        public ISymbol GetSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor)
        {
            var ret = TryGetSymbol(syntax, visitor);
            if (ret == null)
            {
                throw new InvalidOperationException($"Cannot find symbol for syntax");
            }
            return ret;
        }

        public ITypeSymbol GetTypeSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor)
        {
            var ret = TryGetSymbol(syntax, visitor);
            if (ret == null)
            {
                throw new InvalidOperationException($"Cannot find symbol for syntax");
            }
            return GetTypeSymbol(ret);
        }

        public ITypeSymbol? TryGetTypeSymbol(ISymbol symbol)
        {
            //ITypeSymbol TryUnwrapInlineArray(ITypeSymbol type)
            //{
            //    //if (IsInlineArray(type, out int sz, out var fieldType))
            //    //{
            //    //    return Compilation.CreateArrayTypeSymbol(fieldType, 1);
            //    //}
            //    return type;
            //}
            if (symbol.Kind == SymbolKind.DynamicType)
                return (ITypeSymbol)symbol;
            if ((symbol.Kind == SymbolKind.NamedType || symbol.Kind == SymbolKind.ArrayType || symbol.Kind == SymbolKind.PointerType || symbol.Kind == SymbolKind.FunctionPointerType) &&
                symbol is ITypeSymbol type)
            {
                return type;
            }
            if (symbol.Kind == SymbolKind.Property && symbol is IPropertySymbol property)
            {
                return property.Type;
            }
            if (symbol.Kind == SymbolKind.Field && symbol is IFieldSymbol field)
            {
                return field.Type;
            }
            if (symbol.Kind == SymbolKind.Local && symbol is ILocalSymbol local)
            {
                return local.Type;
            }
            if (symbol.Kind == SymbolKind.Parameter && symbol is IParameterSymbol parameter)
            {
                return parameter.Type;
            }
            if (symbol.Kind == SymbolKind.TypeParameter && symbol is ITypeParameterSymbol tparameter)
            {
                return tparameter;
            }
            if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol method)
            {
                if (method.Name == "op_Implicit")
                    return method.Parameters.First().Type;
                if (method.MethodKind == MethodKind.Constructor)
                    return method.ContainingType;
                return method.ReturnType;
            }
            if (symbol.Kind == SymbolKind.Discard && symbol is IDiscardSymbol discard)
            {
                return discard.Type;
            }
            if (symbol.Kind == SymbolKind.Event && symbol is IEventSymbol ev)
            {
                return ev.Type;
            }
            if (symbol.Kind == SymbolKind.RangeVariable && symbol is IRangeVariableSymbol rv)
            {
                // 1. Get the syntax node where the range variable is declared
                var syntaxRef = rv.DeclaringSyntaxReferences.FirstOrDefault();

                if (syntaxRef != null)
                {
                    var collectionNode = (syntaxRef.GetSyntax() as FromClauseSyntax)?.Expression;
                    if (collectionNode != null)
                    {
                        // 2. Query the semantic model for the type of that node
                        TypeInfo typeInfo = Compilation.GetSemanticModel(collectionNode.SyntaxTree).GetTypeInfo(collectionNode);
                        var typeSymbol = typeInfo.Type;
                        ITypeSymbol? elementType = null;
                        if (typeSymbol != null && (typeSymbol.IsEnumerable(out elementType) || typeSymbol.IsArray(out elementType)))
                        {
                            return elementType!;
                        }
                    }
                    // typeSymbol now holds the ITypeSymbol you need
                }
                //return rv.;
            }
            return null;
        }

        public ITypeSymbol GetTypeSymbol(ISymbol symbol)
        {
            return TryGetTypeSymbol(symbol) ?? throw new InvalidOperationException($"Cannot evaluate type from {symbol}");
        }

        Dictionary<string, Dictionary<string, Dictionary<object, object?>>>? _attachedAttributesCache;
        private Dictionary<string, Dictionary<string, Dictionary<object, object?>[]>>? _exactMatchCache;
        private WildcardPattern[]? _wildcardPatterns;

        // Inline struct definition optimized for zero heap-allocations during lookups
        private readonly struct WildcardPattern
        {
            public readonly string Prefix;
            public readonly string Suffix;
            public readonly Regex CompiledRegex;
            public readonly Dictionary<string, Dictionary<object, object?>[]> Values;

            public WildcardPattern(string rawPattern, Dictionary<string, Dictionary<object, object?>[]> values)
            {
                Values = values;
                // Strip out the leading '*'
                var pattern = rawPattern.Substring(1);

                // Extract plain text prefix/suffix before the first wildcard to skip Regex processing when possible
                int firstWildcard = pattern.IndexOfAny(['.', '+', '?', '*', '^', '$', '[', '(']);
                Prefix = firstWildcard > 0
                    ? pattern.Substring(0, firstWildcard).Replace("\\", "")
                    : string.Empty;

                int lastWildcard = pattern.LastIndexOfAny(['.', '+', '?', '*', '^', '$', ']', ')']);
                Suffix = (lastWildcard >= 0 && lastWildcard < pattern.Length - 1)
                    ? pattern.Substring(lastWildcard + 1).Replace("\\", "")
                    : string.Empty;

                // Pre-compile to native IL. CultureInvariant speeds up character parsing.
                CompiledRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            }
        }

        bool HasAttachedAttributes(string fullSymbolName, string attributeTypeName, out Dictionary<object, object?>[] constructorArgs)
        {
            //if (!ready)
            //{
            //    constructorArgs = null!;
            //    return false;
            //}

            if (_exactMatchCache == null)
            {
                lock (this)
                {
                    if (_exactMatchCache == null) // Double-check lock pattern
                    {
                        var attachedAttribute = GetSymbol(typeof(AttachedAttribute).FullName!, null);

                        var rawCache = Compilation.SourceModule.ReferencedAssemblySymbols.Concat([Compilation.Assembly])
                            .SelectMany(e => e.GetAttributes().Where(a => a.AttributeClass?.Equals(attachedAttribute, SymbolEqualityComparer.Default) ?? false))
                            .GroupBy(e =>
                            {
                                var type = e.ConstructorArguments[0].Value;
                                return type is ITypeSymbol ts
                                    ? ts.OriginalDefinition.CreateSignature(this, withGlobalNamespace: false)
                                    : (type?.ToString() ?? "");
                            }).ToDictionary(e => e.Key, e =>
                            {
                                return e.Select(a =>
                                {
                                    if (a.ConstructorArguments.Length < 2) return (null!, null!);
                                    var attType = (ITypeSymbol)a.ConstructorArguments[1].Value!;
                                    var attValues = a.ConstructorArguments.Length > 2
                                        ? a.ConstructorArguments[2].Values.Select((v, i) => ((object)i, v.Value)).ToDictionary(e => e.Item1, e => e.Value)
                                        : [];
                                    return (attType, attValues);
                                }).Where(a => a.attType != null)
                                .GroupBy(e => e.attType!, SymbolEqualityComparer.Default)
                                .ToDictionary(e => e.Key!.CreateSignature(this, withGlobalNamespace: false), e => e.Select(ee => ee.attValues!).ToArray());
                                //.ToDictionary(e => e.attType.CreateSignature(this, withGlobalNamespace: false), e => e.attValues);
                            })!;

                        var exact = new Dictionary<string, Dictionary<string, Dictionary<object, object?>[]>>(rawCache.Count, StringComparer.Ordinal);
                        var wildcards = new List<WildcardPattern>();

                        foreach (var kvp in rawCache)
                        {
                            if (kvp.Key.StartsWith("*"))
                            {
                                wildcards.Add(new WildcardPattern(kvp.Key, kvp.Value));
                            }
                            else
                            {
                                exact[kvp.Key] = kvp.Value;
                            }
                        }

                        _wildcardPatterns = [.. wildcards];
                        _exactMatchCache = exact;
                    }
                }
            }

            // Look up exact matches
            if (_exactMatchCache.TryGetValue(fullSymbolName, out var values) && values.TryGetValue(attributeTypeName, out constructorArgs))
            {
                return true;
            }

            // Look up non-generic matches
            var noGeneric = fullSymbolName.RemoveGenericParameterNames(out _);
            if (_exactMatchCache.TryGetValue(noGeneric, out values) && values.TryGetValue(attributeTypeName, out constructorArgs))
            {
                return true;
            }

            // --- WILDCARD EVALUATION PASS ---
            var patternSpan = _wildcardPatterns.AsSpan();
            for (int i = 0; i < patternSpan.Length; i++)
            {
                ref readonly var pattern = ref patternSpan[i];

                // Short circuit if plain-text prefix string doesn't match
                if (pattern.Prefix.Length > 0 && !fullSymbolName.StartsWith(pattern.Prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                // Short circuit if plain-text suffix string doesn't match
                if (pattern.Suffix.Length > 0 && !fullSymbolName.EndsWith(pattern.Suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                // Fast IL execution via pre-compiled engine instance
                if (pattern.CompiledRegex.IsMatch(fullSymbolName))
                {
                    if (pattern.Values.TryGetValue(attributeTypeName, out constructorArgs))
                    {
                        return true;
                    }
                }
            }

            constructorArgs = null!;
            return false;
        }


        //bool HasAttachedAttribute(string fullSymbolName, string attributeTypeName, out Dictionary<object, object?> constructorArgs)
        //{
        //    if (!ready)
        //    {
        //        constructorArgs = null!;
        //        return false;
        //    }
        //    if (_attachedAttributesCache == null)
        //    {
        //        lock (this)
        //        {
        //            var attachedAttribute = GetSymbol(typeof(AttachedAttribute).FullName!, null);
        //            _attachedAttributesCache = Compilation.SourceModule.ReferencedAssemblySymbols.Concat([Compilation.Assembly])
        //                .SelectMany(e => e.GetAttributes().Where(a => a.AttributeClass?.Equals(attachedAttribute, SymbolEqualityComparer.Default) ?? false))
        //                .GroupBy(e =>
        //                {
        //                    string typeName;
        //                    var type = e.ConstructorArguments[0].Value;
        //                    if (type is ITypeSymbol ts)
        //                    {
        //                        typeName = ts.CreateSignature(this, withGlobalNamespace: false);
        //                    }
        //                    else
        //                    {
        //                        typeName = type?.ToString() ?? "";
        //                    }
        //                    return typeName;
        //                }).ToDictionary(e => e.Key, e =>
        //                {
        //                    return e.Select(a =>
        //                    {
        //                        if (a.ConstructorArguments.Length < 2)
        //                            return (null!, null!);
        //                        var attType = (ITypeSymbol)a.ConstructorArguments[1].Value!;
        //                        var ctors = attType.GetMembers(".ctor");
        //                        var attValues = a.ConstructorArguments.Length > 2 ? a.ConstructorArguments[2].Values.Select((v, i) => ((object)i, v.Value)).ToDictionary(e => e.Item1, e => e.Value) : new Dictionary<object, object?>();
        //                        return (attType, attValues);
        //                    }).Where(a => a.attType != null)
        //                    .ToDictionary(e => e.attType.CreateSignature(this, withGlobalNamespace: false), e => e.attValues);
        //                })!;
        //        }
        //    }
        //    var noGeneric = fullSymbolName.RemoveGenericParameterNames(out _);
        //    if (_attachedAttributesCache.TryGetValue(fullSymbolName, out var values))
        //    {
        //        if (values.TryGetValue(attributeTypeName, out var args))
        //        {
        //            constructorArgs = args;
        //            return true;
        //        }
        //    }
        //    if (_attachedAttributesCache.TryGetValue(noGeneric, out values))
        //    {
        //        if (values.TryGetValue(attributeTypeName, out var args))
        //        {
        //            constructorArgs = args;
        //            return true;
        //        }
        //    }
        //    var patternValues = _attachedAttributesCache.FirstOrDefault(p =>
        //    {
        //        if (p.Key.StartsWith("*"))
        //        {
        //            //The regex.Match is too costly, use string search to do most of the matching that can simply be matched
        //            //Fallback to regex if all fails
        //            var pattern = p.Key.Substring(1);
        //            var countDotPlus = 0;
        //            int where = 0;
        //            while (true)
        //            {
        //                var dotPlus = pattern.IndexOf(".+", where);
        //                if (dotPlus < 0)
        //                    dotPlus = pattern.IndexOf(".?", where);
        //                if (dotPlus < 0)
        //                    break;
        //                countDotPlus++;
        //                where = dotPlus + 2;
        //            }
        //            //if (fullSymbolName.Contains("System.Reflection.Emit") && pattern.Contains("System\\.Reflection\\.Emit\\."))
        //            //{

        //            //}
        //            if (countDotPlus == 0)
        //                return pattern == fullSymbolName;
        //            if (countDotPlus >= 1)
        //            {
        //                var firstDotPlus = pattern.IndexOf(".+");
        //                if (firstDotPlus < 0)
        //                    firstDotPlus = pattern.IndexOf(".?");
        //                if (firstDotPlus >= 0)
        //                {
        //                    var fixedPattern = pattern.Substring(0, firstDotPlus).Replace("\\", "");
        //                    if (fixedPattern.Length > 0 && !fullSymbolName.StartsWith(fixedPattern))
        //                    {
        //                        return false;
        //                    }
        //                }
        //                var lastDotPlus = pattern.LastIndexOf(".+");
        //                if (lastDotPlus < 0)
        //                    lastDotPlus = pattern.LastIndexOf(".?");
        //                if (lastDotPlus >= 0)
        //                {
        //                    var fixedPattern = pattern.Substring(lastDotPlus + 2, pattern.Length - lastDotPlus - 2).Replace("\\", "");
        //                    if (fixedPattern.Length > 0 && !fullSymbolName.EndsWith(fixedPattern))
        //                    {
        //                        return false;
        //                    }
        //                }
        //                return true;
        //            }
        //            return Regex.IsMatch(fullSymbolName, pattern);
        //        }
        //        return false;
        //    }).Value;
        //    if (patternValues != null && patternValues.TryGetValue(attributeTypeName, out var args2))
        //    {
        //        constructorArgs = args2;
        //        return true;
        //    }
        //    constructorArgs = null!;
        //    return false;
        //}

        public bool HasAnyAttribute(ISymbol symbol, TranslatorSyntaxVisitor? visitor, bool inherits, params string[] attributeNames)
        {
            if (symbol is ITypeSymbol ts && ts.IsNullable(out var it))
                symbol = it!;
            if (attributeNames.Any(a => HasAttachedAttributes(symbol.CreateSignature(this, withGlobalNamespace: false), a, out _)))
                return true;
            //return symbol.HasAnyAttribute(inherits, attributeNames, out _);
            ////var symbols = attributeNames.Select(s => GetTypeSymbol(s, visitor)).ToList();
            if (symbol.GetAttributes().Select(a => a.AttributeClass).Where(e => e != null).Any(a =>
            {
                var aName = a!.CreateSignature(this, withGlobalNamespace: false)!;
                if (!aName.EndsWith("Attribute", StringComparison.Ordinal))
                    aName += "Attribute";
                return attributeNames.Contains(aName);
                //var aSymbol = TryGetTypeSymbol(aName, visitor);
                //if (aSymbol ==null)
                //    return false;
                //return symbols.Contains(aSymbol);
            }))
                return true;
            if (inherits && symbol.Kind == SymbolKind.NamedType && symbol is INamedTypeSymbol ns && ns.BaseType != null)
            {
                return HasAnyAttribute(ns.BaseType, visitor, inherits, attributeNames);
            }
            if (inherits && symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol ms && ms.OverriddenMethod != null)
            {
                return HasAnyAttribute(ms.OverriddenMethod, visitor, inherits, attributeNames);
            }
            if (inherits && symbol.Kind == SymbolKind.Property && symbol is IPropertySymbol ps && ps.OverriddenProperty != null)
            {
                return HasAnyAttribute(ps.OverriddenProperty, visitor, inherits, attributeNames);
            }
            if (inherits && symbol.Kind == SymbolKind.Event && symbol is IEventSymbol ev && ev.OverriddenEvent != null)
            {
                return HasAnyAttribute(ev.OverriddenEvent, visitor, inherits, attributeNames);
            }
            return false;
        }
        public bool HasAttributes(
            ISymbol symbol,
            string attributeName,
            TranslatorSyntaxVisitor? visitor,
            bool inherits,
            out Dictionary<object, object?>[] constructorArgs,
            bool checkAttached = true)
        {
            if (symbol.Kind == SymbolKind.Namespace ||
                (Constants.DisableBootClass && attributeName == typeof(BootAttribute).FullName))
            {
                constructorArgs = null!;
                return false;
            }

            // Ensure the attribute target has its suffix standardized once right here
            // This allows us to do exact string comparisons inside the tight loop
            if (!attributeName.EndsWith("Attribute", StringComparison.Ordinal))
            {
                attributeName += "Attribute";
            }

            if (checkAttached)
            {
                // Check attached cache attributes safely
                var symbolSignature = symbol.OriginalDefinition.CreateSignature(this, withTypeParameterNames: true, withGlobalNamespace: false);
                if (HasAttachedAttributes(symbolSignature, attributeName, out var args))
                {
                    constructorArgs = args;
                    return true;
                }
                symbolSignature = symbol.OriginalDefinition.CreateSignature(this, withTypeParameterNames: false, withGlobalNamespace: false);
                if (HasAttachedAttributes(symbolSignature, attributeName, out args))
                {
                    constructorArgs = args;
                    return true;
                }
            }
            var attrSymbol = TryGetSymbol(attributeName, visitor);
            if (attrSymbol == null)
            {
                constructorArgs = null!;
                return false;
            }

            List<Dictionary<object, object?>> cArgss = new();
            var attributes = symbol.GetAttributes();
            for (int i = 0; i < attributes.Length; i++)
            {
                var a = attributes[i];
                var attributeClass = a.AttributeClass;
                if (attributeClass == null) continue;

                // Perform fast reference equality first using Roslyn's comparer
                if (attrSymbol.Equals(attributeClass, SymbolEqualityComparer.Default))
                {
                    // Build the arguments dictionary strictly on-demand (Lazy initialization)
                    var cArgs = new Dictionary<object, object?>(a.ConstructorArguments.Length + a.NamedArguments.Length);

                    // Map positional constructor arguments cleanly
                    var positionalArgs = a.ConstructorArguments;
                    for (int j = 0; j < positionalArgs.Length; j++)
                    {
                        cArgs[j] = positionalArgs[j].Value;
                    }

                    // Map explicitly named arguments cleanly
                    var namedArgs = a.NamedArguments;
                    for (int j = 0; j < namedArgs.Length; j++)
                    {
                        var namedPair = namedArgs[j];
                        cArgs[namedPair.Key] = namedPair.Value.Value;
                    }

                    cArgss.Add(cArgs);
                    continue;
                }

                // Fallback string signature validation if reference matching isn't direct
                var aName = attributeClass.CreateSignature(this, withGlobalNamespace: false);
                if (aName != null)
                {
                    // Instead of allocating a concatenation string, check standard endings cleanly
                    bool isMatch = aName.Equals(attributeName, StringComparison.Ordinal) ||
                                  (!aName.EndsWith("Attribute", StringComparison.Ordinal) &&
                                   (aName.Length + 9 == attributeName.Length) &&
                                   attributeName.StartsWith(aName, StringComparison.Ordinal));

                    if (isMatch)
                    {
                        var cArgs = new Dictionary<object, object?>(a.ConstructorArguments.Length + a.NamedArguments.Length);

                        var positionalArgs = a.ConstructorArguments;
                        for (int j = 0; j < positionalArgs.Length; j++)
                        {
                            cArgs[j] = positionalArgs[j].Value;
                        }

                        var namedArgs = a.NamedArguments;
                        for (int j = 0; j < namedArgs.Length; j++)
                        {
                            var namedPair = namedArgs[j];
                            cArgs[namedPair.Key] = namedPair.Value.Value;
                        }

                        cArgss.Add(cArgs);
                    }
                }
            }
            if (cArgss.Count > 0)
            {
                constructorArgs = cArgss.ToArray();
                return true;
            }

            if (inherits)
            {
                switch (symbol)
                {
                    case ITypeSymbol ns when ns.BaseType != null:
                        return HasAttributes(ns.BaseType, attributeName, visitor, inherits, out constructorArgs);

                    case IMethodSymbol ms when ms.OverriddenMethod != null:
                        return HasAttributes(ms.OverriddenMethod, attributeName, visitor, inherits, out constructorArgs);

                    case IPropertySymbol ps when ps.OverriddenProperty != null:
                        return HasAttributes(ps.OverriddenProperty, attributeName, visitor, inherits, out constructorArgs);
                }
            }

            constructorArgs = null!;
            return false;
        }


        public bool HasAttribute(
            ISymbol symbol,
            string attributeName,
            TranslatorSyntaxVisitor? visitor,
            bool inherits,
            out Dictionary<object, object?> constructorArgs,
            bool checkAttached = true)
        {
            if (symbol.Kind == SymbolKind.Namespace ||
                (Constants.DisableBootClass && attributeName == typeof(BootAttribute).FullName))
            {
                constructorArgs = null!;
                return false;
            }

            // Ensure the attribute target has its suffix standardized once right here
            // This allows us to do exact string comparisons inside the tight loop
            if (!attributeName.EndsWith("Attribute", StringComparison.Ordinal))
            {
                attributeName += "Attribute";
            }

            if (checkAttached)
            {
                // Check attached cache attributes safely
                var symbolSignature = symbol.CreateSignature(this, withGlobalNamespace: false);
                if (HasAttachedAttributes(symbolSignature, attributeName, out var args))
                {
                    constructorArgs = args[0];
                    return true;
                }
            }
            var attrSymbol = TryGetSymbol(attributeName, visitor);
            if (attrSymbol == null)
            {
                constructorArgs = null!;
                return false;
            }

            var attributes = symbol.GetAttributes();
            for (int i = 0; i < attributes.Length; i++)
            {
                var a = attributes[i];
                var attributeClass = a.AttributeClass;
                if (attributeClass == null) continue;

                // Perform fast reference equality first using Roslyn's comparer
                if (attrSymbol.Equals(attributeClass, SymbolEqualityComparer.Default))
                {
                    // Build the arguments dictionary strictly on-demand (Lazy initialization)
                    var cArgs = new Dictionary<object, object?>(a.ConstructorArguments.Length + a.NamedArguments.Length);

                    // Map positional constructor arguments cleanly
                    var positionalArgs = a.ConstructorArguments;
                    for (int j = 0; j < positionalArgs.Length; j++)
                    {
                        cArgs[j] = positionalArgs[j].Value;
                    }

                    // Map explicitly named arguments cleanly
                    var namedArgs = a.NamedArguments;
                    for (int j = 0; j < namedArgs.Length; j++)
                    {
                        var namedPair = namedArgs[j];
                        cArgs[namedPair.Key] = namedPair.Value.Value;
                    }

                    constructorArgs = cArgs;
                    return true;
                }

                // Fallback string signature validation if reference matching isn't direct
                var aName = attributeClass.CreateSignature(this, withGlobalNamespace: false);
                if (aName != null)
                {
                    // Instead of allocating a concatenation string, check standard endings cleanly
                    bool isMatch = aName.Equals(attributeName, StringComparison.Ordinal) ||
                                  (!aName.EndsWith("Attribute", StringComparison.Ordinal) &&
                                   (aName.Length + 9 == attributeName.Length) &&
                                   attributeName.StartsWith(aName, StringComparison.Ordinal));

                    if (isMatch)
                    {
                        var cArgs = new Dictionary<object, object?>(a.ConstructorArguments.Length + a.NamedArguments.Length);

                        var positionalArgs = a.ConstructorArguments;
                        for (int j = 0; j < positionalArgs.Length; j++)
                        {
                            cArgs[j] = positionalArgs[j].Value;
                        }

                        var namedArgs = a.NamedArguments;
                        for (int j = 0; j < namedArgs.Length; j++)
                        {
                            var namedPair = namedArgs[j];
                            cArgs[namedPair.Key] = namedPair.Value.Value;
                        }

                        constructorArgs = cArgs;
                        return true;
                    }
                }
            }

            if (inherits)
            {
                switch (symbol)
                {
                    case ITypeSymbol ns when ns.BaseType != null:
                        return HasAttribute(ns.BaseType, attributeName, visitor, inherits, out constructorArgs);

                    case IMethodSymbol ms when ms.OverriddenMethod != null:
                        return HasAttribute(ms.OverriddenMethod, attributeName, visitor, inherits, out constructorArgs);

                    case IPropertySymbol ps when ps.OverriddenProperty != null:
                        return HasAttribute(ps.OverriddenProperty, attributeName, visitor, inherits, out constructorArgs);
                }
            }

            constructorArgs = null!;
            return false;
        }

        //public bool HasAttribute(ISymbol symbol, string attributeName, TranslatorSyntaxVisitor? visitor, bool inherits, out Dictionary<object, object?> constructorArgs)
        //{
        //    if (symbol.Kind == SymbolKind.Namespace ||
        //        (Constants.DisableBootClass && attributeName == typeof(BootAttribute).FullName))
        //    {
        //        constructorArgs = null;
        //        return false;
        //    }
        //    //constructorArgs = null;
        //    //if (attributeName == typeof(BootAttribute).FullName)
        //    //return false;
        //    if (HasAttachedAttribute(symbol.CreateSignature(this, withGlobalNamespace: false), attributeName, out var args))
        //    {
        //        constructorArgs = args;
        //        return true;
        //    }
        //    constructorArgs = null!;
        //    Dictionary<object, object?> cArgs = new Dictionary<object, object?>();
        //    var attrSymbol = TryGetSymbol(attributeName, visitor);
        //    if (attrSymbol == null)
        //        return false;
        //    if (symbol.GetAttributes().Select(a => (a, a.AttributeClass)).Where(e => e.AttributeClass != null).Any(a =>
        //    {
        //        var aName = a.AttributeClass!.CreateSignature(this, withGlobalNamespace: false)!;
        //        if (!aName.EndsWith("Attribute"))
        //            aName += "Attribute";
        //        if (aName != attributeName)
        //            return false;
        //        var aSymbol = a.AttributeClass;// GetTypeSymbol(aName, visitor);
        //        if (attrSymbol.Equals(aSymbol, SymbolEqualityComparer.Default))
        //        {
        //            //cArgs = a.a.ConstructorArguments.Select(c => c.Kind == TypedConstantKind.Array ? c.Values : c.Value!).ToArray();
        //            cArgs = a.a.ConstructorArguments.Select((c, i) => ((object)i, c.Value))
        //            .Concat(a.a.NamedArguments.Select(a => ((object)a.Key, a.Value.Value)))
        //            .ToDictionary(kv => kv.Item1, kv => kv.Value);
        //            return true;
        //        }
        //        return false;
        //    }))
        //    {
        //        constructorArgs = cArgs;
        //        return true;
        //    }
        //    if (inherits && symbol.Kind == SymbolKind.NamedType && symbol is ITypeSymbol ns && ns.BaseType != null)
        //    {
        //        return HasAttribute(ns.BaseType, attributeName, visitor, inherits, out constructorArgs);
        //    }
        //    if (inherits && symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol ms && ms.OverriddenMethod != null)
        //    {
        //        return HasAttribute(ms.OverriddenMethod, attributeName, visitor, inherits, out constructorArgs);
        //    }
        //    if (inherits && symbol.Kind == SymbolKind.Property && symbol is IPropertySymbol ps && ps.OverriddenProperty != null)
        //    {
        //        return HasAttribute(ps.OverriddenProperty, attributeName, visitor, inherits, out constructorArgs);
        //    }
        //    return false;
        //}

        //ConcurrentDictionary<(ISymbol, Type, string), object> _cacheSymbolValue = new();
        //T CacheSymbolValue<T>(ISymbol symbol, Func<(ISymbol Symbol, Type Type, string), T> create, [CallerMemberName] string caller = "")
        //{
        //    return create((symbol, typeof(T), caller));
        //    //return (T)_cacheSymbolValue.GetOrAdd((symbol, typeof(T), caller), create);
        //}

        public bool IsExtern(ISymbol type)
        {
            //return CacheSymbolValue<bool>(type, (state) =>
            //{
            if (type.IsExtern || HasAnyAttribute(type, null, false, typeof(ExternalAttribute).FullName!, typeof(NonScriptableAttribute).FullName!))
                return true;
            if (type.ContainingType != null)
            {
                return IsExtern(type.ContainingType);
            }
            return false;
            //});
        }

        //public bool ShouldExportType(MemberDeclarationSyntax node, TranslatorSyntaxVisitor? visitor)
        //{
        //    return !HasAnyAttribute(node, visitor, typeof(ExternalAttribute).FullName, typeof(ExternalInterfaceAttribute).FullName, typeof(NonScriptableAttribute).FullName, typeof(ObjectLiteralAttribute).FullName);
        //}

        public bool IsBootClass(ITypeSymbol type)
        {
            //return CacheSymbolValue<bool>(type, (state) =>
            //{
            bool isBootClass = HasAttribute(type, typeof(BootAttribute).FullName!, null, false, out _);
            var parentIsBootClass = type.ContainingType != null ? HasAttribute(type.ContainingType, typeof(BootAttribute).FullName!, null, false, out _) : false;
            if (isBootClass || parentIsBootClass)
            {
                if (type.BaseType != null)
                {
                    //var systemObject = GetSymbol("System.Object", null);
                    //var systemValueType = GetSymbol("System.ValueType", null);
                    if (type.BaseType.Equals(SystemObject, SymbolEqualityComparer.Default) ||
                        type.BaseType.Equals(SystemValueType, SymbolEqualityComparer.Default) ||
                        IsBootClass(type.BaseType))
                    {
                        return true;
                    }
                    else
                        return false;
                }
                return true;
            }
            return false;
            //});
        }

        public bool IsInlineArray(ISymbol type, out int size, out ITypeSymbol fieldType)
        {
            size = 0;
            fieldType = null!;
            bool isInlineArray = HasAttribute(type, "System.Runtime.CompilerServices.InlineArrayAttribute", null, false, out var args);
            //bool isInlineArray = HasAttribute(type, typeof(InlineArrayAttribute).FullName, null, false, out var args);
            if (isInlineArray)
            {
                size = (int)args![0]!;
                var field = (IFieldSymbol?)((ITypeSymbol)type).GetMembers().FirstOrDefault();
                if (field != null)
                {
                    fieldType = field.Type;
                }
                return true;
            }
            return false;
        }

        public bool IsFixedSizeField(ISymbol field, out int size, out ITypeSymbol fieldElementType)
        {
            size = 0;
            fieldElementType = null!;
            if (field.Kind == SymbolKind.Field)
            {
                var f = (IFieldSymbol)field;
                if (f.IsFixedSizeBuffer)
                {
                    size = f.FixedSize;
                    f.Type.IsPointer(out fieldElementType);
                    return true;
                }
            }
            return false;
        }

        public bool IsAnonymousType(ISymbol symbol)
        {
            //return CacheSymbolValue<bool>(symbol, (state) =>
            //{
            if (symbol.Kind == SymbolKind.NamedType &&
            symbol is INamedTypeSymbol namedType &&
            namedType.TypeKind == TypeKind.Class &&
            namedType.IsAnonymousType) // Or check name for compiler-generated patterns
            {
                return true;
            }
            return false;
            //});
        }

        public bool IsAnonymousFunction(ISymbol symbol)
        {
            //return CacheSymbolValue(symbol, (kc) =>
            //{
            if (symbol.Kind == SymbolKind.NamedType &&
                symbol is INamedTypeSymbol namedType &&
                namedType.TypeKind == TypeKind.Delegate &&
                namedType.IsAnonymousType) // Or check name for compiler-generated patterns
            {
                return true;
            }
            return false;
            //});
        }

        public bool IsNativeFunction(ISymbol symbol)
        {
            //return CacheSymbolValue<bool>(symbol, (state) =>
            //{
            if (IsAnonymousFunction(symbol))
                return true;
            if (HasAttribute(symbol, typeof(NativeDelegateAttribute).FullName!, null, false, out _))
                return true;
            return false;
            //});
        }

        public bool IsExternal(ISymbol symbol, TranslatorSyntaxVisitor? visitor)
        {
            //return CacheSymbolValue<bool>(symbol, (state) =>
            //{
            if (symbol.IsExtern)
                return true;
            if (HasAnyAttribute(symbol, visitor, false, typeof(ExternalAttribute).FullName!))
                return true;
            if (symbol.ContainingType != null)
            {
                return IsExternal(symbol.ContainingType, visitor);
            }
            return false;
            //});
        }

        public bool ShouldExportType(ISymbol symbol, TranslatorSyntaxVisitor? visitor)
        {
            //return CacheSymbolValue<bool>(symbol, (state) =>
            //{
            if (symbol.IsExtern)
                return false;
            //if (IsInlineArray(symbol, out _, out _))  //replace by array type at runtime
            //    return false;
            if (HasAnyAttribute(symbol, visitor, false, typeof(ExternalAttribute).FullName!, typeof(NonScriptableAttribute).FullName!, typeof(ObjectLiteralAttribute).FullName!))
                return false;
            if (symbol.ContainingType != null)
            {
                return ShouldExportType(symbol.ContainingType, visitor);
            }
            return true;
            //});
        }

        public bool IsReflectable(ISymbol symbol, TranslatorSyntaxVisitor? visitor, bool skipAssemblyCheck = false)
        {
            var has = HasAttribute(symbol, typeof(ReflectableAttribute).FullName!, visitor, false, out var args);
            if (has)
            {
                if (args == null || args.Count == 0)
                    return true;
                if (args[0] is bool b)
                    return b;
                if (args[0] is string s)
                    return s == "true";
            }
            if (!skipAssemblyCheck)
            {
                has = HasAttribute(symbol.ContainingAssembly, typeof(ReflectableAttribute).FullName!, visitor, false, out args);
                if (has)
                {
                    if (args == null || args.Count == 0)
                        return true;
                    if (args[0] is bool b)
                        return b;
                    if (args[0] is string s)
                        return s == "true";
                }
            }
            if (symbol.ContainingType != null)
                return IsReflectable(symbol.ContainingType, visitor, skipAssemblyCheck: true);
            return true;
        }

        public string? GetDefaultValue(TypeSyntax type, TranslatorSyntaxVisitor? visitor, bool createValueInstance = false)
        {
            if (type.IsKind(SyntaxKind.NullableType))
                return "null";
            if (type is PredefinedTypeSyntax id)
            {
                switch (id.Keyword.ValueText)
                {
                    case "byte":
                    case "sbyte":
                    case "uint":
                    case "int":
                    case "nint":
                    case "nuint":
                    case "ushort":
                    case "short":
                    case "double":
                    case "float":
                    case "char":
                        return "0";
                    case "long":
                    case "ulong":
                        return "0n";
                    case "bool":
                        return "false";
                    case "object":
                    case "string":
                        return "null";
                }
            }

            if (type.IsKind(SyntaxKind.RefType)/* type is RefTypeSyntax*/)
            {
                return null;
            }
            var sym = GetTypeSymbol(type, visitor);
            if (sym != null)
                return GetDefaultValue(sym, createValueInstance);
            return "null";
        }

        public string? GetDefaultValue(ITypeSymbol type, bool createValueInstance = false)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return "false";
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                case SpecialType.System_Enum:
                    return "0";
                case SpecialType.System_UInt64:
                case SpecialType.System_Int64:
                    return "0n";
                case SpecialType.System_Nullable_T:
                    return "null";
                case SpecialType.System_ValueType:
                    if (createValueInstance)
                        return $"new {type.ComputeOutputTypeName(this)}()";
                    return null;
            }
            if (type.BaseType?.SpecialType == SpecialType.System_Enum)
            {
                if (((INamedTypeSymbol)type).EnumUnderlyingType!.IsLongNumericType())
                    return "0n";
                return "0";
            }
            //if (IsInlineArray(type, out var inlineSize, out var fieldType))
            //{
            //    return $"/*[InlineArray({inlineSize})] {type}*/ {GlobalName}.{Constants.CreateArray}({fieldType.ComputeOutputTypeName(this)}, {inlineSize})";
            //}
            if (type.Kind == SymbolKind.TypeParameter)
            {
                return $"{GlobalName}.{Constants.DefaultTypeName}({type.ComputeOutputTypeName(this)})";
            }
            if (type.IsNullable(out _))
            {
                return "null";
            }
            if (type.IsValueType)
            {
                if (createValueInstance)
                {
                    var tt = type as INamedTypeSymbol;
                    bool hasArity = false;
                    while (!hasArity && tt != null)
                    {
                        if (tt.Arity > 0)
                        {
                            hasArity = true;
                            break;
                        }
                        tt = tt.ContainingType;
                    }
                    return $"new {(hasArity ? "(" : "")}{type.ComputeOutputTypeName(this)}{(hasArity ? ")" : "")}()";
                }
                return null;
            }
            return "null";
        }
        //public string ResolveFullTypeName(ISymbol type)
        //{
        //    return AllSymbols[type].FullName;
        //    var ret = type.Name;
        //    ISymbol current = type.ContainingType ?? (ISymbol)type.ContainingNamespace;
        //    while (!string.IsNullOrEmpty(current?.Name))
        //    {
        //        ret = current.Name + "." + ret;
        //        current = current.ContainingType ?? (ISymbol)current.ContainingNamespace;
        //    }
        //    return Utilities.ResolvePredefinedTypeName(ret);
        //}

        public string ResolveInterpolatedExpression(InterpolatedStringExpressionSyntax expression, TranslatorSyntaxVisitor visitor)
        {
            string result = "";
            foreach (var content in expression.Contents)
            {
                if (content.IsKind(SyntaxKind.InterpolatedStringText) && content is InterpolatedStringTextSyntax sy)
                {
                    result += sy.GetText().ToString().Replace("{{", "{").Replace("}}", "}");
                }
                else if (content.IsKind(SyntaxKind.Interpolation) && content is InterpolationSyntax ip)
                {
                    var boundMember = visitor.GetExpressionBoundTarget(ip.Expression).TypeSyntaxOrSymbol;
                    if (boundMember == null && ip.Expression.IsKind(SyntaxKind.InvocationExpression))
                    {
                        var invoke = (InvocationExpressionSyntax)ip.Expression;
                        if (invoke.Expression.IsKind(SyntaxKind.IdentifierName))
                        {
                            var id = (IdentifierNameSyntax)invoke.Expression;
                            if (id.Identifier.ValueText == "nameof")
                            {
                                var arg = invoke.ArgumentList.Arguments[0].Expression;
                                var idd = arg.ToString().Split('.').Last();
                                result += idd;
                                continue;
                            }
                        }

                    }
                    if (boundMember == null)
                    {
                        throw new InvalidOperationException($"Cannot resolve {ip.Expression} in interpolated expression {expression}.");
                    }
                    if (boundMember is IFieldSymbol fs && fs.IsConst)
                    {
                        result += fs.ConstantValue?.ToString();
                    }
                    else
                        throw new InvalidOperationException($"Only const members can be used in this interpolated expression. Error resolving {ip.Expression} in {expression}!");
                }
                else
                {
                    throw new InvalidOperationException($"The value of {content} cannot be resolved at compile time in {expression}.");
                }
            }
            return result;
        }

        public string? Evaluate(string keyword, TranslatorSyntaxVisitor? visitor)
        {
            var value = Project.Evaluate(keyword).LastOrDefault();
            if (string.IsNullOrEmpty(value) && visitor != null && visitor.Pragmas.TryGetValue(keyword, out var n))
            {
                return n.ToString();
            }
            return value;
        }

        ConcurrentDictionary<string, string> assemblyGlobalNamespaceCache = new ConcurrentDictionary<string, string>();

        public string GetAssemblyGlobalSlug(IAssemblySymbol assembly)
        {
            if (assembly.Name.EndsWith("NetJs.System.Private.CoreLib"))
                return Constants.SystemPrivateCoreLibSlug;
            if (assemblyGlobalNamespaceCache.TryGetValue(assembly.Name, out var slug))
                return slug;
            if (HasAttribute(assembly, typeof(AssemblySlugAttribute).FullName!, null, false, out var args, checkAttached: false))
            {
                slug = (string)args![0]!;
                if (!slug.StartsWith("$"))
                    slug = "$" + slug;
            }
            else
            {
                if (assembly.Name.StartsWith($"{Constants.ProjectName}.System.") ||
                    assembly.Name.StartsWith($"{Constants.ProjectName}.Microsoft.") ||
                    assembly.Name.StartsWith($"System.") ||
                    assembly.Name.StartsWith($"Microsoft."))
                {
                    var name = assembly.Name;
                    if (name.StartsWith($"{Constants.ProjectName}."))
                        name = name.Substring(6);
                    slug = "$" + string.Join("", name.Split('.').Select(c => char.ToLower(c[0])));
                }
                else
                {
                    slug = "$" + assembly.Name.GenerateShortId();
                }
            }
            if (assemblyGlobalNamespaceCache.Values.Contains(slug))
            {
                throw new InvalidOperationException($"Global slug name {slug} for {assembly} clashes with an existing slug");
            }
            assemblyGlobalNamespaceCache[assembly.Name] = slug;
            return slug;
        }

        public int? SizeOf(ITypeSymbol type)
        {
            //return CacheSymbolValue(type, (state) =>
            //{
            if (type.TypeKind == TypeKind.TypeParameter)
                return null;
            if (type.IsStatic)
                return null;
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    return 1;
                case SpecialType.System_SByte:
                    return 1;
                case SpecialType.System_Int16:
                    return 2;
                case SpecialType.System_Int32:
                    return 4;
                case SpecialType.System_IntPtr:
                    return 4;
                case SpecialType.System_UIntPtr:
                    return 4;
                case SpecialType.System_Byte:
                    return 1;
                case SpecialType.System_UInt16:
                    return 2;
                case SpecialType.System_UInt32:
                    return 4;
                case SpecialType.System_Single:
                    return 4;
                case SpecialType.System_Double:
                    return 8;
                case SpecialType.System_Char:
                    return 2;
                case SpecialType.System_Int64:
                    return 8;
                case SpecialType.System_UInt64:
                    return 8;
                    //case SpecialType.System_Enum:
                    //return SizeOf(type.BaseType);
            }
            if (type.TypeKind == TypeKind.Enum)
            {
                return SizeOf(((INamedTypeSymbol)type).EnumUnderlyingType!);
            }
            if (!type.IsValueType || type.IsPointer(out _))
            {
                return 4;
            }
            if (type.IsValueType)
            {
                if (IsInlineArray(type, out var iSize, out var fieldType))
                {
                    var fieldSize = SizeOf(fieldType);
                    return iSize * fieldSize;
                }
                if (HasAttribute(type, typeof(StructLayoutAttribute).FullName!, null, false, out var structLayoutAttribute))
                {
                    //var layoutKind = (LayoutKind)(int)structLayoutAttribute![0];
                    var size = structLayoutAttribute.GetValueOrDefault("Size");
                    if (size != null)
                        return (int)size;
                }
                var layout = StructLayout(type);
                if (layout.Count == 0)
                    return 0;
                //W'd rather do sum of size, but sum of size will not work if there are holes in FieldOffset definition
                //return layout.Sum(kv => kv.Value.Size);
                var maxOffset = layout.MaxBy(e => e.Value.Offset);
                return maxOffset.Value.Offset + maxOffset.Value.Size;
            }
            //if (type.TypeKind == TypeKind.Enum && type.BaseType != null)
            //{
            //    return SizeOf(type.BaseType);
            //}
            return null;
            //});
        }

        public bool IsDefinedTypeParameter(INamedTypeSymbol type)
        {
            //return CacheSymbolValue<bool>(type, (state) =>
            //{
            if (type.IsSealed &&
            SymbolEqualityComparer.Default.Equals(type.ContainingNamespace, Compilation.GlobalNamespace) &&
            type.Name.StartsWith("T") &&
            HasAttribute(type, typeof(BootAttribute).FullName!, null, false, out _))
            {
                return true;
            }
            return false;
            //});
        }

        ConcurrentDictionary<ISymbol, Dictionary<ISymbol, (int Offset, int Size)>> _staticStructLayoutCache = new ConcurrentDictionary<ISymbol, Dictionary<ISymbol, (int Offset, int Size)>>(SymbolEqualityComparer.Default);
        ConcurrentDictionary<ISymbol, Dictionary<ISymbol, (int Offset, int Size)>> _instanceStructLayoutCache = new ConcurrentDictionary<ISymbol, Dictionary<ISymbol, (int Offset, int Size)>>(SymbolEqualityComparer.Default);
        public Dictionary<ISymbol, (int Offset, int Size)> StructLayout(ITypeSymbol type, bool isStatic = false)
        {
            return (isStatic ? _staticStructLayoutCache : _instanceStructLayoutCache).GetOrAdd(type, (__) =>
            {
                int nextOffset = 0;
                return type.GetMembers().Where(m => m.IsStatic == isStatic).Where(m =>
                    (m.Kind == SymbolKind.Field && ((IFieldSymbol)m).AssociatedSymbol == null && !((IFieldSymbol)m).IsConst) ||
                    (m.Kind == SymbolKind.Event) ||
                    (m.Kind == SymbolKind.Property && ((IPropertySymbol)m).IsAutoProperty()))
                    .Select(field =>
                    {
                        if (HasAttribute(field, typeof(FieldOffsetAttribute).FullName!, null, false, out var fieldOffsetAttribute))
                        {
                            //var arg = fieldOffsetAttribute.Single().Value;
                            var offsetArg = fieldOffsetAttribute[0];
                            return (field, (int)fieldOffsetAttribute[0]!);
                        }
                        if (field.Kind == SymbolKind.Field)
                            return (field, int.MaxValue / 2);
                        return (field: field, int.MaxValue);
                    }).OrderBy(o => o.Item2)
                    .Select(e =>
                    {
                        var type = (e.field as IFieldSymbol)?.Type ??
                        (e.field as IEventSymbol)?.Type ??
                        (e.field as IPropertySymbol)?.Type ??
                        throw new InvalidOperationException();
                        if (e.Item2 < int.MaxValue / 2)
                        {
                            var size = SizeOf(type) ?? 1;
                            if (size == 0)
                            {
                                size = 1;
                                //throw new InvalidOperationException($"SizeOf {type} returns 0");
                            }
                            nextOffset = e.Item2 + size;
                            return (e.field, e.Item2, size);
                        }
                        else
                        {
                            if (IsFixedSizeField(e.field, out var iSize, out var fieldType))
                            {
                                var selfOffset = nextOffset;
                                var itemSize = SizeOf(fieldType) ?? 1;
                                nextOffset += iSize * itemSize;
                                return (e.field, selfOffset, iSize * itemSize);
                            }
                            if (IsInlineArray(type!, out iSize, out fieldType))
                            {
                                var selfOffset = nextOffset;
                                var itemSize = SizeOf(fieldType) ?? 1;
                                nextOffset += iSize * itemSize;
                                return (e.field, selfOffset, iSize * itemSize);
                            }
                            else
                            {
                                var selfOffset = nextOffset;
                                var size = SizeOf(type) ?? 1;
                                if (size == 0)
                                {
                                    size = 1;
                                    //throw new InvalidOperationException($"SizeOf {type} returns 0");
                                }
                                nextOffset += size;
                                return (e.field, selfOffset, size);
                                //lastOffset += _global.Get(type!, this);
                            }
                        }
                    })
                    .ToDictionary(kv => kv.field, kv => (kv.Item2, kv.Item3), SymbolEqualityComparer.Default);
            });
        }

        public TypeKindModel MapTypeKind(Microsoft.CodeAnalysis.TypeKind roslynKind) =>
             roslynKind switch
             {
                 Microsoft.CodeAnalysis.TypeKind.Class => TypeKindModel.Class,
                 Microsoft.CodeAnalysis.TypeKind.Struct => TypeKindModel.Struct,
                 Microsoft.CodeAnalysis.TypeKind.Interface => TypeKindModel.Interface,
                 Microsoft.CodeAnalysis.TypeKind.Enum => TypeKindModel.Enum,
                 Microsoft.CodeAnalysis.TypeKind.Delegate => TypeKindModel.Delegate,
                 Microsoft.CodeAnalysis.TypeKind.Array => TypeKindModel.Array,
                 Microsoft.CodeAnalysis.TypeKind.Pointer => TypeKindModel.Pointer,
                 _ => TypeKindModel.Unknown
             };

        public KnownTypeHandle KnownTypeFromName(string t)
        {
            return t == "System.Void" ? KnownTypeHandle.SystemVoid :
                    t == "System.Object" ? KnownTypeHandle.SystemObject :
                    t == "System.ValueType" ? KnownTypeHandle.SystemValueType :
                    t == "System.Boolean" ? KnownTypeHandle.SystemBool :
                    t == "System.SByte" ? KnownTypeHandle.SystemSByte :
                    t == "System.Byte" ? KnownTypeHandle.SystemByte :
                    t == "System.Char" ? KnownTypeHandle.SystemChar :
                    t == "System.Int16" ? KnownTypeHandle.SystemInt16 :
                    t == "System.UInt16" ? KnownTypeHandle.SystemUInt16 :
                    t == "System.Int32" ? KnownTypeHandle.SystemInt32 :
                    t == "System.UInt32" ? KnownTypeHandle.SystemUInt32 :
                    t == "System.IntPtr" ? KnownTypeHandle.SystemIntPtr :
                    t == "System.UIntPtr" ? KnownTypeHandle.SystemUIntPtr :
                    t == "System.Int64" ? KnownTypeHandle.SystemInt64 :
                    t == "System.UInt64" ? KnownTypeHandle.SystemUInt64 :
                    t == "System.Single" ? KnownTypeHandle.SystemSingle :
                    t == "System.Double" ? KnownTypeHandle.SystemDouble :
                    t == "System.String" ? KnownTypeHandle.SystemString :
                    t == "System.Enum" ? KnownTypeHandle.SystemEnum :
                    t == "System.Array" ? KnownTypeHandle.SystemArray :
                    t == "System.Nullable<>" ? KnownTypeHandle.SystemNullable :
                    t == "NetJs.Pointer<>" ? KnownTypeHandle.SystemPointer :
                    t == "NetJs.Ref<>" ? KnownTypeHandle.SystemReference :
                    t == "AnonymousType<>" ? KnownTypeHandle.AnonymousType :
                    KnownTypeHandle.SystemDynamic;
        }

        public KnownTypeHandle KnownTypeFrom(ITypeSymbol t)
        {
            return KnownTypeFromName(t.CreateSignature(this, withGlobalNamespace: false));
        }

        /// <summary>
        /// A struct that is simply numeric fields or other pure struct, no reference type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsPureStructType(ITypeSymbol type)
        {
            //return CacheSymbolValue<bool>(type, (state) =>
            //{
            if (type.TypeKind == TypeKind.Struct && !type.IsJsPrimitive() && !IsBootClass(type))
            {
                var candidates = type.GetMembers().Where(m => !m.IsStatic && m.Kind == SymbolKind.Field);
                if (!candidates.Any())
                    return false;
                if (candidates.All(field =>
                {
                    if (field.GetRefKind() != RefKind.None)
                    {
                        return false;
                    }
                    var fieldType = GetTypeSymbol(field);
                    return fieldType.IsNumberNumericType() ||
                    fieldType.IsLongNumericType() ||
                    fieldType.SpecialType == SpecialType.System_Boolean ||
                    fieldType.TypeKind == TypeKind.Enum ||
                    (IsInlineArray(fieldType, out _, out var element) && (element.IsNumericType() || element.TypeKind == TypeKind.Enum || element.SpecialType == SpecialType.System_Boolean)) ||
                    (IsFixedSizeField(field, out _, out var element2) && (element2.IsNumericType() || element2.TypeKind == TypeKind.Enum || element2.SpecialType == SpecialType.System_Boolean)) ||
                    IsPureStructType(fieldType);
                }))
                {
                    return true;
                }
            }
            return false;
            //});
        }

        public TypeFlagsModel GetTypeFlags(ITypeSymbol type)
        {
            var flags = TypeFlagsModel.None;

            if (type.DeclaredAccessibility == Accessibility.Public)
                flags |= TypeFlagsModel.IsPublic;
            if (type.IsAbstract)
                flags |= TypeFlagsModel.IsAbstract;
            if (type.IsSealed)
                flags |= TypeFlagsModel.IsSealed;
            if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Interface)
                flags |= TypeFlagsModel.IsInterface;
            if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
                flags |= TypeFlagsModel.IsEnum;
            if (type.IsValueType)
            {
                flags |= TypeFlagsModel.IsValueType;
                if (IsPureStructType(type))
                {
                    flags |= TypeFlagsModel.IsPureStruct;
                }
            }
            if (type is INamedTypeSymbol named && named.IsGenericType)
            {
                if (!named.IsType("System.Array<>", true)) //this is not to be interpreted as a generic type, we use it to type T[]
                    flags |= TypeFlagsModel.IsGenericType;
            }
            if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Class)
                flags |= TypeFlagsModel.IsClass;
            if (type.BaseType?.Name == "Enum" && type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
                flags |= TypeFlagsModel.IsFlags;
            if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array || type.IsType("System.Array") || type.IsType("System.Array<>", true))
                flags |= TypeFlagsModel.IsArray;
            if (type.ContainingSymbol.Kind == Microsoft.CodeAnalysis.SymbolKind.NamedType)
                flags |= TypeFlagsModel.IsNested;
            if (type.IsPointer(out _))
                flags |= TypeFlagsModel.IsPointer;
            if (type.IsJsPrimitive())
                flags |= TypeFlagsModel.IsPrimitive;
            if (HasAttribute(type, typeof(StructLayoutAttribute).FullName!, null, false, out var structLayoutAttribute))
            {
                flags |= TypeFlagsModel.IsStructLayout;
            }
            if (IsInlineArray(type, out _, out _))
                flags |= TypeFlagsModel.IsInlineArray;
            return flags;
        }

        //public bool IsInlineArray(ISymbol type, out int size, out ITypeSymbol fieldType)
        //{
        //    size = 0;
        //    fieldType = null!;
        //    bool isInlineArray = HasAnyAttribute(type, false, ["System.Runtime.CompilerServices.InlineArrayAttribute"], out var args);
        //    //bool isInlineArray = HasAttribute(type, typeof(InlineArrayAttribute).FullName, null, false, out var args);
        //    if (isInlineArray)
        //    {
        //        size = (int)args.Single().Value[0]!;
        //        var field = (IFieldSymbol?)((ITypeSymbol)type).GetMembers().FirstOrDefault();
        //        if (field != null)
        //        {
        //            fieldType = field.Type;
        //        }
        //        return true;
        //    }
        //    return false;
        //}

        IMethodSymbol? _main;
        public IMethodSymbol? MainEntry
        {
            get
            {
                if (_main != null)
                    return _main;
                var main = Compilation.GetEntryPoint(CancellationToken.None);
                if (main == null)
                {
                    var ns = Project.Evaluate("RootNamespace").LastOrDefault();
                    main = (IMethodSymbol?)TryGetSymbol($"{ns}.Program.Main(System.String[])", null);
                    main ??= (IMethodSymbol?)TryGetSymbol($"{ns}.Program.Main()", null);
                }
                return _main = main;
            }
        }
    }
}