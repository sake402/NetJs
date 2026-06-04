using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.OneOf.Types;
using Newtonsoft.Json.Linq;
using NuGet.ContentModel;
using NuGet.Packaging;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks.Sources;
using System.Xml.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace NetJs.Translator.CSharpToJavascript
{

    public partial record class GlobalCompilationVisitor
    {
        public CSharpCompilation Compilation { get; internal set; }
        public ConcurrentDictionary<ISymbol, (string WithTypeParameter, string WithoutTypeParameter)> FullTypeNameCache { get; } = new(SymbolEqualityComparer.Default);
        public IProject Project { get; }
        public List<string> ProcessedTypeNodes { get; } = new List<string>();
        public Dictionary<SyntaxTree, TranslatorSyntaxVisitor> Visitors { get; } = new Dictionary<SyntaxTree, TranslatorSyntaxVisitor>();
        public Dictionary<INamedTypeSymbol, TranslatorSyntaxVisitor> TypeVisitors { get; } = new Dictionary<INamedTypeSymbol, TranslatorSyntaxVisitor>(SymbolEqualityComparer.Default);
        public Dictionary<INamedTypeSymbol, ScriptWriter> TypeWriters { get; } = new Dictionary<INamedTypeSymbol, ScriptWriter>(SymbolEqualityComparer.Default);
        public SymbolDescriptor Symbols { get; private set; } = new();
        public SymbolDescriptor ImportedNames { get; }

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
        public Dictionary<string, IEnumerable<ITypeSymbol>> Delegates { get; private set; }
        public OutputMode OutputMode { get; }
        public string GlobalName { get; }
        public ReflectionMetadataBuilder Reflection { get; set; } = default!;


        public List<string> ModuleInitializers = new();
        public void RegisterModuleInitializer(string methodCall)
        {
            ModuleInitializers.Add(methodCall);
        }

        public SymbolMetadata? GetMetadata(ISymbol symbol)
        {
            //Detect primary constructor parameter
            if (symbol.Kind == SymbolKind.Parameter && symbol.ContainingSymbol is IMethodSymbol mmethod && mmethod.IsPrimaryConstructor(this))
            {
                return new SymbolMetadata(this)
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
            if (metadata == null && symbol is IPropertySymbol property && property.PartialDefinitionPart != null)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(property.PartialDefinitionPart.OriginalDefinition);
                if (metadata != null)
                    return metadata;
            }
            if (metadata == null && symbol is IMethodSymbol method && method.PartialDefinitionPart != null)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(method.PartialDefinitionPart.OriginalDefinition);
                if (metadata != null)
                    return metadata;
            }
            if (metadata == null && symbol is IEventSymbol evt && evt.PartialDefinitionPart != null)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(evt.PartialDefinitionPart.OriginalDefinition);
                if (metadata != null)
                    return metadata;
            }
            if (metadata == null && symbol is INamedTypeSymbol type && type.ConstructedFrom != null)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(type.ConstructedFrom);
            }
            if (metadata == null && symbol.Kind == SymbolKind.DynamicType)
            {
                metadata = SymbolMetadatas.GetValueOrDefault(SystemDynamic);
            }
            if (metadata == null && symbol is IFieldSymbol field && symbol.ContainingType.IsTupleType)
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

        public GlobalCompilationVisitor(CSharpCompilation compilation, IProject project, IEnumerable<SymbolDescriptor> importedSymbols)
        {
            Compilation = compilation;
            Project = project;
            GlobalName = Evaluate("Global") ?? "$";
            AllSymbols = new Dictionary<string, SymbolMetadata>();
            SymbolMetadatas = new Dictionary<ISymbol, SymbolMetadata>(SymbolEqualityComparer.Default);
            ExtensionMethods = new Dictionary<string, IEnumerable<IMethodSymbol>>();
            Delegates = new Dictionary<string, IEnumerable<ITypeSymbol>>();
            Symbols = Symbols with
            {
                GlobalNamespace = GlobalName,
                AssemblySlug = GetAssemblyGlobalSlug(compilation.Assembly)
            };
            ImportedNames = new SymbolDescriptor();
            foreach (var m in importedSymbols)
            {
                foreach (var kv in m.Types)
                {
                    if (!ImportedNames.Types.ContainsKey(kv.Key))
                        ImportedNames.Types.Add(kv.Key, kv.Value);
                    else
                    {
                        Console.WriteLine($"Warning: conflicting symbol type key \"{kv.Key}\"");
                    }
                }
                foreach (var kv in m.Members)
                {
                    if (!ImportedNames.Members.ContainsKey(kv.Key))
                        ImportedNames.Members.Add(kv.Key, kv.Value);
                    else
                    {
                        Console.WriteLine($"Warning: conflicting symbol member key \"{kv.Key}\"");
                    }
                }
                foreach (var kv in m.LinkerSubstitutions)
                {
                    ImportedNames.LinkerSubstitutions.Add(kv);
                }
            }
            OutputMode = project.GetOutputMode();
            //AllNodes = compilation.SyntaxTrees.SelectMany(c => c.GetRoot().DescendantNodes()).ToList();
            Dictionary<string, int> usedKeys = new();
            //TypeNodes = AllNodes.Where(e => e is MemberDeclarationSyntax)
            //    .GroupBy(e => CreateFullMemberName((MemberDeclarationSyntax)e)!)
            //    .Where(e => e.Key != null)
            //    .ToDictionary(e =>
            //{
            //    var key = e.Key;
            //    var originalKey = key;
            //    int i = usedKeys.GetValueOrDefault(key);
            //    if (i > 0)
            //    {
            //        i++;
            //        key += "$$" + i;
            //    }
            //    if (i > 0)
            //        usedKeys[originalKey] = i + 1;
            //    return key;
            //}, e => new TypeSyntaxCache
            //{
            //    FullName = e.Key,
            //    Syntax = e.Cast<MemberDeclarationSyntax>().ToList()
            //});
            //ReversedTypeNodes = TypeNodes.ToDictionary(e => e.Value.Syntax.First(), e => e.Value);
            IEnumerable<ISymbol> GetConstructorsInType(ISymbol type)
            {
                if (type.Kind == SymbolKind.NamedType && type is ITypeSymbol t)
                {
                    foreach (var child in t.GetMembers(".cctor")) //static constructor
                        if (child.Kind == SymbolKind.Method/* is IMethodSymbol method*/)
                            yield return child;
                    foreach (var child in t.GetMembers(".ctor"))
                        if (child.Kind == SymbolKind.Method/* is IMethodSymbol method*/)
                            yield return child;
                }
            }
            IEnumerable<ISymbol> GetImplicitOperatorsInType(ITypeSymbol type)
            {
                foreach (var child in type.GetMembers("op_Implicit"))
                    if (child.Kind == SymbolKind.Method/* is IMethodSymbol method*/)
                        yield return child;
            }
            IEnumerable<IMethodSymbol> GetMethodsInMethod(IMethodSymbol type)
            {
                var decl = type.DeclaringSyntaxReferences;
                foreach (var d in decl)
                {
                    var localMethod = d.GetSyntax().DescendantNodes().Where(e => e.IsKind(SyntaxKind.LocalFunctionStatement)/* is LocalFunctionStatementSyntax*/).FastCast<LocalFunctionStatementSyntax>();
                    foreach (var lm in localMethod)
                    {
                        var symbol = compilation.GetSemanticModel(lm.SyntaxTree).GetDeclaredSymbol(lm);
                        if (symbol != null)
                            yield return symbol;
                    }
                }
            }
            IEnumerable<IMethodSymbol> GetMethodsInType(ITypeSymbol type)
            {
                foreach (var child in type.GetMembers())
                    if (child.Kind == SymbolKind.Method/* is IMethodSymbol method*/)
                    {
                        var method = Unsafe.As<IMethodSymbol>(child);
                        yield return method;
                        foreach (var localMethod in GetMethodsInMethod(method))
                            yield return localMethod;
                    }
            }
            IEnumerable<IPropertySymbol> GetPropertiesInType(ITypeSymbol type)
            {
                foreach (var child in type.GetMembers())
                    if (child.Kind == SymbolKind.Property/* is IPropertySymbol property*/)
                        yield return Unsafe.As<IPropertySymbol>(child);
            }
            IEnumerable<IFieldSymbol> GetFieldInType(ITypeSymbol type)
            {
                foreach (var child in type.GetMembers())
                    if (child.Kind == SymbolKind.Field/* is IFieldSymbol field*/)
                        yield return Unsafe.As<IFieldSymbol>(child);
            }
            IEnumerable<IEventSymbol> GetEventInType(ITypeSymbol type)
            {
                foreach (var child in type.GetMembers())
                    if (child.Kind == SymbolKind.Event/* is IEventSymbol _event*/)
                        yield return Unsafe.As<IEventSymbol>(child);
            }
            IEnumerable<ISymbol> GetMembersInType(ITypeSymbol type)
            {
                yield return type;
                foreach (var child in type.GetTypeMembers())
                    foreach (var innerType in GetMembersInType(child))
                        yield return innerType;
                foreach (var method in GetMethodsInType(type))
                    yield return method;
                foreach (var method in GetImplicitOperatorsInType(type))
                    yield return method;
                foreach (var property in GetPropertiesInType(type))
                    yield return property;
                foreach (var field in GetFieldInType(type))
                    yield return field;
                foreach (var _event in GetEventInType(type))
                    yield return _event;
            }
            IEnumerable<ISymbol> GetAllInNamespaces(INamespaceSymbol @namespace)
            {
                //if (deep && @namespace.Kind == SymbolKind.Namespace && @namespace is INamespaceSymbol ns)
                //{
                yield return @namespace;
                foreach (var child in @namespace.GetNamespaceMembers())
                    foreach (var namespace2 in GetAllInNamespaces(child))
                        yield return namespace2;
                //}
                foreach (var type in @namespace.GetTypeMembers())
                    foreach (var itype in GetMembersInType(type))
                        yield return itype;
            }
            var referencedSymbols = compilation.SourceModule.ReferencedAssemblySymbols
                        .SelectMany(a => GetAllInNamespaces(a.GlobalNamespace));
            usedKeys = new();
            IEnumerable<ISymbol> SplitPropertySymbol(ISymbol symbol)
            {
                yield return symbol;
                if (symbol.Kind == SymbolKind.Property/* is IPropertySymbol property*/)
                {
                    var property = Unsafe.As<IPropertySymbol>(symbol);
                    if (property.GetMethod != null && !property.GetMethod.Equals(property, SymbolEqualityComparer.Default))
                        yield return property.GetMethod;
                    if (property.SetMethod != null && !property.SetMethod.Equals(property, SymbolEqualityComparer.Default))
                        yield return property.SetMethod;
                }
            }
            $"Collecting symbols".Profile(() =>
            {
                AllSymbols =
                    compilation.GetSymbolsWithName(e => true, SymbolFilter.Namespace)
                    .SelectMany(a => GetAllInNamespaces(Unsafe.As<INamespaceSymbol>(a)))
                    .Concat(referencedSymbols)
                    .SelectMany(SplitPropertySymbol)
                    .SelectMany(c => (ISymbol[])[c, .. GetConstructorsInType(c)])
                    .Distinct(SymbolEqualityComparer.Default)

                    //compilation.GetSymbolsWithName(e => true, SymbolFilter.All)
                    //.Concat(referencedSymbols)

                    .Select(symbol =>
                    {
                        var WithTypeParameter = symbol.CreateSignature(this, withTypeParameterNames: true, withGlobalNamespace: true);
                        var WithoutTypeParameter = symbol.CreateSignature(this, withTypeParameterNames: false, withGlobalNamespace: true);
                        var originalKey = WithoutTypeParameter;
                        //if (symbol.Kind == SymbolKind.NamedType)
                        //{
                        var i = usedKeys.GetValueOrDefault(WithoutTypeParameter);
                        if (i > 0)
                        {
                            i++;
                            WithoutTypeParameter += "$$" + i;
                            WithTypeParameter += "$$" + i;
                        }
                        usedKeys[originalKey] = i + 1;
                        //}
                        return (withoutTypeParameterNames: WithoutTypeParameter, withTypeParameterNames: WithTypeParameter, symbol);
                    })
                    .ToDictionary(s =>
                    {
                        return s.withoutTypeParameterNames;
                    }, s =>
                    {
                        //var fullNameNoMethodParameter = s.withoutTypeParameterNames.Contains('(') ? s.withoutTypeParameterNames.Split('(').First() : s.withTypeParameterNames;
                        //var split = fullNameNoMethodParameter.Split('.');
                        string signature;
                        if (s.symbol.Kind == SymbolKind.NamedType/* is INamespaceOrTypeSymbol ns*/)
                        {
                            signature = s.withTypeParameterNames;
                        }
                        else
                        {
                            int lastDot = -1;
                            //if (s.symbol is IMethodSymbol || (s.symbol is IPropertySymbol p && p.IsIndexer))
                            if (s.symbol.Kind == SymbolKind.Method || (s.symbol.Kind == SymbolKind.Property && Unsafe.As<IPropertySymbol>(s.symbol).IsIndexer))
                            {
                                var braceIndex = s.withTypeParameterNames.IndexOf('(');
                                if (braceIndex > 0)
                                {
                                    //int splitAt = s.key[..braceIndex].LastIndexOf('.');
                                    lastDot = s.withTypeParameterNames.Substring(0, braceIndex).LastIndexOf('.');
                                    //signature = s.key.Substring(splitAt + 1);
                                }
                            }
                            if (lastDot < 0)
                                lastDot = s.withTypeParameterNames.LastIndexOf('.');
                            if (lastDot > 0)
                            {
                                if (s.withTypeParameterNames[lastDot - 1] == '.') //..ctor
                                    lastDot--;
                                signature = s.withTypeParameterNames.Substring(lastDot + 1);
                            }
                            else
                            {
                                signature = s.withTypeParameterNames;
                            }
                        }
                        //var braceIndex = s.key.IndexOf('(');
                        //if (braceIndex > 0)
                        //{
                        //    //int splitAt = s.key[..braceIndex].LastIndexOf('.');
                        //    int splitAt = s.key.Substring(0, braceIndex).LastIndexOf('.');
                        //    signature = s.key.Substring(splitAt + 1);
                        //}
                        //else
                        //{
                        //    signature = split.Last();
                        //}
                        return new SymbolMetadata(this)
                        {
                            Symbol = s.symbol,
                            FullName = s.withoutTypeParameterNames,
                            Signature = signature,
                            DeclaringReferences = s.symbol.DeclaringSyntaxReferences,
                            //Nodes = s.symbol.DeclaringSyntaxReferences.Select(e => e.GetSyntax()).ToList(),
                        };
                    });
            });
            $"Collecting symbol metadatas".Profile(() =>
            {
                SymbolMetadatas = AllSymbols
                    //.GroupBy(e => e.Value.Symbol, SymbolEqualityComparer.Default)
                    .ToDictionary(e => e.Value.Symbol, e => e.Value, SymbolEqualityComparer.Default);
            });
            //resolve class name overloads. Only generic ones may class though
            ConventionAttribute? GetConvention(ISymbol symbol)
            {
                foreach (var a in symbol.GetAttributes().Select(a => (a, a.AttributeClass)).Where(e => e.AttributeClass != null))
                {
                    var aName = a.AttributeClass!.CreateSignature(this, withGlobalNamespace: false)!;
                    if (!aName.EndsWith("Attribute"))
                        aName += "Attribute";
                    if (aName == typeof(ConventionAttribute).FullName)
                    {
                        var notation = a.a.ConstructorArguments.Count() > 0 ? a.a.ConstructorArguments.ElementAt(0).Value : a.a.NamedArguments.FirstOrDefault(c => c.Key == nameof(ConventionAttribute.Notation)).Value.Value;
                        var target = a.a.ConstructorArguments.Count() > 1 ? a.a.ConstructorArguments.ElementAt(1).Value : a.a.NamedArguments.FirstOrDefault(c => c.Key == nameof(ConventionAttribute.Target)).Value.Value;
                        var member = a.a.NamedArguments.FirstOrDefault(c => c.Key == nameof(ConventionAttribute.Member)).Value.Value;
                        return new ConventionAttribute
                        {
                            Notation = notation != null ? (Notation)int.Parse(notation.ToString()!) : Notation.None,
                            Target = target != null ? (ConventionTarget)int.Parse(target.ToString()!) : ConventionTarget.All,
                            Member = member != null ? (ConventionMember)int.Parse(member.ToString()!) : ConventionMember.All,
                        };
                    }
                }
                if (symbol.ContainingSymbol != null)
                {
                    if (symbol is INamedTypeSymbol && symbol.ContainingSymbol is INamedTypeSymbol) //dont inherit conventions for inner class
                    {

                    }
                    else
                        return GetConvention(symbol.ContainingSymbol);
                }
                return null;
            }
            string ToNotation(string s, Notation n)
            {
                switch (n)
                {
                    case Notation.LowerCase:
                        return s.ToLower();
                    case Notation.UpperCase:
                        return s.ToUpper();
                    case Notation.CamelCase:
                        return new string(s.ToCharArray().Select((ss, i) => i == 0 ? char.ToLower(ss) : ss).ToArray());
                    case Notation.PascalCase:
                        return new string(s.ToCharArray().Select((ss, i) => i == 0 ? char.ToUpper(ss) : ss).ToArray());
                }
                return s;
            }

            int Hierachy(INamedTypeSymbol t)
            {
                int depth = 0;
                while (t?.BaseType != null)
                {
                    depth++;
                    t = t.BaseType;
                }
                return depth;
            }

            var assemblyNamespace = GetAssemblyGlobalSlug(Compilation.Assembly);
            Dictionary<ITypeSymbol, Dictionary<string, HashSet<string>>> usedMemberNames = new(SymbolEqualityComparer.Default);
            var reversMemberNames = ImportedNames.Members.ToDictionary(e => e.Key, e => e.Value.ToDictionary(ee => ee.Value.Signature ?? "Null"/*The yaml deserializer deserializes Null value as null*/, ee => ee.Key));

            bool IsMemberNameUsed(ITypeSymbol type, string overloadName, string tryUseName, out HashSet<string> overloadUsedNames)
            {
                if (!usedMemberNames.TryGetValue(type, out var typeUsedMemberNames))
                {
                    typeUsedMemberNames = new();
                    usedMemberNames[type] = typeUsedMemberNames;
                    ////pre populate with every names the base type has used, so they dont get reused, unless overriden
                    //var baseType = pproperty.ContainingType.BaseType;
                    //while (baseType != null)
                    //{
                    //    if (usedMemberNames.TryGetValue(baseType, out var baseUsedBaseNames))
                    //    {
                    //        foreach (var kv in baseUsedBaseNames)
                    //        {
                    //            if (!usedPropertyNames.TryGetValue(kv.Key, out var upn))
                    //            {
                    //                upn = new HashSet<string>();
                    //                usedPropertyNames[kv.Key] = upn;
                    //            }
                    //            upn.AddRange(kv.Value);
                    //        }
                    //        //usedPropertyNames.AddRange(usedBaseNames);
                    //    }
                    //    baseType = baseType.BaseType;
                    //}
                }
                if (!typeUsedMemberNames.TryGetValue(overloadName, out overloadUsedNames))
                {
                    overloadUsedNames = new HashSet<string>();
                    typeUsedMemberNames[overloadName] = overloadUsedNames;
                }
                if (overloadUsedNames.Contains(tryUseName))
                {
                    return true;
                }
                if (type.BaseType != null)
                {
                    return IsMemberNameUsed(type.BaseType.OriginalDefinition, overloadName, tryUseName, out _);
                }
                return false;
            }


            //Dictionary<string, string> usedNamespaceName = new();
            //foreach (var ns in AllSymbols.Values.Where(e => e.Symbol.Kind == SymbolKind.Namespace))
            //{
            //    var name = GlobalName + (string.IsNullOrEmpty(ns.FullName) ? "" : "." + ns.FullName);
            //    ns.OverloadName = SymbolMetadata.ShortName(this, null, null, ns.FullName, name, usedNamespaceName);
            //}
            $"Preprocessing type symbols".Profile(() =>
            {
                Dictionary<string, HashSet<string>> usedTypeNames = new();
                var reversedTypeNames = ImportedNames.Types.ToDictionary(e => e.Value.Signature, e => e.Key);
                foreach (var type in AllSymbols!.Values.Where(e => e.Symbol.Kind == SymbolKind.NamedType/* is ITypeSymbol*/)/*.GroupBy(e => e.FullName.Split('<')[0])*/)
                {
                    //foreach (var type in group)
                    {
                        var ttype = Unsafe.As<INamedTypeSymbol>(type.Symbol);
                        //trying to detect compiler generated types
                        if (ttype.IsImplicitlyDeclared)
                            continue;
                        if (ttype.IsAnonymousType)
                            continue;
                        //if (ttype.IsGenericType && ttype.Arity == 0)
                        //    continue;
                        if (ttype.Name.Contains("<>"))
                            continue;
                        if (ttype.Name.StartsWith("<"))
                            continue;
                        if (ttype.Name.Contains("="))
                            continue;
                        bool isImportedType = !SymbolEqualityComparer.Default.Equals(ttype.ContainingAssembly, compilation.Assembly);
                        if (isImportedType && reversedTypeNames.TryGetValue(type.Signature, out var ovName))
                        {
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
                            string overloadedName = type.Symbol.Name;
                            string? originalPrefixOverloadName = null;
                            string? shortPrefixOverloadName = null;
                            //string? originalPrefixInvocationName = null;
                            //string? shortPrefixInvocationName = null;
                            bool hasNameAttribute = false;
                            if (HasAttribute(type.Symbol, typeof(NameAttribute).FullName!, null, false, out var args))
                            {
                                var name = (string)args![0]!;
                                if (!HasAttribute(type.Symbol, typeof(ExternalAttribute).FullName!, null, false, out _))
                                {
                                    var prefix = GlobalName + "." + assemblyNamespace + ".";
                                    name = name.StartsWith(prefix) ? name : prefix + name;
                                }
                                type.OriginalOverloadName = name;
                                type.OverloadName = name;
                                hasNameAttribute = true;
                            }
                            else
                            {
                                if (OutputMode.HasFlag(OutputMode.Global))
                                {
                                    //if (ttype.ContainingSymbol.Kind == SymbolKind.NamedType/* is INamedTypeSymbol container*/)
                                    //{
                                    //    if (ttype.ContainingSymbol.Name == "YieldToIterator" && ttype.Name == "Enumerator")
                                    //    {

                                    //    }
                                    //    overloadedName = ttype.Name + (ttype.Arity > 0 ? "$" + string.Join("", Enumerable.Range(1, ttype.Arity).Select(i => "$")) : "");
                                    //    var containerMeata = SymbolMetadatas![ttype.ContainingSymbol];
                                    //    originalPrefixOverloadName = containerMeata.OriginalOverloadName;
                                    //    shortPrefixOverloadName = containerMeata.OverloadName;
                                    //    originalPrefixInvocationName = containerMeata.OriginalInvocationName;
                                    //    shortPrefixInvocationName = containerMeata.InvocationName;
                                    //}
                                    //else
                                    {
                                        if (ttype.IsExtension)
                                        {
                                            var parentSaticClass = ttype.ContainingType;
                                            var parentSaticClassMetadata = SymbolMetadatas![parentSaticClass];
                                            type.OriginalOverloadName = parentSaticClassMetadata.OriginalOverloadName;
                                            type.OverloadName = parentSaticClassMetadata.OverloadName;
                                            continue;
                                        }
                                        else
                                        {
                                            var prefix = GlobalName + "." + assemblyNamespace + ".";
                                            var fn = type.FullName.Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                                            overloadedName = (fn.StartsWith(prefix) ? null : prefix) + fn;
                                        }
                                    }
                                }
                                if (!usedTypeNames.TryGetValue(overloadedName, out var usedName))
                                {
                                    usedName = new HashSet<string>();
                                    usedTypeNames[overloadedName] = usedName;
                                }
                                if (!usedName.Add(overloadedName))
                                {
                                    overloadedName = $"{overloadedName}${usedName.Count}";
                                    usedName.Add(overloadedName);
                                }
                                type.OriginalOverloadName = (originalPrefixOverloadName != null ? originalPrefixOverloadName + "." : "") + overloadedName;
                                type.OverloadName = SymbolMetadata.ShortName(this, shortPrefixOverloadName, originalPrefixOverloadName, type.Signature, overloadedName, !isExtern ? Symbols.Types : new Dictionary<string, SymbolValue>(), generate: !isExtern, export: !hasNameAttribute && !isExtern && !isImportedType);
                            }
                        }
                    }
                }
            });

            $"Preprocessing field symbols".Profile(() =>
            {
                foreach (var field in AllSymbols!.Values
                    .Where(e => e.Symbol.Kind == SymbolKind.Field)
                    .OrderBy(m => Hierachy(Unsafe.As<IFieldSymbol>(m.Symbol).ContainingType)))
                {
                    var ffield = Unsafe.As<IFieldSymbol>(field.Symbol);
                    //skip compiler generated backing fields. We dont need it
                    if (ffield.IsImplicitlyDeclared)
                        continue;
                    if (ffield.Name.Contains("<>"))
                        continue;
                    if (ffield.Name.StartsWith("<"))
                        continue;
                    if (ffield.Name.Contains("="))
                        continue;
                    if (ffield.ContainingType.IsImplicitlyDeclared)
                        continue;
                    if (ffield.ContainingType.Name.Contains("<>"))
                        continue;
                    if (ffield.ContainingType.Name.StartsWith("<"))
                        continue;
                    if (ffield.ContainingType.Name.Contains("="))
                        continue;
                    bool isImportedType = !SymbolEqualityComparer.Default.Equals(ffield.ContainingAssembly, compilation.Assembly);
                    var declaringType = field.Symbol.ContainingType;
                    var declaringTypeMetadata = SymbolMetadatas![declaringType.OriginalDefinition];
                    if (isImportedType && reversMemberNames.TryGetValue(declaringTypeMetadata.Signature, out var m) && m.TryGetValue(field.Signature, out var ovName))
                    {
                        var overloadNames = ovName.Split('|');
                        if (overloadNames.Length == 2)
                        {
                            field.OriginalOverloadName = overloadNames[0];
                            field.OverloadName = overloadNames[1];
                        }
                        else
                        {
                            field.OriginalOverloadName = overloadNames[0];
                            field.OverloadName = overloadNames[0];
                        }
                    }
                    else
                    {
                        string overloadedName = ffield.Name;
                        bool isExtern = IsExtern(field.Symbol);
                        bool hasNameAttribute = false;
                        if (HasAttribute(field.Symbol, typeof(NameAttribute).FullName!, null, false, out var args))
                        {
                            overloadedName = (string)args![0];
                            hasNameAttribute = true;
                        }
                        else
                        {
                            var name = ffield.Name.RemoveGenericParameterNames(out _);
                            //name = name.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                            //if (!isExtern)
                            {
                                if (!name.Contains(".")) //doesnt apply to explicit interface implementations
                                {
                                    var convention = GetConvention(field.Symbol);
                                    if (convention?.Member == ConventionMember.All || (convention?.Member.HasFlag(ConventionMember.Property) ?? false))
                                    {
                                        name = ToNotation(name, convention.Notation);
                                    }
                                }
                            }
                            overloadedName = name;
                            if (!isExtern)
                            {
                                if (declaringType.TypeKind == TypeKind.Interface)
                                {
                                    if (!overloadedName.Contains("."))
                                    {
                                        overloadedName = declaringTypeMetadata.OverloadName + "." + overloadedName;
                                        if (overloadedName.StartsWith(GlobalName + "."))
                                            overloadedName = overloadedName.Substring(GlobalName.Length + 1);
                                    }
                                }
                            }
                            overloadedName = overloadedName.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                            //If we are using shortnames, overload resolution will be handled by the shortname
                            if (!isExtern && !OutputMode.HasFlag(OutputMode.ShortNames))
                            {
                                if (!usedMemberNames.TryGetValue(ffield.ContainingType, out var usedFieldNames))
                                {
                                    usedFieldNames = new();
                                    usedMemberNames[ffield.ContainingType] = usedFieldNames;
                                }
                                if (!usedFieldNames.TryGetValue(overloadedName, out var usedName))
                                {
                                    usedName = new HashSet<string>();
                                    usedFieldNames[overloadedName] = usedName;
                                }
                                if (!usedName.Add(overloadedName))
                                {
                                    overloadedName = $"{overloadedName}${usedName.Count}";
                                    usedName.Add(overloadedName);
                                }
                            }
                        }
                        bool export = !hasNameAttribute && !isExtern && !isImportedType;
                        Dictionary<string, SymbolValue> exportNames;
                        if (!export)
                        {
                            exportNames = new Dictionary<string, SymbolValue>();
                        }
                        else if (!Symbols.Members.TryGetValue(declaringTypeMetadata.Signature, out exportNames))
                        {
                            exportNames = new Dictionary<string, SymbolValue>();
                            Symbols.Members.Add(declaringTypeMetadata.Signature, exportNames);
                        }
                        field.OriginalOverloadName = overloadedName;
                        field.OverloadName = SymbolMetadata.ShortName(this, null, null, field.Signature, overloadedName, exportNames, generate: !isExtern, export: export);
                    }
                }
            });

            $"Preprocessing event symbols".Profile(() =>
            {
                foreach (var @event in AllSymbols!.Values
                    .Where(e => e.Symbol.Kind == SymbolKind.Event)
                    .OrderBy(m => Hierachy(Unsafe.As<IEventSymbol>(m.Symbol).ContainingType)))
                {
                    var eevent = Unsafe.As<IEventSymbol>(@event.Symbol);
                    //skip compiler generated backing fields. We dont need it
                    if (eevent.IsImplicitlyDeclared)
                        continue;
                    if (eevent.Name.Contains("<>"))
                        continue;
                    if (eevent.Name.StartsWith("<"))
                        continue;
                    if (eevent.Name.Contains("="))
                        continue;
                    if (eevent.ContainingType.IsImplicitlyDeclared)
                        continue;
                    if (eevent.ContainingType.Name.Contains("<>"))
                        continue;
                    if (eevent.ContainingType.Name.StartsWith("<"))
                        continue;
                    if (eevent.ContainingType.Name.Contains("="))
                        continue;
                    bool isImportedType = !SymbolEqualityComparer.Default.Equals(eevent.ContainingAssembly, compilation.Assembly);
                    var declaringType = @event.Symbol.ContainingType;
                    var declaringTypeMetadata = SymbolMetadatas![declaringType.OriginalDefinition];
                    if (isImportedType && reversMemberNames.TryGetValue(declaringTypeMetadata.Signature, out var m) && m.TryGetValue(@event.Signature, out var ovName))
                    {
                        var overloadNames = ovName.Split('|');
                        if (overloadNames.Length == 2)
                        {
                            @event.OriginalOverloadName = overloadNames[0];
                            @event.OverloadName = overloadNames[1];
                        }
                        else
                        {
                            @event.OriginalOverloadName = overloadNames[0];
                            @event.OverloadName = overloadNames[0];
                        }
                    }
                    else
                    {
                        string overloadedName = eevent.Name;
                        bool isExtern = IsExtern(@event.Symbol);
                        bool hasNameAttribute = false;
                        if (HasAttribute(@event.Symbol, typeof(NameAttribute).FullName!, null, false, out var args))
                        {
                            overloadedName = (string)args![0];
                            hasNameAttribute = true;
                        }
                        else
                        {
                            var name = eevent.Name.RemoveGenericParameterNames(out _);
                            //name = name.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                            //if (!isExtern)
                            {
                                if (!name.Contains(".")) //doesnt apply to explicit interface implementations
                                {
                                    var convention = GetConvention(@event.Symbol);
                                    if (convention?.Member == ConventionMember.All || (convention?.Member.HasFlag(ConventionMember.Property) ?? false))
                                    {
                                        name = ToNotation(name, convention.Notation);
                                    }
                                }
                            }
                            overloadedName = name;
                            if (!isExtern)
                            {
                                if (declaringType.TypeKind == TypeKind.Interface)
                                {
                                    if (!overloadedName.Contains("."))
                                    {
                                        overloadedName = declaringTypeMetadata.OverloadName + "." + overloadedName;
                                        if (overloadedName.StartsWith(GlobalName + "."))
                                            overloadedName = overloadedName.Substring(GlobalName.Length + 1);
                                    }
                                }
                            }
                            overloadedName = overloadedName.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                            //If we are using shortnames, overload resolution will be handled by the shortname
                            if (!isExtern && !OutputMode.HasFlag(OutputMode.ShortNames))
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
                                usedNames.Add(tryName);

                                //if (!usedMemberNames.TryGetValue(eevent.ContainingType, out var usedFieldNames))
                                //{
                                //    usedFieldNames = new();
                                //    usedMemberNames[eevent.ContainingType] = usedFieldNames;
                                //}
                                //if (!usedFieldNames.TryGetValue(overloadedName, out var usedName))
                                //{
                                //    usedName = new HashSet<string>();
                                //    usedFieldNames[overloadedName] = usedName;
                                //}
                                //if (!usedName.Add(overloadedName))
                                //{
                                //    overloadedName = $"{overloadedName}${usedName.Count}";
                                //    usedName.Add(overloadedName);
                                //}
                            }
                        }
                        bool export = !hasNameAttribute && !isExtern && !isImportedType;
                        Dictionary<string, SymbolValue> exportNames;
                        if (!export)
                        {
                            exportNames = new Dictionary<string, SymbolValue>();
                        }
                        else if (!Symbols.Members.TryGetValue(declaringTypeMetadata.Signature, out exportNames))
                        {
                            exportNames = new Dictionary<string, SymbolValue>();
                            Symbols.Members.Add(declaringTypeMetadata.Signature, exportNames);
                        }
                        @event.OriginalOverloadName = overloadedName;
                        @event.OverloadName = SymbolMetadata.ShortName(this, null, null, @event.Signature, overloadedName, exportNames, generate: !isExtern, export: export);
                    }
                }
            });


            $"Preprocessing property symbols".Profile(() =>
            {
                foreach (var property in AllSymbols!.Values
                    .Where(e => e.Symbol.Kind == SymbolKind.Property)
                    .OrderBy(m => Hierachy(Unsafe.As<IPropertySymbol>(m.Symbol).ContainingType)) //make sure we process base/virtual property before overrides once, since we want to use the base overload name for all
                    )
                {
                    var pproperty = Unsafe.As<IPropertySymbol>(property.Symbol);
                    if (pproperty.IsOverride) //a property that overrides a base property must use exactly the same overload name as its base
                    {
                        var baseProperty = pproperty.OverriddenProperty!;
                        while (baseProperty.IsOverride)
                            baseProperty = baseProperty.OverriddenProperty!;
                        var overriddenMetadata = GetRequiredMetadata(baseProperty);
                        property.OriginalOverloadName = overriddenMetadata.OriginalOverloadName;
                        property.OverloadName = overriddenMetadata.OverloadName;
                    }
                    else
                    {
                        bool isImportedType = !SymbolEqualityComparer.Default.Equals(pproperty.ContainingAssembly, compilation.Assembly);
                        var declaringType = property.Symbol.ContainingType;
                        var declaringTypeMetadata = SymbolMetadatas![declaringType.OriginalDefinition];
                        if (isImportedType && reversMemberNames.TryGetValue(declaringTypeMetadata.Signature, out var m) && m.TryGetValue(property.Signature, out var ovName))
                        {
                            var overloadNames = ovName.Split('|');
                            if (overloadNames.Length == 2)
                            {
                                property.OriginalOverloadName = overloadNames[0];
                                property.OverloadName = overloadNames[1];
                            }
                            else
                            {
                                property.OriginalOverloadName = overloadNames[0];
                                property.OverloadName = overloadNames[0];
                            }
                        }
                        else
                        {
                            string overloadedName = pproperty.Name;
                            bool isExtern = IsExtern(property.Symbol);
                            bool hasNameAttribute = false;
                            if (HasAttribute(property.Symbol, typeof(NameAttribute).FullName!, null, false, out var args))
                            {
                                overloadedName = (string)args![0];
                                hasNameAttribute = true;
                            }
                            else
                            {
                                string GetCleanName()
                                {
                                    if (pproperty.Name.StartsWith("<") && pproperty.ExplicitInterfaceImplementations.Any())
                                    {
                                        var originalInterfaceMethod = pproperty.ExplicitInterfaceImplementations.First();
                                        // 2. Define a clean format: Namespace + Containing Types + Name (Omits "global::")
                                        var cleanFormat = new SymbolDisplayFormat(
                                            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                                            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                                            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType
                                        );

                                        return originalInterfaceMethod.ToDisplayString(cleanFormat);
                                    }
                                    return pproperty.Name;
                                }
                                string name = GetCleanName().RemoveGenericParameterNames(out _);
                                if (name == "this[]")
                                {
                                    name = "Item";
                                }
                                //name = name.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                                //if (!isExtern)
                                {
                                    if (!name.Contains(".")) //doesnt apply to explicit interface implementations
                                    {
                                        var convention = GetConvention(property.Symbol);
                                        if (convention?.Member == ConventionMember.All || (convention?.Member.HasFlag(ConventionMember.Property) ?? false))
                                        {
                                            name = ToNotation(name, convention.Notation);
                                        }
                                    }
                                }
                                overloadedName = name;
                                if (!isExtern)
                                {
                                    if (declaringType.TypeKind == TypeKind.Interface)
                                    {
                                        if (!overloadedName.Contains("."))
                                        {
                                            overloadedName = declaringTypeMetadata.OverloadName + "." + overloadedName;
                                            var assemblySlug = GetAssemblyGlobalSlug(declaringType.ContainingAssembly);
                                            if (overloadedName.StartsWith(GlobalName + "." + assemblySlug + "."))
                                            {
                                                overloadedName = overloadedName.Substring(GlobalName.Length + 1 + assemblySlug.Length + 1);
                                                //if (overloadedName.StartsWith(assemblyNamespace + "."))
                                                //{
                                                //    overloadedName = overloadedName.Substring(assemblyNamespace.Length + 1);
                                                //}
                                            }
                                        }
                                    }
                                }
                                overloadedName = overloadedName.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                                //If we are using shortnames, overload resolution will be handled by the shortname
                                if (!isExtern && !OutputMode.HasFlag(OutputMode.ShortNames))
                                {
                                    //if (!usedMemberNames.TryGetValue(pproperty.ContainingType, out var usedPropertyNames))
                                    //{
                                    //    usedPropertyNames = new();
                                    //    usedMemberNames[pproperty.ContainingType] = usedPropertyNames;
                                    //    ////pre populate with every names the base type has used, so they dont get reused, unless overriden
                                    //    //var baseType = pproperty.ContainingType.BaseType;
                                    //    //while (baseType != null)
                                    //    //{
                                    //    //    if (usedMemberNames.TryGetValue(baseType, out var baseUsedBaseNames))
                                    //    //    {
                                    //    //        foreach (var kv in baseUsedBaseNames)
                                    //    //        {
                                    //    //            if (!usedPropertyNames.TryGetValue(kv.Key, out var upn))
                                    //    //            {
                                    //    //                upn = new HashSet<string>();
                                    //    //                usedPropertyNames[kv.Key] = upn;
                                    //    //            }
                                    //    //            upn.AddRange(kv.Value);
                                    //    //        }
                                    //    //        //usedPropertyNames.AddRange(usedBaseNames);
                                    //    //    }
                                    //    //    baseType = baseType.BaseType;
                                    //    //}
                                    //}
                                    //if (!usedPropertyNames.TryGetValue(overloadedName, out var usedName))
                                    //{
                                    //    usedName = new HashSet<string>();
                                    //    usedPropertyNames[overloadedName] = usedName;
                                    //}
                                    int iNameSlug = 1;
                                    string tryName = overloadedName;
                                    HashSet<string>? usedNames = null;
                                    while (IsMemberNameUsed(pproperty.ContainingType, overloadedName, tryName, out usedNames))
                                    {
                                        tryName = $"{overloadedName}${iNameSlug}";
                                        iNameSlug++;
                                    }
                                    overloadedName = tryName;
                                    usedNames.Add(tryName);
                                    //if (!usedName.Add(overloadedName))
                                    //{
                                    //    overloadedName = $"{overloadedName}${usedName.Count}";
                                    //    usedName.Add(overloadedName);
                                    //}
                                }
                            }
                            bool export = !hasNameAttribute && !isExtern && !isImportedType;
                            Dictionary<string, SymbolValue> exportNames;
                            if (!export)
                            {
                                exportNames = new Dictionary<string, SymbolValue>();
                            }
                            else if (!Symbols.Members.TryGetValue(declaringTypeMetadata.Signature, out exportNames))
                            {
                                exportNames = new Dictionary<string, SymbolValue>();
                                Symbols.Members.Add(declaringTypeMetadata.Signature, exportNames);
                            }
                            property.OriginalOverloadName = overloadedName;
                            property.OverloadName = SymbolMetadata.ShortName(this, null, null, property.Signature, overloadedName, exportNames, generate: !isExtern, export: export);
                        }
                    }
                }
            });

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

            $"Preprocessing method symbols".Profile(() =>
            {
                foreach (var method in AllSymbols!.Values.Where(e => e.Symbol.Kind == SymbolKind.Method)
                    //foreach (var group in AllSymbols.Values.Where(e => e.Symbol.Kind == SymbolKind.Method)
                    //.OrderBy(m => ((IMethodSymbol)m.Symbol).IsOverride ? 2 : 1)
                    //.GroupBy<SymbolMetadata, INamedTypeSymbol>(e => e.Symbol.ContainingType, SymbolEqualityComparer.Default)
                    //.OrderBy(o => Hierachy(o.Key))// //make sure we process base/virtual methods before overrides once, since we want to use the base overload name for all
                    .OrderBy(m => Hierachy(Unsafe.As<IMethodSymbol>(m.Symbol).ContainingType)) //make sure we process base/virtual methods before overrides ones, since we want to use the base overload name for all
                    )
                {
                    var mmethod = Unsafe.As<IMethodSymbol>(method.Symbol);
                    if (mmethod.Name == ".ctor" && mmethod.ContainingType.BaseType != null)
                    {
                        if (!usedMemberNames.TryGetValue(mmethod.ContainingType, out var usedMethodNames))
                        {
                            usedMethodNames = new();
                            usedMemberNames[mmethod.ContainingType] = usedMethodNames;
                        }
                        if (!usedMethodNames.TryGetValue("$ctor", out var usedName))
                        {
                            usedName = new HashSet<string>();
                            usedMethodNames["$ctor"] = usedName;
                        }
                        //All constructor of a class hierachy must have a unique name
                        //Constructors are not overloadable/overidable anyway.
                        //Load all lower class contructor names from lower hierachy class into this hashset to make sure we dont resuse the same name
                        var ctorNames = GetConstructorNames(mmethod.ContainingType.BaseType);
                        foreach (var name in ctorNames)
                            usedName.Add(name);
                    }
                    //if (mmethod.Name=="GetHashCode" && mmethod.ContainingType.Name== "StringEqualityComparer")
                    //{

                    //}
                    //foreach (var method in group)
                    //{
                    if (mmethod.IsOverride) //a method that overrides a base method must use exactly the same overload name as its base
                    {
                        var baseMethod = mmethod.OverriddenMethod!;
                        while (baseMethod.IsOverride)
                            baseMethod = baseMethod.OverriddenMethod!;
                        var overriddenMetadata = GetRequiredMetadata(baseMethod);
                        method.OriginalOverloadName = overriddenMetadata.OriginalOverloadName;
                        method.OverloadName = overriddenMetadata.OverloadName;
                    }
                    else
                    {
                        bool isImportedType = !SymbolEqualityComparer.Default.Equals(mmethod.ContainingAssembly, compilation.Assembly);
                        var declaringType = method.Symbol.ContainingType;
                        var declaringTypeMetadata = SymbolMetadatas![declaringType.OriginalDefinition];
                        if (isImportedType && reversMemberNames.TryGetValue(declaringTypeMetadata.Signature, out var m) && m.TryGetValue(method.Signature, out var ovName))
                        {
                            var overloadNames = ovName.Split('|');
                            if (overloadNames.Length == 2)
                            {
                                method.OriginalOverloadName = overloadNames[0];
                                method.OverloadName = overloadNames[1];
                            }
                            else
                            {
                                method.OriginalOverloadName = overloadNames[0];
                                method.OverloadName = overloadNames[0];
                            }
                        }
                        else
                        {
                            string overloadedName = mmethod.Name.RemoveGenericParameterNames(out _);
                            //if we have added an overload to Item accessor name eg Item$1, we want to make sure the same reflects in the methods get_Item$1
                            if (mmethod.AssociatedSymbol != null && mmethod.AssociatedSymbol is IPropertySymbol p && p.IsIndexer)
                            {
                                var metadata = GetRequiredMetadata(mmethod.AssociatedSymbol);
                                overloadedName = overloadedName.Replace("_Item", "_" + metadata.OverloadName);
                            }
                            bool isExtern = IsExtern(method.Symbol);
                            bool hasNameAttribute = false;
                            if (HasAttribute(method.Symbol, typeof(NameAttribute).FullName!, null, false, out var args))
                            {
                                overloadedName = (string)args![0];
                                hasNameAttribute = true;
                            }
                            else
                            {
                                string GetCleaName()
                                {
                                    if (mmethod.Name.StartsWith("<") && mmethod.ExplicitInterfaceImplementations.Any())
                                    {
                                        var originalInterfaceMethod = mmethod.ExplicitInterfaceImplementations.First();
                                        // 2. Define a clean format: Namespace + Containing Types + Name (Omits "global::")
                                        var cleanFormat = new SymbolDisplayFormat(
                                            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                                            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                                            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType
                                        );

                                        return originalInterfaceMethod.ToDisplayString(cleanFormat);
                                    }
                                    return mmethod.Name;
                                }
                                overloadedName = GetCleaName().RemoveGenericParameterNames(out _);
                                if (!overloadedName.Contains(".")) //doesnt apply to explicit interface implementations
                                {
                                    var convention = GetConvention(method.Symbol);
                                    if (convention?.Member == ConventionMember.All || (convention?.Member.HasFlag(ConventionMember.Method) ?? false))
                                    {
                                        overloadedName = ToNotation(overloadedName, convention.Notation);
                                    }
                                }
                                if (!isExtern)
                                {
                                    if (declaringType.TypeKind == TypeKind.Interface)
                                    {
                                        if (!overloadedName.Contains("."))
                                        {
                                            overloadedName = declaringTypeMetadata.OverloadName + "." + overloadedName;
                                            var assemblySlug = GetAssemblyGlobalSlug(declaringType.ContainingAssembly);
                                            if (overloadedName.StartsWith(GlobalName + "." + assemblySlug + "."))
                                            {
                                                overloadedName = overloadedName.Substring(GlobalName.Length + 1 + assemblySlug.Length + 1);
                                                //if (overloadedName.StartsWith(assemblyNamespace + "."))
                                                //{
                                                //    overloadedName = overloadedName.Substring(assemblyNamespace.Length + 1);
                                                //}
                                            }
                                        }
                                    }
                                }
                                string originalName = overloadedName;
                                overloadedName = overloadedName.Replace(".", "$").Replace(",", "$").Replace("<", "$").Replace(">", "$").Replace(" ", "");
                                //If we are using shortnames, overload resolution will be handled by the shortname
                                if (!isExtern && !OutputMode.HasFlag(OutputMode.ShortNames))
                                {
                                    //if (!usedMemberNames.TryGetValue(mmethod.ContainingType, out var usedMethodNames))
                                    //{
                                    //    usedMethodNames = new();
                                    //    usedMemberNames[mmethod.ContainingType] = usedMethodNames;
                                    //    //pre populate with every names the base type has used, so they dont get reused, unless overriden
                                    //    var baseType = mmethod.ContainingType.BaseType;
                                    //    if (mmethod.ContainingType.Name == "Boolean")
                                    //    {

                                    //    }
                                    //    while (baseType != null)
                                    //    {
                                    //        if (usedMemberNames.TryGetValue(baseType, out var baseUsedBaseNames))
                                    //        {
                                    //            foreach (var kv in baseUsedBaseNames)
                                    //            {
                                    //                if (!usedMethodNames.TryGetValue(kv.Key, out var upn))
                                    //                {
                                    //                    upn = new HashSet<string>();
                                    //                    usedMethodNames[kv.Key] = upn;
                                    //                }
                                    //                upn.AddRange(kv.Value);
                                    //            }
                                    //            //usedPropertyNames.AddRange(usedBaseNames);
                                    //        }
                                    //        baseType = baseType.BaseType;
                                    //    }
                                    //}
                                    //if (!usedMethodNames.TryGetValue(overloadedName, out var usedName))
                                    //{
                                    //    usedName = new HashSet<string>();
                                    //    usedMethodNames[overloadedName] = usedName;
                                    //}
                                    //if (!usedName.Add(overloadedName))
                                    //{
                                    //    overloadedName = $"{overloadedName}${usedName.Count}";
                                    //    usedName.Add(overloadedName);
                                    //}
                                    int iNameSlug = 1;
                                    string tryName = overloadedName;
                                    HashSet<string>? usedNames = null;
                                    while (IsMemberNameUsed(mmethod.ContainingType, overloadedName, tryName, out usedNames))
                                    {
                                        tryName = $"{overloadedName}${iNameSlug}";
                                        iNameSlug++;
                                    }
                                    overloadedName = tryName;
                                    usedNames.Add(tryName);
                                }
                                //no overload resolution on extern
                                //if (!isExtern)
                                //{
                                //    //If we are using shortnames, overload resolution will be handled by the shortname
                                //    if (!OutputMode.HasFlag(OutputMode.ShortNames))
                                //    {
                                //        if (!usedNames.TryGetValue(originalName, out var usedName))
                                //        {
                                //            usedName = new HashSet<string>();
                                //            usedNames[originalName] = usedName;
                                //        }
                                //        if (!usedName.Add(overloadedName))
                                //        {
                                //            overloadedName = $"{overloadedName}${usedName.Count}";
                                //            usedName.Add(overloadedName);
                                //        }
                                //    }
                                //}
                            }
                            bool export = !hasNameAttribute && !isExtern && !isImportedType;
                            Dictionary<string, SymbolValue> exportNames;
                            if (!export)
                            {
                                exportNames = new Dictionary<string, SymbolValue>();
                            }
                            else if (!Symbols.Members.TryGetValue(declaringTypeMetadata.Signature, out exportNames))
                            {
                                exportNames = new Dictionary<string, SymbolValue>();
                                Symbols.Members.Add(declaringTypeMetadata.Signature, exportNames);
                            }
                            method.OriginalOverloadName = overloadedName;
                            method.OverloadName = SymbolMetadata.ShortName(this, null, null, method.Signature, overloadedName, exportNames, generate: !isExtern, export: export);
                        }
                    }
                }
            });

            SyntaxSymbols = AllSymbols.Where(e => e.Value.DeclaringReferences.Any())
                .SelectMany(e => e.Value.DeclaringReferences.Select(d => (e, d.GetSyntax())))
                .GroupBy(e => e.Item2)
                .ToDictionary(e => e.Key, e => (IEnumerable<SymbolMetadata>)e.Select(ee => ee.e.Value).ToList());
            //TypeSymbols = AllSymbols
            //    .Where(e => e.Value.Symbol is INamespaceOrTypeSymbol)
            //    .ToDictionary(e => e.Key, e => new NamedSymbolCache { Symbol = (INamespaceOrTypeSymbol)e.Value.Symbol });
            $"Collecting extension methods".Profile(() =>
            {
                ExtensionMethods = AllSymbols.Where(e => e.Value.Symbol is IMethodSymbol m && m.IsStatic && m.IsExtensionMethod)
                    .Select(e => (IMethodSymbol)e.Value.Symbol)
                    .GroupBy(e => e.Name)
                    .ToDictionary(e => e.Key, e => (IEnumerable<IMethodSymbol>)e.ToList());
            });

            $"Collecting delegates".Profile(() =>
            {
                Delegates = AllSymbols.Where(e => e.Value.Symbol is ITypeSymbol t && t.TypeKind == TypeKind.Delegate)
                    .Select(e => (ITypeSymbol)e.Value.Symbol)
                    .GroupBy(e => e.Name)
                    .ToDictionary(e => e.Key, e => (IEnumerable<ITypeSymbol>)e.ToList());
            });


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
                                            return new ILLinkerAssembly.Type.Member
                                            {
                                                MemberType = ILLinkerAssembly.Type.MemberType.Field,
                                                Name = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Name)),
                                                Signature = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Signature)),
                                                Body = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Body)),
                                                Value = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Value)),
                                            };
                                        }).ToList(),
                                        Properties = node.Elements("property")
                                        .Select(node =>
                                        {
                                            return new ILLinkerAssembly.Type.Member
                                            {
                                                MemberType = ILLinkerAssembly.Type.MemberType.Property,
                                                Name = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Name)),
                                                Signature = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Signature)),
                                                Body = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Body)),
                                                Value = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Value)),
                                            };
                                        }).ToList(),
                                        Methods = node.Elements("method")
                                        .Select(node =>
                                        {
                                            return new ILLinkerAssembly.Type.Member
                                            {
                                                MemberType = ILLinkerAssembly.Type.MemberType.Method,
                                                Name = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Name)),
                                                Signature = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Signature)),
                                                Body = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Body)),
                                                Value = GetValue(node, nameof(ILLinkerAssembly.Type.Member.Value)),
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
                            Fields = e.Select(ee => new ILLinkerAssembly.Type.Member
                            {
                                MemberType = ILLinkerAssembly.Type.MemberType.Field,
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

            //var i32 = (INamedTypeSymbol)GetTypeSymbol("System.Int32", null);
            //var ibi = (INamedTypeSymbol)GetTypeSymbol("System.Numerics.IBinaryInteger<>", null);

            //var r32 = i32.OutputRank(0);
            //var rbi = ibi.OutputRank(0);
        }

        Dictionary<BaseTypeDeclarationSyntax, string> _fullTypeNameCache = new();
        Dictionary<NamespaceDeclarationSyntax, string> _fullNamespaceCache = new();

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

        public string ResolveFullNamespace(NamespaceDeclarationSyntax type)
        {
            if (_fullNamespaceCache.TryGetValue(type, out var cached))
                return cached;
            string? parent = null;
            if (type.Parent is NamespaceDeclarationSyntax ns)
            {
                parent = ResolveFullNamespace(ns);
            }
            var ret = parent + type.Name.ToString().Trim();
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
            var typeName = type.Identifier.ValueText;
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
            var ret = parent + type.Identifier.ValueText.Trim().TrimEnd('?');
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

        public ISymbol GetSymbol(string typeName, TranslatorSyntaxVisitor? visitor/*, out ISymbol? declaringSymbol, out SymbolKind declaringKind*/)
        {
            var ret = TryGetSymbol(typeName, visitor/*, out declaringSymbol, out declaringKind*/);
            if (ret == null)
            {
                throw new InvalidOperationException($"Cannot find symbol {typeName}");
            }
            return ret;
        }

        public INamedTypeSymbol SystemObject => field ??= (INamedTypeSymbol)GetSymbol("System.Object", null/*, out _, out _*/);
        public INamedTypeSymbol SystemValueType => field ??= (INamedTypeSymbol)GetSymbol("System.ValueType", null/*, out _, out _*/);
        public INamedTypeSymbol SystemBoolean => field ??= (INamedTypeSymbol)GetSymbol("System.Boolean", null/*, out _, out _*/);
        public INamedTypeSymbol SystemChar => field ??= (INamedTypeSymbol)GetSymbol("System.Char", null/*, out _, out _*/);
        public INamedTypeSymbol SystemSByte => field ??= (INamedTypeSymbol)GetSymbol("System.SByte", null/*, out _, out _*/);
        public INamedTypeSymbol SystemByte => field ??= (INamedTypeSymbol)GetSymbol("System.Byte", null/*, out _, out _*/);
        public INamedTypeSymbol SystemString => field ??= (INamedTypeSymbol)GetSymbol("System.String", null/*, out _, out _*/);
        public INamedTypeSymbol SystemInt16 => field ??= (INamedTypeSymbol)GetSymbol("System.Int16", null/*, out _, out _*/);
        public INamedTypeSymbol SystemUInt16 => field ??= (INamedTypeSymbol)GetSymbol("System.UInt16", null/*, out _, out _*/);
        public INamedTypeSymbol SystemInt32 => field ??= (INamedTypeSymbol)GetSymbol("System.Int32", null/*, out _, out _*/);
        public INamedTypeSymbol SystemUInt32 => field ??= (INamedTypeSymbol)GetSymbol("System.UInt32", null/*, out _, out _*/);
        public INamedTypeSymbol SystemInt64 => field ??= (INamedTypeSymbol)GetSymbol("System.Int64", null/*, out _, out _*/);
        public INamedTypeSymbol SystemUInt64 => field ??= (INamedTypeSymbol)GetSymbol("System.UInt64", null/*, out _, out _*/);
        public INamedTypeSymbol SystemDouble => field ??= (INamedTypeSymbol)GetSymbol("System.Double", null/*, out _, out _*/);
        public INamedTypeSymbol SystemSingle => field ??= (INamedTypeSymbol)GetSymbol("System.Single", null/*, out _, out _*/);
        public INamedTypeSymbol SystemArray => field ??= (INamedTypeSymbol)GetSymbol("System.Array", null/*, out _, out _*/);
        public INamedTypeSymbol SystemEnum => field ??= (INamedTypeSymbol)GetSymbol("System.Enum", null/*, out _, out _*/);
        public INamedTypeSymbol SystemType => field ??= (INamedTypeSymbol)GetSymbol("System.Type", null/*, out _, out _*/);
        public INamedTypeSymbol SystemReadOnlySpan => field ??= (INamedTypeSymbol)GetSymbol("System.ReadOnlySpan<>", null);
        public INamedTypeSymbol SystemSpan => field ??= (INamedTypeSymbol)GetSymbol("System.Span<>", null);
        public INamedTypeSymbol SystemRange => field ??= (INamedTypeSymbol)GetSymbol("System.Range", null);
        public INamedTypeSymbol SystemIndex => field ??= (INamedTypeSymbol)GetSymbol("System.Index", null);
        public INamedTypeSymbol SystemDelegate => field ??= (INamedTypeSymbol)GetSymbol("System.Delegate", null);
        public INamedTypeSymbol SystemIEnumerable => field ??= (INamedTypeSymbol)GetSymbol("System.Collections.IEnumerable", null);
        public INamedTypeSymbol SystemIEnumerableT => field ??= (INamedTypeSymbol)GetSymbol("System.Collections.Generic.IEnumerable<>", null);
        public INamedTypeSymbol SystemDynamic => field ??= (INamedTypeSymbol)GetSymbol("System.DynamicType", null);
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

        public INamedTypeSymbol DeletedObject => field ??= (INamedTypeSymbol)GetSymbol("DeletedObject", null/*, out _, out _*/);

        public INamedTypeSymbol AwaitableInterface => field ??= (INamedTypeSymbol)GetSymbol("System.IAwaitable", null);

        public ITypeSymbol Union(IEnumerable<ITypeSymbol> types, TranslatorSyntaxVisitor? visitor)
        {
            var n = types.Count();
            var u = (INamedTypeSymbol)GetSymbol($"dotnetJs.Union<{string.Join(",", Enumerable.Range(1, n).Select(r => ""))}>", visitor/*, out _, out _*/);
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
                        return (INamedTypeSymbol)GetSymbol($"dotnetJs.Action", visitor/*, out _, out _*/);
                    }
                    else
                    {
                        var action = (INamedTypeSymbol)GetSymbol($"dotnetJs.Action<{string.Join(",", Enumerable.Range(1, method.Parameters.Count()).Select(r => ""))}>", visitor/*, out _, out _*/);
                        action = action.Construct(method.Parameters.Select(p => p.Type).ToArray());
                        return action;
                    }
                }
                else
                {
                    var function = (INamedTypeSymbol)GetSymbol($"dotnetJs.Function<{string.Join(",", Enumerable.Range(1, method.Parameters.Count() + 1).Select(r => ""))}>", visitor/*, out _, out _*/);
                    function = function.Construct(method.Parameters.Select(p => p.Type).Concat([method.ReturnType]).ToArray());
                    return function;
                }
            }
            var mfunction = (INamedTypeSymbol)GetSymbol($"dotnetJs.Function<>", visitor/*, out _, out _*/);
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
                var arrayT = (INamedTypeSymbol)GetSymbol("System.Array<>", null/*, out _, out _*/);
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
            (type.TypeSyntaxOrSymbol is TypeSyntax ts ? TryGetSymbol(ts, visitor/*, out declaringType, out declaringKind*/) : type.TypeSyntaxOrSymbol is TypeParameterSyntax tps ? TryGetSymbol(tps.Identifier.ValueText, visitor/*, out declaringType, out declaringKind*/) : type.TypeSyntaxOrSymbol is BaseTypeDeclarationSyntax typ ? GetSymbol(typ, visitor/*, out declaringType, out declaringKind*/) : null);
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
                if (visitor != null && symbol is INamedTypeSymbol ns)
                    visitor.Dependencies.Add(ns);
                if (genericTypes?.Length > 0 && genericTypes.All(e => e.Length > 0))
                {
                    var ps = genericTypes.Select((s, i) => TryGetSymbol(s, visitor, filterCandidate: (s) => s is ITypeSymbol) ?? ((INamedTypeSymbol)symbol).TypeArguments.ElementAt(i)).ToList();
                    if (ps.Any(p => p.Kind != SymbolKind.TypeParameter))
                        symbol = ((INamedTypeSymbol)symbol).Construct(ps.Cast<ITypeSymbol>().ToArray());
                }
                if (isNullable && ((ITypeSymbol)symbol).IsValueType)
                {
                    var nullable = (INamedTypeSymbol)GetSymbol("System.Nullable<>", visitor);
                    return nullable.Construct([(ITypeSymbol)symbol]);
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
                    var tupleElementTypes = tupleInnerTypes.Value.Types.Select(s => (ITypeSymbol)GetSymbol(s, visitor/*, out _, out _*/)).ToArray();
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
            typeName = Utilities.ResolvePredefinedTypeName(typeName);
            var value = AllSymbols.GetValueOrDefault(typeName);
            bool ForEachAssembly(Func<string, bool> iterate, string? prefixTypeName = null)
            {
                var currentAssembly = Compilation.Assembly;
                var slug = GetAssemblyGlobalSlug(currentAssembly);
                var lTypeName = GlobalName + "." + slug + "." + (prefixTypeName != null ? prefixTypeName + "." : "") + typeName;
                var result = iterate(lTypeName);
                if (result)
                    return result;
                lTypeName = slug + "." + (prefixTypeName != null ? prefixTypeName + "." : "") + typeName;
                result = iterate(lTypeName);
                if (result)
                    return result;
                var dependencies = Compilation.SourceModule.ReferencedAssemblySymbols;
                foreach (var dep in dependencies)
                {
                    slug = GetAssemblyGlobalSlug(dep);
                    lTypeName = GlobalName + "." + slug + "." + (prefixTypeName != null ? prefixTypeName + "." : "") + typeName;
                    result = iterate(lTypeName);
                    if (result)
                        return result;
                    lTypeName = slug + "." + (prefixTypeName != null ? prefixTypeName + "." : "") + typeName;
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
            if (expression is ArgumentSyntax arg)
                expression = arg.Expression;
            foreach (var semanticModel in visitor.SemanticModels)
            {
                if (semanticModel.SyntaxTree == expression.SyntaxTree)
                {
                    var ret = semanticModel.GetConstantValue(expression);
                    return ret;
                }
            }
            if (expression is IdentifierNameSyntax id)
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

        List<SyntaxNode> _gettingSyntax = new();
        public ISymbol? TryGetSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor/*, out ISymbol? declaringSymbol, out SymbolKind declaringKind*/)
        {
            if (_gettingSyntax.Contains(syntax))
                return null;
            _gettingSyntax.Add(syntax);
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
                        }
                    }
                }
                if (ret != null)
                {
                    if (visitor != null && ret is INamedTypeSymbol ns)
                        visitor.Dependencies.Add(ns);
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
                        var nullable = (INamedTypeSymbol)GetSymbol("System.Nullable<>", visitor);
                        while (operations.TryPop(out var op))
                        {
                            if (op == TypeWrapOperation.Array)
                                symbol = Compilation.CreateArrayTypeSymbol((ITypeSymbol)symbol);
                            else if (op == TypeWrapOperation.Nullable)
                                symbol = nullable.Construct((ITypeSymbol)symbol);
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
            finally { _gettingSyntax.Remove(syntax); }
        }

        public ITypeSymbol? TryGetTypeSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor/*, out ISymbol? declaringType, out SymbolKind declaringKind*/)
        {
            var ret = TryGetSymbol(syntax, visitor/*, out declaringType, out declaringKind*/);
            if (ret != null)
            {
                return TryGetTypeSymbol(ret);
            }
            return null;
        }

        public ISymbol GetSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor)
        {
            var ret = TryGetSymbol(syntax, visitor/*, out declaringType, out declaringKind*/);
            if (ret == null)
            {
                throw new InvalidOperationException($"Cannot find symbol for syntax");
            }
            return ret;
        }

        public ITypeSymbol GetTypeSymbol(SyntaxNode syntax, TranslatorSyntaxVisitor? visitor)
        {
            var ret = TryGetSymbol(syntax, visitor/*, out declaringType, out declaringKind*/);
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
        bool HasAttachedAttribute(string fullSymbolName, string attributeTypeName, out Dictionary<object, object?> constructorArgs)
        {
            if (!ready)
            {
                constructorArgs = null!;
                return false;
            }
            if (_attachedAttributesCache == null)
            {
                var attachedAttribute = GetSymbol(typeof(AttachedAttribute).FullName!, null/*, out _, out _*/);
                _attachedAttributesCache = Compilation.SourceModule.ReferencedAssemblySymbols.Concat([Compilation.Assembly])
                    .SelectMany(e => e.GetAttributes().Where(a => a.AttributeClass?.Equals(attachedAttribute, SymbolEqualityComparer.Default) ?? false))
                    .GroupBy(e =>
                    {
                        string typeName;
                        var type = e.ConstructorArguments[0].Value;
                        if (type is ITypeSymbol ts)
                        {
                            typeName = ts.CreateSignature(this, withGlobalNamespace: false);
                        }
                        else
                        {
                            typeName = type?.ToString() ?? "";
                        }
                        return typeName;
                    }).ToDictionary(e => e.Key, e =>
                    {
                        return e.Select(a =>
                        {
                            if (a.ConstructorArguments.Length < 2)
                                return (null!, null!);
                            var attType = (ITypeSymbol)a.ConstructorArguments[1].Value!;
                            var ctors = attType.GetMembers(".ctor");
                            var attValues = a.ConstructorArguments.Length > 2 ? a.ConstructorArguments[2].Values.Select((v, i) => ((object)i, v.Value)).ToDictionary(e => e.Item1, e => e.Value) : new Dictionary<object, object?>();
                            return (attType, attValues);
                        }).Where(a => a.attType != null)
                        .ToDictionary(e => e.attType.CreateSignature(this, withGlobalNamespace: false), e => e.attValues);
                    })!;
            }
            var noGeneric = fullSymbolName.RemoveGenericParameterNames(out _);
            if (_attachedAttributesCache.TryGetValue(fullSymbolName, out var values))
            {
                if (values.TryGetValue(attributeTypeName, out var args))
                {
                    constructorArgs = args;
                    return true;
                }
            }
            if (_attachedAttributesCache.TryGetValue(noGeneric, out values))
            {
                if (values.TryGetValue(attributeTypeName, out var args))
                {
                    constructorArgs = args;
                    return true;
                }
            }
            var patternValues = _attachedAttributesCache.FirstOrDefault(p =>
            {
                if (p.Key.StartsWith("*"))
                {
                    //The regex.Match is too costly, use string search to do most of the matching that can simply be matched
                    //Fallback to regex if all fails
                    var pattern = p.Key.Substring(1);
                    var countDotPlus = 0;
                    int where = 0;
                    while (true)
                    {
                        var dotPlus = pattern.IndexOf(".+", where);
                        if (dotPlus < 0)
                            dotPlus = pattern.IndexOf(".?", where);
                        if (dotPlus < 0)
                            break;
                        countDotPlus++;
                        where = dotPlus + 2;
                    }
                    //if (fullSymbolName.Contains("System.Reflection.Emit") && pattern.Contains("System\\.Reflection\\.Emit\\."))
                    //{

                    //}
                    if (countDotPlus == 0)
                        return pattern == fullSymbolName;
                    if (countDotPlus >= 1)
                    {
                        var firstDotPlus = pattern.IndexOf(".+");
                        if (firstDotPlus < 0)
                            firstDotPlus = pattern.IndexOf(".?");
                        if (firstDotPlus >= 0)
                        {
                            var fixedPattern = pattern.Substring(0, firstDotPlus).Replace("\\", "");
                            if (fixedPattern.Length > 0 && !fullSymbolName.StartsWith(fixedPattern))
                            {
                                return false;
                            }
                        }
                        var lastDotPlus = pattern.LastIndexOf(".+");
                        if (lastDotPlus < 0)
                            lastDotPlus = pattern.LastIndexOf(".?");
                        if (lastDotPlus >= 0)
                        {
                            var fixedPattern = pattern.Substring(lastDotPlus + 2, pattern.Length - lastDotPlus - 2).Replace("\\", "");
                            if (fixedPattern.Length > 0 && !fullSymbolName.EndsWith(fixedPattern))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    return Regex.IsMatch(fullSymbolName, pattern);
                }
                return false;
            }).Value;
            if (patternValues != null && patternValues.TryGetValue(attributeTypeName, out var args2))
            {
                constructorArgs = args2;
                return true;
            }
            constructorArgs = null!;
            return false;
        }

        public bool HasAnyAttribute(ISymbol symbol, TranslatorSyntaxVisitor? visitor, bool inherits, params string[] attributeNames)
        {
            if (symbol is ITypeSymbol ts && ts.IsNullable(out var it))
                symbol = it!;
            if (attributeNames.Any(a => HasAttachedAttribute(symbol.CreateSignature(this, withGlobalNamespace: false), a, out _)))
                return true;
            return symbol.HasAnyAttribute(inherits, attributeNames, out _);
            ////var symbols = attributeNames.Select(s => GetTypeSymbol(s, visitor/*, out _, out _*/)).ToList();
            //if (symbol.GetAttributes().Select(a => a.AttributeClass).Where(e => e != null).Any(a =>
            //{
            //    var aName = a!.CreateFullTypeName(this, withGlobalNamespace: false)!;
            //    if (!aName.EndsWith("Attribute"))
            //        aName += "Attribute";
            //    return attributeNames.Contains(aName);
            //    //var aSymbol = TryGetTypeSymbol(aName, visitor/*, out _, out _*/);
            //    //if (aSymbol ==null)
            //    //    return false;
            //    //return symbols.Contains(aSymbol);
            //}))
            //    return true;
            //if (inherits && symbol is ITypeSymbol ns && ns.BaseType != null)
            //{
            //    return HasAnyAttribute(ns.BaseType, visitor, inherits, attributeNames);
            //}
            //if (inherits && symbol is IMethodSymbol ms && ms.OverriddenMethod != null)
            //{
            //    return HasAnyAttribute(ms.OverriddenMethod, visitor, inherits, attributeNames);
            //}
            //if (inherits && symbol is IPropertySymbol ps && ps.OverriddenProperty != null)
            //{
            //    return HasAnyAttribute(ps.OverriddenProperty, visitor, inherits, attributeNames);
            //}
            //return false;
        }

        public bool HasAttribute(ISymbol symbol, string attributeName, TranslatorSyntaxVisitor? visitor, bool inherits, out Dictionary<object, object?> constructorArgs)
        {
            if (symbol.Kind == SymbolKind.Namespace ||
                (Constants.DisableBootClass && attributeName == typeof(BootAttribute).FullName))
            {
                constructorArgs = null;
                return false;
            }
            //constructorArgs = null;
            //if (attributeName == typeof(BootAttribute).FullName)
            //return false;
            if (HasAttachedAttribute(symbol.CreateSignature(this, withGlobalNamespace: false), attributeName, out var args))
            {
                constructorArgs = args;
                return true;
            }
            constructorArgs = null!;
            Dictionary<object, object?> cArgs = new Dictionary<object, object?>();
            var attrSymbol = TryGetSymbol(attributeName, visitor/*, out _, out _*/);
            if (attrSymbol == null)
                return false;
            if (symbol.GetAttributes().Select(a => (a, a.AttributeClass)).Where(e => e.AttributeClass != null).Any(a =>
            {
                var aName = a.AttributeClass!.CreateSignature(this, withGlobalNamespace: false)!;
                if (!aName.EndsWith("Attribute"))
                    aName += "Attribute";
                if (aName != attributeName)
                    return false;
                var aSymbol = a.AttributeClass;// GetTypeSymbol(aName, visitor/*, out _, out _*/);
                if (attrSymbol.Equals(aSymbol, SymbolEqualityComparer.Default))
                {
                    //cArgs = a.a.ConstructorArguments.Select(c => c.Kind == TypedConstantKind.Array ? c.Values : c.Value!).ToArray();
                    cArgs = a.a.ConstructorArguments.Select((c, i) => ((object)i, c.Value))
                    .Concat(a.a.NamedArguments.Select(a => ((object)a.Key, a.Value.Value)))
                    .ToDictionary(kv => kv.Item1, kv => kv.Value);
                    return true;
                }
                return false;
            }))
            {
                constructorArgs = cArgs;
                return true;
            }
            if (inherits && symbol is ITypeSymbol ns && ns.BaseType != null)
            {
                return HasAttribute(ns.BaseType, attributeName, visitor, inherits, out constructorArgs);
            }
            if (inherits && symbol is IMethodSymbol ms && ms.OverriddenMethod != null)
            {
                return HasAttribute(ms.OverriddenMethod, attributeName, visitor, inherits, out constructorArgs);
            }
            if (inherits && symbol is IPropertySymbol ps && ps.OverriddenProperty != null)
            {
                return HasAttribute(ps.OverriddenProperty, attributeName, visitor, inherits, out constructorArgs);
            }
            return false;
        }


        public bool IsExtern(ISymbol type)
        {
            if (type.IsExtern || HasAnyAttribute(type, null, false, typeof(ExternalAttribute).FullName!, typeof(NonScriptableAttribute).FullName!))
                return true;
            if (type.ContainingType != null)
            {
                return IsExtern(type.ContainingType);
            }
            return false;
        }

        //public bool ShouldExportType(MemberDeclarationSyntax node, TranslatorSyntaxVisitor? visitor)
        //{
        //    return !HasAnyAttribute(node, visitor, typeof(ExternalAttribute).FullName, typeof(ExternalInterfaceAttribute).FullName, typeof(NonScriptableAttribute).FullName, typeof(ObjectLiteralAttribute).FullName);
        //}

        public bool IsBootClass(INamedTypeSymbol type)
        {
            bool isBootClass = HasAttribute(type, typeof(BootAttribute).FullName, null, false, out _);
            var parentIsBootClass = type.ContainingType != null ? HasAttribute(type.ContainingType, typeof(BootAttribute).FullName, null, false, out _) : false;
            if (isBootClass || parentIsBootClass)
            {
                if (type.BaseType != null)
                {
                    var systemObject = GetSymbol("System.Object", null/*, out _, out _*/);
                    var systemValueType = GetSymbol("System.ValueType", null/*, out _, out _*/);
                    if (type.BaseType.Equals(systemObject, SymbolEqualityComparer.Default) ||
                        type.BaseType.Equals(systemValueType, SymbolEqualityComparer.Default) ||
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
        }

        public bool IsInlineArray(ISymbol type, out int size, out ITypeSymbol fieldType)
        {
            size = 0;
            fieldType = null!;
            bool isInlineArray = HasAttribute(type, "System.Runtime.CompilerServices.InlineArrayAttribute", null, false, out var args);
            //bool isInlineArray = HasAttribute(type, typeof(InlineArrayAttribute).FullName, null, false, out var args);
            if (isInlineArray)
            {
                size = (int)args[0];
                var field = (IFieldSymbol?)((ITypeSymbol)type).GetMembers().FirstOrDefault();
                if (field != null)
                {
                    fieldType = field.Type;
                }
                return true;
            }
            return false;
        }

        public bool IsAnonymousType(ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType &&
                namedType.TypeKind == TypeKind.Class &&
                namedType.IsAnonymousType) // Or check name for compiler-generated patterns
            {
                return true;
            }
            return false;
        }

        public bool IsAnonymousFunction(ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType &&
                namedType.TypeKind == TypeKind.Delegate &&
                namedType.IsAnonymousType) // Or check name for compiler-generated patterns
            {
                return true;
            }
            return false;
        }

        public bool IsNativeFunction(ISymbol symbol)
        {
            if (IsAnonymousFunction(symbol))
                return true;
            if (HasAttribute(symbol, typeof(NativeDelegateAttribute).FullName, null, false, out _))
                return true;
            return false;
        }

        public bool IsExternal(ISymbol symbol, TranslatorSyntaxVisitor? visitor)
        {
            if (symbol.IsExtern)
                return true;
            if (HasAnyAttribute(symbol, visitor, false, typeof(ExternalAttribute).FullName))
                return true;
            if (symbol.ContainingType != null)
            {
                return IsExternal(symbol.ContainingType, visitor);
            }
            return false;
        }

        public bool ShouldExportType(ISymbol symbol, TranslatorSyntaxVisitor? visitor)
        {
            if (symbol.IsExtern)
                return false;
            //if (IsInlineArray(symbol, out _, out _))  //replace by array type at runtime
            //    return false;
            if (HasAnyAttribute(symbol, visitor, false, typeof(ExternalAttribute).FullName, typeof(NonScriptableAttribute).FullName, typeof(ObjectLiteralAttribute).FullName))
                return false;
            if (symbol.ContainingType != null)
            {
                return ShouldExportType(symbol.ContainingType, visitor);
            }
            return true;
        }

        public bool IsReflectable(ISymbol symbol, TranslatorSyntaxVisitor? visitor, bool skipAssemblyCheck = false)
        {
            var has = HasAttribute(symbol, typeof(ReflectableAttribute).FullName, visitor, false, out var args);
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
                has = HasAttribute(symbol.ContainingAssembly, typeof(ReflectableAttribute).FullName, visitor, false, out args);
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

            if (type is RefTypeSyntax)
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
                case SpecialType.System_ValueType:
                    if (createValueInstance)
                        return $"new {type.ComputeOutputTypeName(this)}()";
                    return null;
            }
            if (type.BaseType?.SpecialType == SpecialType.System_Enum)
            {
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
                if (content is InterpolatedStringTextSyntax sy)
                {
                    result += sy.GetText();
                }
                else if (content is InterpolationSyntax ip)
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

        Dictionary<string, int> pragmas = new Dictionary<string, int>();
        public IDisposable DefinePragma(string keyword)
        {
            int n = 0;
            if (pragmas.TryGetValue(keyword, out n))
            {
                n++;
            }
            else
            {
                n = 1;
            }
            pragmas[keyword] = n;
            return new DelegateDispose(() =>
            {
                n--;
                if (n == 0)
                {
                    pragmas.Remove(keyword);
                }
            });
        }

        public string? Evaluate(string keyword)
        {
            var value = Project.Evaluate(keyword);
            if (string.IsNullOrEmpty(value) && pragmas.TryGetValue(keyword, out var n))
            {
                return n.ToString();
            }
            return value;
        }

        Dictionary<string, string> assemblyGlobalNamespaceCache = new Dictionary<string, string>();
        public string GetAssemblyGlobalSlug(IAssemblySymbol assembly)
        {
            if (assemblyGlobalNamespaceCache.TryGetValue(assembly.Name, out var slug))
                return slug;
            if (HasAttribute(assembly, typeof(AssemblySlugAttribute).FullName, null, false, out var args))
            {
                slug = (string)args![0]!;
            }
            else
            {
                var name = assembly.Name;
                if (name.StartsWith($"{Constants.ProjectName}."))
                    name = name.Substring(6);
                slug = "$" + string.Join("", name.Split('.').Select(c => char.ToLower(c[0])));
                if (assemblyGlobalNamespaceCache.Values.Contains(slug))
                {
                    //throw new InvalidOperationException($"Auto generated global namespace for {assembly} clashes with an existing slug");
                }
            }
            assemblyGlobalNamespaceCache[assembly.Name] = slug;
            return slug;
        }

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
                    var ns = Project.Evaluate("RootNamespace");
                    main = (IMethodSymbol?)TryGetSymbol($"{ns}.Program.Main(System.String[])", null);
                    main ??= (IMethodSymbol?)TryGetSymbol($"{ns}.Program.Main()", null);
                }
                return _main = main;
            }
        }
    }
}