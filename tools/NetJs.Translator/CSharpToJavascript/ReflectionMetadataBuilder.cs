using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NetJs.Translator.CSharpToJavascript
{

    public class ReflectionMetadataBuilder
    {
        public static readonly JsonSerializerOptions SerializationOption = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        GlobalCompilationVisitor global;
        bool isSystemPrivateCoreLib;
        string[] embeddedFiles;
        string[] resxFiles;

        static ulong? NullIfZero(ulong value)
        {
            if (value == 0)
                return null;
            return value;
        }

        static T[]? NullIfEmpty<T>(T[]? value)
        {
            if (value == null)
                return null;
            if (value.Length == 0)
                return null;
            return value;
        }

        public ReflectionMetadataBuilder(GlobalCompilationVisitor global, bool isSystemPrivateCoreLib, string[] resxFiles, string[] embeddedFiles)
        {
            this.global = global;
            this.isSystemPrivateCoreLib = isSystemPrivateCoreLib;
            this.resxFiles = resxFiles;
            this.embeddedFiles = embeddedFiles;
        }

        //static string RemoveGlobal(string? value)
        //{
        //    if (value?.StartsWith("global::") ?? false)
        //        return value.Substring(8);
        //    return value!;
        //}

        uint assemblyHandle;
        ITypeSymbol[] types = default!;
        public Handle NumericTypeHandle(INamedTypeSymbol type)
        {
            if (global.IsDefinedTypeParameter(type))
            {
                var index = int.Parse(type.Name.Substring("T".Length));
                var handle = index + (int)KnownTypeHandle.GenericType1Placeholder;
                return ((ulong)handle << ReflectionHandleExtension.TypeShift);
            }
            int typeHandle = Array.IndexOf(types, type.OriginalDefinition);
            if (typeHandle < 0)
            {
                var nn = type.CreateSignature(global, withGlobalNamespace: true);
                var symbol = global.ImportedNames.Types.GetValueOrDefault(nn);
                if (symbol?.Handle != null)
                {
                    return symbol.Handle.Value;
                }
            }
            if (typeHandle < 0)
                return 0;
            return (assemblyHandle << ReflectionHandleExtension.AssemblyShift) | ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
        }

        public Handle TypeHandle(ITypeSymbol type, TranslatorSyntaxVisitor? fromVisitor)
        {
            //if (!global.ShouldExportType(type, null))
                //return default!;
            if (type.Kind == SymbolKind.TypeParameter)
            {
                ITypeParameterSymbol tp = (ITypeParameterSymbol)type;
                if (tp.DeclaringType != null)
                {
                    return GenericTypeHandle(tp);
                }
                else
                {
                    //Type declared on a method
                    var index = tp.DeclaringMethod!.TypeParameters.IndexOf(tp);
                    return GenericMethodHandle(tp.DeclaringMethod, index);
                }
            }
            if (type is INamedTypeSymbol nt)
            {
                if (nt.IsGenericType)
                {
                    if (fromVisitor != null && SymbolEqualityComparer.Default.Equals(fromVisitor.CurrentTypeSymbol, type))
                    {
                        return $"this.{Constants.PrototypeTypeHandle}";
                    }
                    static IEnumerable<ITypeParameterSymbol> GetTypeArguments(INamedTypeSymbol ts)
                    {
                        foreach (var tts in ts.TypeArguments)
                        {
                            if (tts is ITypeParameterSymbol tp)
                                yield return tp;
                            if (tts is INamedTypeSymbol nt)
                            {
                                foreach (var ttss in GetTypeArguments(nt))
                                    yield return ttss;
                            }
                            else if (tts is IArrayTypeSymbol at)
                            {
                                if (at.ElementType is ITypeParameterSymbol tp2)
                                    yield return tp2;
                                if (at.ElementType is INamedTypeSymbol nt2)
                                    foreach (var ttss in GetTypeArguments(nt2))
                                        yield return ttss;
                            }
                        }
                        if (ts.ContainingType != null)
                        {
                            foreach (var tts in GetTypeArguments(ts.ContainingType))
                                yield return tts;
                        }
                    }
                    var allTypeArgs = GetTypeArguments(nt);
                    IMethodSymbol? declaringMethod = null;
                    if (allTypeArgs.Any(t => (declaringMethod = t.DeclaringMethod) != null))
                    {
                        //at least a generic type argument on this type is defined by method
                        return $"({string.Join(", ", declaringMethod!.TypeParameters.Select(tp => tp.Name))}) => {nt.ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
                    }
                    else
                    {
                        //if (fromVisitor != null && nt.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter))
                        //{
                        //    static IEnumerable<ITypeSymbol> GetTypeArguments(INamedTypeSymbol ts)
                        //    {
                        //        foreach (var tts in ts.TypeArguments)
                        //            yield return tts;
                        //        if (ts.ContainingType != null)
                        //        {
                        //            foreach (var tts in GetTypeArguments(ts.ContainingType))
                        //                yield return tts;
                        //        }
                        //    }
                        //    var ts = nt.TypeArguments;
                        //    var scopTypeArguments = GetTypeArguments(fromVisitor.CurrentTypeSymbol);
                        //    var allTypeArgumentsAreAvailableInScope = nt.TypeArguments.All(t => scopTypeArguments.Contains(t, SymbolEqualityComparer.Default));
                        //    //$.$spc.System.Collections.Generic.HashSet$$(T).AlternateLookup$$(TAlternate) should be $.$spc.System.Collections.Generic.HashSet$$(T).AlternateLookup$$()
                        //    if (!allTypeArgumentsAreAvailableInScope)
                        //    {
                        //        return $"{nt.ConstructUnboundGenericType().ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
                        //    }
                        //}
                        if (nt.ContainingType != null && nt.TypeParameters.Length > 0 && fromVisitor != null && !SymbolEqualityComparer.Default.Equals(nt, fromVisitor.CurrentTypeSymbol))
                        {
                            static IEnumerable<ITypeSymbol> GetScopeTypeArguments(INamedTypeSymbol ts)
                            {
                                foreach (var tts in ts.TypeArguments)
                                    yield return tts;
                                if (ts.ContainingType != null)
                                {
                                    foreach (var tts in GetScopeTypeArguments(ts.ContainingType))
                                        yield return tts;
                                }
                            }
                            var ts = nt.TypeArguments;
                            var scopeTypeArguments = GetScopeTypeArguments(fromVisitor.CurrentTypeSymbol);
                            var allTypeArgumentsAreAvailableInScope = nt.TypeArguments.All(t => scopeTypeArguments.Contains(t, SymbolEqualityComparer.Default));
                            //$.$spc.System.Collections.Generic.HashSet$$(T).AlternateLookup$$(TAlternate) should be $.$spc.System.Collections.Generic.HashSet$$(T).AlternateLookup$$()
                            if (!allTypeArgumentsAreAvailableInScope)
                            {
                                //return $"{nt.ConstructUnboundGenericType().ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
                                return NumericTypeHandle(nt);
                            }
                        }
                        return $"{nt.ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
                    }
                }
                return NumericTypeHandle(nt);
            }
            int typeHandle = Array.IndexOf(types, type.OriginalDefinition);
            if (typeHandle < 0)
            {
                var nn = type.CreateSignature(global, withGlobalNamespace: true);
                var symbol = global.ImportedNames.Types.GetValueOrDefault(nn);
                if (symbol?.Handle != null)
                {
                    return symbol.Handle.Value;
                }
            }
            if (typeHandle < 0)
                return 0;
            return (assemblyHandle << ReflectionHandleExtension.AssemblyShift) | ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
        }

        Handle GenericTypeHandle(int typeIndex)
        {
            var typeHandle = typeIndex + (int)KnownTypeHandle.GenericType1Placeholder;
            return ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
        }

        Handle GenericTypeHandle(ITypeParameterSymbol type)
        {
            var typeIndex = type.DeclaringType!.TypeArguments.IndexOf(type);
            return new Handle() { Expression = $"{type.Name}?.{Constants.PrototypeTypeHandle}??{GenericTypeHandle(typeIndex).Expression}" };
        }

        Handle GenericMethodHandle(IMethodSymbol method, int typeIndex)
        {
            var typeHandle = typeIndex + (int)KnownTypeHandle.GenericMethodType1Placeholder;
            var memberIndex = method.ContainingType.GetMembers().IndexOf(method);
            return ((ulong)typeHandle << ReflectionHandleExtension.TypeShift) |
                ((ulong)memberIndex << ReflectionHandleExtension.MemberShift);
        }

        Handle MemberHandle(ISymbol member, TranslatorSyntaxVisitor? fromVisitor)
        {
            var index = member.ContainingType.GetMembers().IndexOf(member) + 1;
            var handle = TypeHandle(member.ContainingType, fromVisitor);
            if (member.ContainingType.IsGenericType)
                handle = handle.Or($"0x{(ulong)index << ReflectionHandleExtension.MemberShift:X}");
            else
                handle = handle.Or((ulong)index << ReflectionHandleExtension.MemberShift);
            return handle;
        }

        public IEnumerable<INamedTypeSymbol> InitializeForAssembly(IAssemblySymbol assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (!global.HasAttribute(assembly, typeof(AssemblyHandleAttribute).FullName, null, false, out var args))
            {
                //throw new InvalidOperationException("An AssemblyHandleAttribute must be defined on all assembly");
            }
            assemblyHandle = (uint)(args?.ElementAtOrDefault(0).Value ?? (isSystemPrivateCoreLib ? 1 : (uint)new Random().Next(32768, 65536)));
            //uint assemblyHandle = (uint)args[0];
            var types = GetAllTypes(assembly.GlobalNamespace).Concat(assembly.GlobalNamespace
                    .GetNamespaceMembers()
                    .SelectMany(GetAllTypes))
                .Distinct(SymbolEqualityComparer.Default)
                .Cast<INamedTypeSymbol>()
                .ToList();
            //var _typeNames = new string[] { "" }
            //.Concat(Enumerable.Range(1, isSystemPrivateCoreLib ? 32 : 0)
            //.Select(i => $"$T{i}")).Concat(types.Select(t => t.CreateSignature(global, withGlobalNamespace: false))).Distinct();
            //make sure unknown type is index zero, System.Object is at index 1
            this.types = new ITypeSymbol[] { null }.Concat(types).OrderBy(t =>
            t == null ? (int)KnownTypeHandle.Unknown :
            SymbolEqualityComparer.Default.Equals(t, global.SystemVoid) ? (int)KnownTypeHandle.SystemVoid :
            SymbolEqualityComparer.Default.Equals(t, global.SystemObject) ? (int)KnownTypeHandle.SystemObject :
            SymbolEqualityComparer.Default.Equals(t, global.SystemValueType) ? (int)KnownTypeHandle.SystemValueType :
            SymbolEqualityComparer.Default.Equals(t, global.SystemBoolean) ? (int)KnownTypeHandle.SystemBool :
            SymbolEqualityComparer.Default.Equals(t, global.SystemChar) ? (int)KnownTypeHandle.SystemChar :
            SymbolEqualityComparer.Default.Equals(t, global.SystemSByte) ? (int)KnownTypeHandle.SystemSByte :
            SymbolEqualityComparer.Default.Equals(t, global.SystemByte) ? (int)KnownTypeHandle.SystemByte :
            SymbolEqualityComparer.Default.Equals(t, global.SystemInt16) ? (int)KnownTypeHandle.SystemInt16 :
            SymbolEqualityComparer.Default.Equals(t, global.SystemUInt16) ? (int)KnownTypeHandle.SystemUInt16 :
            SymbolEqualityComparer.Default.Equals(t, global.SystemInt32) ? (int)KnownTypeHandle.SystemInt32 :
            SymbolEqualityComparer.Default.Equals(t, global.SystemUInt32) ? (int)KnownTypeHandle.SystemUint32 :
            SymbolEqualityComparer.Default.Equals(t, global.SystemInt64) ? (int)KnownTypeHandle.SystemInt64 :
            SymbolEqualityComparer.Default.Equals(t, global.SystemUInt64) ? (int)KnownTypeHandle.SystemUint64 :
            SymbolEqualityComparer.Default.Equals(t, global.SystemSingle) ? (int)KnownTypeHandle.SystemSingle :
            SymbolEqualityComparer.Default.Equals(t, global.SystemDouble) ? (int)KnownTypeHandle.SystemDouble :
            SymbolEqualityComparer.Default.Equals(t, global.SystemArray) ? (int)KnownTypeHandle.SystemArray :
            SymbolEqualityComparer.Default.Equals(t, global.SystemEnum) ? (int)KnownTypeHandle.SystemEnum :
            SymbolEqualityComparer.Default.Equals(t, global.SystemString) ? (int)KnownTypeHandle.SystemString :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT1) ? (int)KnownTypeHandle.GenericType1Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT2) ? (int)KnownTypeHandle.GenericType2Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT3) ? (int)KnownTypeHandle.GenericType3Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT4) ? (int)KnownTypeHandle.GenericType4Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT5) ? (int)KnownTypeHandle.GenericType5Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT6) ? (int)KnownTypeHandle.GenericType6Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT7) ? (int)KnownTypeHandle.GenericType7Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT8) ? (int)KnownTypeHandle.GenericType8Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT9) ? (int)KnownTypeHandle.GenericType9Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT10) ? (int)KnownTypeHandle.GenericType10Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT11) ? (int)KnownTypeHandle.GenericType11Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT12) ? (int)KnownTypeHandle.GenericType12Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT13) ? (int)KnownTypeHandle.GenericType13Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT14) ? (int)KnownTypeHandle.GenericType14Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT15) ? (int)KnownTypeHandle.GenericType15Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT16) ? (int)KnownTypeHandle.GenericType16Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT17) ? (int)KnownTypeHandle.GenericType17Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT18) ? (int)KnownTypeHandle.GenericType18Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT19) ? (int)KnownTypeHandle.GenericType19Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT20) ? (int)KnownTypeHandle.GenericType20Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT21) ? (int)KnownTypeHandle.GenericType21Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT22) ? (int)KnownTypeHandle.GenericType22Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT23) ? (int)KnownTypeHandle.GenericType23Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT24) ? (int)KnownTypeHandle.GenericType24Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT25) ? (int)KnownTypeHandle.GenericType25Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT26) ? (int)KnownTypeHandle.GenericType26Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT27) ? (int)KnownTypeHandle.GenericType27Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT28) ? (int)KnownTypeHandle.GenericType28Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT29) ? (int)KnownTypeHandle.GenericType29Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT30) ? (int)KnownTypeHandle.GenericType30Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT31) ? (int)KnownTypeHandle.GenericType31Placeholder :
            SymbolEqualityComparer.Default.Equals(t, global.SystemT32) ? (int)KnownTypeHandle.GenericType32Placeholder :
            int.MaxValue).ToArray();
            //var symbolDictionary = global.Symbols.Types.ToDictionary(e => e.Value.Signature, e => e.Value);
            foreach (var type in types)
            {
                var handle = NumericTypeHandle(type);
                var name = type.CreateSignature(global, withTypeParameterNames: true, withGlobalNamespace: true);
                var symbol = global.Symbols.Types.FirstOrDefault(e => e.Value.Signature == name).Value;// symbolDictionary.GetValueOrDefault(name);
                if (symbol != null)
                {
                    symbol.Handle = handle;
                }
            }
            return types;
        }

        public AssemblyModel FromAssemblySymbol(IAssemblySymbol assembly)
        {
            var types = InitializeForAssembly(assembly);
            var model = new AssemblyModel
            {
                AssemblyFlags = global.MainEntry != null ? System.AssemblyFlags.Entry : System.AssemblyFlags.None,
                Handle = assemblyHandle,
                FullName = assembly.Identity.Name.Replace("NetJs.", ""),
                Version = assembly.Identity.Version?.ToString() ?? "0.0.0.0",
                //TypeNames = typeNames,
                //Types = new ITypeSymbol?[] { null }.Concat(types)
                //    .Select(FromTypeSymbol)
                //    .ToArray(),
                Attributes = NullIfEmpty(assembly.GetAttributes()
                    .Where(a => a.AttributeClass != null)
                    .Where(a => global.ShouldExportType(a.AttributeClass!, null))
                    .Select(s => FromAttribute(s, null))
                    .ToArray())
            };
            List<AssemblyManifestModel> manifests = new List<AssemblyManifestModel>();
            //if (resxFiles != null)
            //{
            //foreach (var resx in resxFiles)
            //{
            //    var manifest = new AssemblyManifestModel();
            //    manifest.Name = Path.GetFileNameWithoutExtension(resx);
            //    var xml = File.ReadAllText(resx);
            //    var doc = XElement.Parse(xml);
            //    var result = doc.Elements("data").Where(r => r.Attribute("name") is not null && r.Element("value") is not null).ToDictionary(e => e.Attribute("name").Value, e => e.Element("value").Value);
            //    manifest.StringResourceData = result;
            //    manifests.Add(manifest);
            //}
            foreach (var resx in resxFiles.Concat(embeddedFiles.Where(e => e.EndsWith(".resx"))).Distinct())
            {
                var stream = new MemoryStream();
                var resourceWriter = new ResourceWriter(stream);
                var manifest = new AssemblyManifestModel();
                manifest.Name = assembly.Name + "." + Path.GetFileNameWithoutExtension(resx) + ".resources";
                var xml = File.ReadAllText(resx);
                var doc = XElement.Parse(xml);
                var result = doc.Elements("data").Where(r => r.Attribute("name") is not null && r.Element("value") is not null).ToDictionary(e => e.Attribute("name").Value, e => e.Element("value").Value);
                foreach (var kv in result)
                {
                    resourceWriter.AddResource(kv.Key, kv.Value);
                }
                resourceWriter.Close();
                manifest.Data = Convert.ToBase64String(stream.ToArray());
                manifests.Add(manifest);
            }
            //}
            //if (embeddedFiles != null)
            //{
            foreach (var file in embeddedFiles.Where(e => !e.EndsWith(".resx")))
            {
                var manifest = new AssemblyManifestModel();
                manifest.Name = Path.GetFileNameWithoutExtension(file);
                var data = File.ReadAllBytes(file);
                var dataLen = data.Length;
                var finalData = new byte[4 + data.Length];
                finalData[0] = (byte)((dataLen >> 0) & 0xFF);
                finalData[1] = (byte)((dataLen >> 8) & 0xFF);
                finalData[2] = (byte)((dataLen >> 16) & 0xFF);
                finalData[3] = (byte)((dataLen >> 24) & 0xFF);
                Array.Copy(data, 0, finalData, 4, dataLen);
                manifest.Data = Convert.ToBase64String(finalData);
                manifests.Add(manifest);
            }
            //}
            model.Manifests = manifests.ToArray();
            return model;
        }

        public TypeModel FromTypeSymbol(ITypeSymbol? symbol, TranslatorSyntaxVisitor? fromVisitor, bool minimal = false)
        {
            if (symbol == null) return new TypeModel { };
            var model = new TypeModel
            {
                //Name = symbol.Name,
                Handle = TypeHandle(symbol, fromVisitor: fromVisitor),
                //AssemblyQualifiedName = $"{RemoveGlobal(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}, {symbol.ContainingAssembly?.Name}",
                BaseType = symbol.BaseType != null ? NullIfZero(TypeHandle(symbol.BaseType, fromVisitor: fromVisitor)) : null,
                DeclaringType = symbol.ContainingType != null ? TypeHandle(symbol.ContainingType, fromVisitor: fromVisitor) : default,
                UnderlyingType = (symbol is INamedTypeSymbol nt && nt.EnumUnderlyingType != null)
                    ? TypeHandle(nt.EnumUnderlyingType, fromVisitor: fromVisitor)
                    : default,
                Kind = global.MapTypeKind(symbol.TypeKind),
                Flags = global.GetTypeFlags(symbol),
                //TypeAttributes = 0,
                KnownType = global. KnownTypeFromName(symbol.CreateSignature(global, withGlobalNamespace: false)),
                Properties = minimal ? null : NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                            .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .OfType<IPropertySymbol>().Select(e => FromPropertySymbol(e, fromVisitor)).ToArray()),
                Methods = minimal ? null : NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                            .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .OfType<IMethodSymbol>()
                            .Where(m => m.MethodKind == MethodKind.Ordinary || m.MethodKind == MethodKind.DelegateInvoke)
                            .Where(m => !global.LinkTrimOutMethod(m))
                            .Select(e => FromMethodSymbol(e, fromVisitor))
                            .ToArray()),
                Constructors = minimal ? null : NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                            .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .OfType<IMethodSymbol>()
                            .Where(m => m.MethodKind == MethodKind.Constructor)
                            .Where(m => !global.LinkTrimOutMethod(m))
                            .Select(e => FromConstructorSymbol(e, fromVisitor)).ToArray()),
                Fields = minimal ? null : NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                            .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .Where(m => !m.Name.Contains("k__BackingField")/*Property backing fields are not needed*/)
                            .OfType<IFieldSymbol>().Select(e => FromFieldSymbol(e, fromVisitor)).ToArray()),
                Events = minimal ? null : NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                             .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .OfType<IEventSymbol>().Select(e => FromEventSymbol(e, fromVisitor)).ToArray()),
                Interfaces = minimal ? null : NullIfEmpty(symbol.AllInterfaces.Where(i => global.ShouldExportType(i, null)).Select(i => TypeHandle(i, fromVisitor: fromVisitor)).ToArray()),
                Attributes = minimal ? null : NullIfEmpty(symbol.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a, fromVisitor)).ToArray()),
                GenericArguments = minimal ? null : NullIfEmpty(symbol is INamedTypeSymbol g && g.TypeParameters.Any()
                    ? g.TypeParameters.Select((t, i) =>
                    {
                        //var handle = TypeHandle(t);
                        //if (handle == 0)
                        //{
                        var handle = GenericTypeHandle(t);
                        //}
                        return handle;
                    }).ToArray()
                    : Array.Empty<Handle>()),
                GenericConstraints = minimal ? null : NullIfEmpty(Array.Empty<GenericParameterConstraintModel>()),
                NestedTypes = minimal ? null : NullIfEmpty(symbol.GetTypeMembers().Where(t => global.ShouldExportType(t, null) && global.IsReflectable(t, null)).Select(t => TypeHandle(t, fromVisitor)).ToArray()),
                GenericParameterCount = symbol is INamedTypeSymbol ng ? ng.TypeParameters.Length : 0,
                Size = global.SizeOf(symbol)
            };

            return model;
        }

        static string ReplaceWholeWord(string input, string wordToReplace, string replacement)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(wordToReplace))
                return input;

            // \b anchors match word boundaries (e.g., spaces, punctuation, start/end of string)
            string pattern = $@"\b{Regex.Escape(wordToReplace)}\b";

            return Regex.Replace(input, pattern, replacement);
        }

        public string FromTypeSymbolAsJson(ITypeSymbol? symbol, TranslatorSyntaxVisitor? fromVisitor, bool minimal = false)
        {
            var model = FromTypeSymbol(symbol, fromVisitor, minimal);
            var json = JsonSerializer.Serialize(model, SerializationOption);
            //if (symbol is INamedTypeSymbol nt && nt.OriginalDefinition.IsGenericType)
            //{
            //    var selfHandle = TypeHandle(nt);
            //    json = ReplaceWholeWord(json, $"{selfHandle}", $"this.{Constants.PrototypeTypeHandle}");
            //    foreach (var t in nt.OriginalDefinition.TypeParameters) //replace every handle in the json with the runtime handle of the generic type
            //    {
            //        var handle = GenericTypeHandle(t);
            //        var needle = $"{handle}";
            //        json = ReplaceWholeWord(json, needle, $"{t.Name}?.{Constants.PrototypeMetadata}?.h??{handle}");
            //    }
            //}
            return json;
        }
        // --- Internal helpers ---
        private IEnumerable<INamedTypeSymbol> GetInnerTypes(ITypeSymbol ns)
        {
            foreach (var nested in ns.GetTypeMembers())
            {
                if (global.ShouldExportType(nested, null) /*&& global.IsReflectable(nested, null)*/)
                    yield return nested;
                foreach (var inner in GetInnerTypes(nested))
                    if (global.ShouldExportType(inner, null)/* && global.IsReflectable(inner, null)*/)
                        yield return inner;
            }
        }
        private IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
        {
            foreach (var type in ns.GetTypeMembers())
            {
                if (global.ShouldExportType(type, null) /*&& global.IsReflectable(type, null)*/)
                    yield return type;
                foreach (var inner in GetInnerTypes(type))
                    yield return inner;
            }
            foreach (var nested in ns.GetNamespaceMembers())
            {
                foreach (var inner in GetAllTypes(nested))
                    //if (global.ShouldExportType(inner, null) && global.IsReflectable(inner, null))
                    yield return inner;
            }
        }

        //private static TypeAttributes GetTypeAttributes(ITypeSymbol type)
        //{
        //    var flags = (CoreLibTypeAttributes)0;

        //    if (type.DeclaredAccessibility == Accessibility.Public)
        //        flags |= CoreLibTypeAttributes.Public;
        //    if (type.IsAbstract)
        //        flags |= CoreLibTypeAttributes.Abstract;
        //    if (type.IsSealed)
        //        flags |= CoreLibTypeAttributes.Sealed;
        //    if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Interface)
        //        flags |= CoreLibTypeAttributes.Interface;
        //    if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
        //        flags |= CoreLibTypeAttributes.Enum;
        //    if (type.IsValueType)
        //        flags |= TypeFlagsModel.IsValueType;
        //    if (type is INamedTypeSymbol named && named.IsGenericType)
        //        flags |= TypeFlagsModel.IsGenericType;
        //    if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Class)
        //        flags |= TypeFlagsModel.IsClass;
        //    if (type.BaseType?.Name == "Enum" && type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
        //        flags |= TypeFlagsModel.IsFlags;
        //    if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array)
        //        flags |= TypeFlagsModel.IsArray;
        //    if (type.ContainingSymbol.Kind == Microsoft.CodeAnalysis.SymbolKind.NamedType)
        //        flags |= TypeFlagsModel.IsNested;

        //    return flags;
        //}

        PropertyModel FromPropertySymbol(IPropertySymbol prop, TranslatorSyntaxVisitor? fromVisitor)
        {
            var name = prop.Name;
            //a method that implements explicitly will have a long name qualified by the interface it is defined on
            //Let's shrink it by removing the interface name
            if (prop.ExplicitInterfaceImplementations.Any())
            {
                var ex = prop.ExplicitInterfaceImplementations.First().ContainingType;
                var handle = TypeHandle(ex, fromVisitor);
                name = (handle.Expression != null ? $"{{{handle}}}." : "") + name.Split('.').Last();
            }
            if (name == "this[]")
            {
                if (global.HasAttribute(prop, typeof(IndexerNameAttribute).FullName, null, false, out var args))
                {
                    name = (string)args[0];
                }
                else
                {
                    name = "Item";
                }
            }
            var metadata = global.GetRequiredMetadata(prop);
            var outputName = metadata.OverloadName ?? name;
            var propertyTypeHandle = !global.ShouldExportType(prop.Type, null) ? default : TypeHandle(prop.Type, fromVisitor);
            //if (propertyTypeHandle == 0 && prop.ContainingType.Arity > 0)
            //{
            //    var args = prop.ContainingType.TypeArguments;
            //    var index = args.IndexOf(prop.Type, 0, SymbolEqualityComparer.Default);
            //    if (index >= 0)
            //        propertyTypeHandle = GenericTypeHandle(index);
            //}
            return new PropertyModel
            {
                Name = name,
                OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
                DeclaringType = TypeHandle(prop.ContainingType, fromVisitor),
                Flags = prop.GetSymbolFlags(),
                PropertyType = propertyTypeHandle,
                IndexParameters = NullIfEmpty(prop.Parameters.Select(e => FromParameterSymbol(e, fromVisitor)).ToArray()),
                GetMethod = prop.GetMethod != null ? FromMethodSymbol(prop.GetMethod, fromVisitor, prop) : null,
                SetMethod = prop.SetMethod != null ? FromMethodSymbol(prop.SetMethod, fromVisitor, prop) : null,
                Handle = MemberHandle(prop, fromVisitor),
                Attributes = NullIfEmpty(prop.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a, fromVisitor)).ToArray())
            };
        }

        static string? NullIfVoid(string t)
        {
            if (t == "void")
                return null;
            return t;
        }

        MethodModel FromMethodSymbol(IMethodSymbol method, TranslatorSyntaxVisitor? fromVisitor, IPropertySymbol? fromProperty = null)
        {
            var name = method.Name;
            //a method that implements explicitly will have a long name qualified by the interface it is defined on
            //Let's shrink it by removing the interface name
            if (method.ExplicitInterfaceImplementations.Any())
            {
                var ex = method.ExplicitInterfaceImplementations.First().ContainingType;
                var handle = TypeHandle(ex, fromVisitor);
                name = (handle > 0 ? $"{{{handle}}}." : "") + name.Split('.').Last();
            }
            var metadata = global.GetRequiredMetadata(method);
            var outputName = metadata.OverloadName ?? name;
            var methodReturnTypeHandle = !global.ShouldExportType(method.ReturnType, null) ? default : TypeHandle(method.ReturnType, fromVisitor);
            //if (methodReturnTypeHandle == 0 && method.ContainingType.Arity > 0)
            //{
            //    var args = method.ContainingType.TypeArguments;
            //    var index = args.IndexOf(method.ReturnType, 0, SymbolEqualityComparer.Default);
            //    if (index >= 0)
            //        methodReturnTypeHandle = GenericTypeHandle(index);
            //}
            return new MethodModel
            {
                Name = fromProperty == null ? name : null!,
                OutputName = fromProperty == null ? (outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null) : null,
                DeclaringType = fromProperty == null ? TypeHandle(method.ContainingType, fromVisitor) : 0,
                Flags = method.GetSymbolFlags(),
                ReturnType = fromProperty == null ? methodReturnTypeHandle : 0,
                Parameters = fromProperty == null ? NullIfEmpty(method.Parameters.Select(e => FromParameterSymbol(e, fromVisitor)).ToArray()) : null,
                GenericArguments = fromProperty == null ? NullIfEmpty(method.TypeArguments.Select(t => !global.ShouldExportType(t, null) ? "object" : t.CreateSignature(global, withGlobalNamespace: false)).ToArray()) : null,
                Handle = MemberHandle(method, fromVisitor),
                Attributes = NullIfEmpty(method.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a, fromVisitor)).ToArray())
            };
        }

        ConstructorModel FromConstructorSymbol(IMethodSymbol ctor, TranslatorSyntaxVisitor? fromVisitor)
        {
            var metadata = global.GetRequiredMetadata(ctor);
            var name = ctor.Name;
            var outputName = metadata.OverloadName ?? name;
            return new ConstructorModel
            {
                Name = name,
                OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
                DeclaringType = TypeHandle(ctor.ContainingType, fromVisitor),
                Flags = ctor.GetSymbolFlags(),
                Parameters = NullIfEmpty(ctor.Parameters.Select(e => FromParameterSymbol(e, fromVisitor)).ToArray()),
                Handle = MemberHandle(ctor, fromVisitor),
                Attributes = NullIfEmpty(ctor.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a, fromVisitor)).ToArray())
            };
        }

        FieldModel FromFieldSymbol(IFieldSymbol field, TranslatorSyntaxVisitor? fromVisitor)
        {
            var metadata = global.GetRequiredMetadata(field);
            var name = field.Name;
            var outputName = metadata.OverloadName ?? name;
            var fieldTypeHandle = !global.ShouldExportType(field.Type, null) ? default : TypeHandle(field.Type, fromVisitor);
            //if (fieldTypeHandle == 0 && field.ContainingType.Arity > 0)
            //{
            //    var args = field.ContainingType.TypeArguments;
            //    var index = args.IndexOf(field.Type, 0, SymbolEqualityComparer.Default);
            //    if (index >= 0)
            //        fieldTypeHandle = GenericTypeHandle(index);
            //}
            return new FieldModel
            {
                Name = name,
                OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
                DeclaringType = !global.ShouldExportType(field.Type, null) ? default : TypeHandle(field.ContainingType, fromVisitor),
                Flags = field.GetSymbolFlags(),
                FieldType = fieldTypeHandle,
                Handle = MemberHandle(field, fromVisitor),
                Attributes = NullIfEmpty(field.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a, fromVisitor)).ToArray())
            };
        }

        EventModel FromEventSymbol(IEventSymbol ev, TranslatorSyntaxVisitor? fromVisitor)
        {
            var metadata = global.GetRequiredMetadata(ev);
            var name = ev.Name;
            var outputName = metadata.OverloadName ?? name;
            var eventTypeHandle = !global.ShouldExportType(ev.Type, null) ? default : TypeHandle(ev.Type, fromVisitor);
            //if (eventTypeHandle == 0 && ev.ContainingType.Arity > 0)
            //{
            //    var args = ev.ContainingType.TypeArguments;
            //    var index = args.IndexOf(ev.Type, 0, SymbolEqualityComparer.Default);
            //    if (index >= 0)
            //        eventTypeHandle = GenericTypeHandle(index);
            //}
            return new EventModel
            {
                Name = name,
                OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
                DeclaringType = TypeHandle(ev.ContainingType, fromVisitor),
                Flags = ev.GetSymbolFlags(),
                EventHandlerType = !global.ShouldExportType(ev.Type, null) ? default : TypeHandle(ev.Type, fromVisitor),
                AddMethod = ev.AddMethod != null ? FromMethodSymbol(ev.AddMethod, fromVisitor) : null,
                RemoveMethod = ev.RemoveMethod != null ? FromMethodSymbol(ev.RemoveMethod, fromVisitor) : null,
                RaiseMethod = ev.RaiseMethod != null ? FromMethodSymbol(ev.RaiseMethod, fromVisitor) : null,
                Handle = MemberHandle(ev, fromVisitor),
                Attributes = NullIfEmpty(ev.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a, fromVisitor)).ToArray())
            };
        }

        ParameterModel FromParameterSymbol(IParameterSymbol param, TranslatorSyntaxVisitor? fromVisitor)
        {
            var paramTypeHandle = !global.ShouldExportType(param.Type, null) ? default : TypeHandle(param.Type, fromVisitor);
            //if (paramTypeHandle == 0 && param.ContainingType.Arity > 0)
            //{
            //    var args = param.ContainingType.TypeArguments;
            //    var index = args.IndexOf(param.Type, 0, SymbolEqualityComparer.Default);
            //    if (index >= 0)
            //        paramTypeHandle = GenericTypeHandle(index);
            //}
            return new ParameterModel
            {
                Name = param.Name,
                ParameterType = paramTypeHandle,
                //Position = param.Ordinal,
                Flags =
                (param.IsOptional ? ParameterFlagsModel.Optional : ParameterFlagsModel.None) |
                (param.RefKind == RefKind.Out ? ParameterFlagsModel.Out : ParameterFlagsModel.None) |
                (param.RefKind == RefKind.Ref ? ParameterFlagsModel.Ref : ParameterFlagsModel.None) |
                (param.IsParams ? ParameterFlagsModel.Params : ParameterFlagsModel.None),
                DefaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue ?? "__typeDefault__" : null,
                Attributes = NullIfEmpty(param.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a, fromVisitor)).ToArray())
            };
        }

        object? AdaptAttrValue(object? a, TranslatorSyntaxVisitor? fromVisitor)
        {
            //if (a is not string && a is not byte && a is not short && a is not int && a is not long && a is not bool && a is not ITypeSymbol && a is not TypedConstant && a is not IEnumerable<ITypeSymbol> && a is not IEnumerable<TypedConstant>)
            //{

            //}
            if (a == null)
                return null;
            if (a is ITypeSymbol t)
                return TypeHandle(t, fromVisitor);
            if (a is TypedConstant tc)
                return TypeHandle(tc.Type!, fromVisitor);
            if (a is IEnumerable<ITypeSymbol> tt)
                return tt.Select(t => TypeHandle(t, fromVisitor));
            if (a is IEnumerable<TypedConstant> tcc)
                return tcc.Select(t => TypeHandle(t.Type!, fromVisitor));
            return a;
        }

        AttributeModel FromAttribute(AttributeData att, TranslatorSyntaxVisitor? fromVisitor)
        {
            return new AttributeModel
            {
                TypeHandle = att.AttributeClass == null ? default : TypeHandle(att.AttributeClass, fromVisitor),
                ConstructorHandle = att.AttributeConstructor == null ? default : MemberHandle(att.AttributeConstructor, fromVisitor),
                ConstructorArguments = NullIfEmpty(att.ConstructorArguments.Select(arg => new AttributeConstructorArgumentModel
                {
                    Type = arg.Type != null ? TypeHandle(arg.Type, fromVisitor) : default,
                    Value = AdaptAttrValue(arg.Kind == TypedConstantKind.Array ? arg.Values : arg.Value, fromVisitor),
                }).ToArray()),
                NamedArguments = NullIfEmpty(att.NamedArguments.Select(arg => new AttributeNamedArgumentModel
                {
                    Name = arg.Key,
                    Type = arg.Value.Type != null ? TypeHandle(arg.Value.Type, fromVisitor) : default,
                    Value = AdaptAttrValue(arg.Value.Kind == TypedConstantKind.Array ? arg.Value.Values : arg.Value.Value, fromVisitor),
                }).ToArray())
            };
        }

    }
}
