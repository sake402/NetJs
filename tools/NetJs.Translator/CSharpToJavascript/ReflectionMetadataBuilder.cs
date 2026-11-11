using Microsoft.CodeAnalysis;
using System.Diagnostics;
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
        static List<int> usedAssemblyHandle = new();
        public static readonly JsonSerializerOptions SerializationOption = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            WriteIndented = true,
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
        Dictionary<ITypeSymbol, int> types = default!;
        public Handle NumericTypeHandle(ITypeSymbol type, bool useCachedHandle = true)
        {
            //if (global.IsDefinedTypeParameter(type))
            //{
            //    var index = int.Parse(type.Name.Substring("T".Length));
            //    var handle = index + (int)KnownTypeHandle.GenericType1Placeholder;
            //    return (assemblyHandle << ReflectionHandleExtension.AssemblyShift) | ((ulong)handle << ReflectionHandleExtension.TypeShift);
            //}
            if (useCachedHandle)
            {
                var metadata = global.GetMetadata(type);
                if (metadata?.Export.Handle != null)
                    return metadata.Export.Handle.Value;
                //var typeSignature = type.CreateSignature(global, withGlobalNamespace: true);
                //var sym = global.Symbols.Types.FirstOrDefault(S => S.Value.Signature == typeSignature).Value;
                //if (sym != null && sym.Handle != null)
                //    return sym.Handle.Value;
                ////if (global.Symbols.Types.TryGetValue(typeSignature, out var symbol) && symbol?.Handle != null)
                ////{
                ////    return symbol.Handle.Value;
                ////}
                //sym = global.ImportedNames.Types.FirstOrDefault(S => S.Value.Signature == typeSignature).Value;
                //if (sym != null && sym.Handle != null)
                //    return sym.Handle.Value;

                ////if (global.ImportedNames.Types.TryGetValue(typeSignature, out symbol) && symbol?.Handle != null)
                ////{
                ////    return symbol.Handle.Value;
                ////}
            }
            if (types!.TryGetValue(type.OriginalDefinition, out int typeHandle))
            {
                Debug.Assert(typeHandle < 0x10000);
                return (assemblyHandle << ReflectionHandleExtension.AssemblyShift) |
                       ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
            }
            return 0;
        }

        Handle MemberHandle(ISymbol member, TranslatorSyntaxVisitor? fromVisitor, bool useCachedHandle = true, bool numericHandle = false)
        {
            var containingType = member.ContainingType;
            var typeHandle = numericHandle ? NumericTypeHandle(containingType, useCachedHandle) : TypeHandle(containingType, fromVisitor);

            if (useCachedHandle)
            {
                var metadata = global.GetMetadata(member);
                if (metadata?.Export.Handle != null/* && metadata.OverloadName != null*/)
                {
                    var lmemberHandle = metadata.Export.Handle.Value;
                    if (!numericHandle && containingType.IsGenericType)
                    {
                        return typeHandle.Or($"0x{lmemberHandle & ~(ReflectionHandleExtension.AssemblyMask | ReflectionHandleExtension.TypeMask):X}");
                    }
                    return lmemberHandle;
                }
            }
            int index = -1;
            var allMembers = containingType.GetMembers().SelectMany(e =>
            {
                if (e.Kind == SymbolKind.Property && e is IPropertySymbol ps)
                {
                    return ((IEnumerable<ISymbol?>)[ps, ps.GetMethod, ps.SetMethod]).Where(e => e != null)!;
                }
                else if (e.Kind == SymbolKind.Event && e is IEventSymbol es)
                {
                    return ((IEnumerable<ISymbol?>)[es, es.AddMethod, es.RemoveMethod]).Where(e => e != null)!;
                }
                return [e];
            }).OrderBy(e => e.Name);
            int i = 0;
            foreach (var imember in allMembers)
            {
                if (imember!.Equals(member, SymbolEqualityComparer.Default))
                {
                    index = i + 1;
                    break;
                }
                i++;
            }

            if (index <= 0)
            {
                return 0;
            }

            ulong memberHandle = (ulong)index << ReflectionHandleExtension.MemberShift;

            if (!numericHandle && containingType.IsGenericType)
            {
                typeHandle = typeHandle.Or($"0x{memberHandle:X}");
            }
            else
            {
                typeHandle = typeHandle.Or(memberHandle);
            }

            return typeHandle;
        }

        public Handle TypeHandle(ITypeSymbol type, TranslatorSyntaxVisitor? fromVisitor)
        {
            if (type == null)
                return default;

            if (!global.ShouldExportType(type, fromVisitor))
                return default;

            bool isOriginalType = SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, type);
            if (type.Kind == SymbolKind.NamedType && ((INamedTypeSymbol)type).Arity == 0 && isOriginalType)
                return NumericTypeHandle((INamedTypeSymbol)type);

            IMethodSymbol? GetDeclaringMethodOfAnyTypeParameter(ITypeSymbol nt)
            {
                IMethodSymbol? declaringMethod = null;
                var argQueue = new Queue<ITypeSymbol>();
                argQueue.Enqueue(nt);

                while (argQueue.Count > 0)
                {
                    var current = argQueue.Dequeue();
                    if (current is INamedTypeSymbol currentNamed)
                    {
                        var typeArgs = currentNamed.TypeArguments;
                        for (int i = 0; i < typeArgs.Length; i++)
                        {
                            var ta = typeArgs[i];
                            if (ta is ITypeParameterSymbol tpArg)
                            {
                                if (tpArg.DeclaringMethod != null)
                                {
                                    declaringMethod = tpArg.DeclaringMethod;
                                    break;
                                }
                            }
                            else if (ta is INamedTypeSymbol or IArrayTypeSymbol)
                            {
                                argQueue.Enqueue(ta);
                            }
                        }

                        if (declaringMethod != null) break;

                        if (currentNamed.ContainingType != null)
                        {
                            argQueue.Enqueue(currentNamed.ContainingType);
                        }
                    }
                    else if (current is IArrayTypeSymbol arrayType)
                    {
                        var elem = arrayType.ElementType;
                        if (elem is ITypeParameterSymbol tpArg && tpArg.DeclaringMethod != null)
                        {
                            declaringMethod = tpArg.DeclaringMethod;
                            break;
                        }
                        else if (elem is INamedTypeSymbol or IArrayTypeSymbol)
                        {
                            argQueue.Enqueue(elem);
                        }
                    }
                }
                return declaringMethod;
            }

            bool isPointer = false;
            ITypeSymbol elementType;
            if (
                (type.IsArray(out elementType) && global.ShouldExportType(elementType, fromVisitor)) ||
                (isPointer = type.IsPointer(out elementType) && global.ShouldExportType(elementType, fromVisitor))
                )
            {
                string? prefix = null;
                if (elementType.TypeKind == TypeKind.TypeParameter)
                {
                    var t = (ITypeParameterSymbol)elementType;
                    if (t.DeclaringMethod != null)
                    {
                        var typeParams = t.DeclaringMethod.TypeParameters;
                        var paramNames = new string[typeParams.Length];
                        for (int i = 0; i < typeParams.Length; i++)
                        {
                            paramNames[i] = typeParams[i].Name;
                        }
                        prefix = $"({string.Join(", ", paramNames)}) => ";
                    }
                }
                else
                {
                    IMethodSymbol? declaringMethod = GetDeclaringMethodOfAnyTypeParameter(elementType);
                    if (declaringMethod != null)
                    {
                        //return GenericMethodTypeParameterHandle(declaringMethod, nt);
                        var typeParams = declaringMethod.TypeParameters;
                        var paramNames = new string[typeParams.Length];
                        for (int i = 0; i < typeParams.Length; i++)
                        {
                            paramNames[i] = typeParams[i].Name;
                        }
                        prefix = $"({string.Join(", ", paramNames)}) => ";
                    }
                }
                return $"{prefix}{global.GlobalName}.{(isPointer ? Constants.TypePointerName : Constants.TypeArrayName)}({elementType.ComputeOutputTypeName(global)}).{Constants.PrototypeTypeHandle}";
            }

            if (type.Kind == SymbolKind.TypeParameter)
            {
                ITypeParameterSymbol tp = (ITypeParameterSymbol)type;
                if (tp.DeclaringType != null)
                {
                    return GenericTypeHandle(tp);
                }
                else
                {
                    var declaringMethod = tp.DeclaringMethod!;
                    return GenericMethodTypeParameterHandle(declaringMethod, tp);
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

                    IMethodSymbol? declaringMethod = GetDeclaringMethodOfAnyTypeParameter(nt);

                    if (declaringMethod != null)
                    {
                        var typeParams = declaringMethod.TypeParameters;
                        var paramNames = new string[typeParams.Length];
                        for (int i = 0; i < typeParams.Length; i++)
                        {
                            paramNames[i] = typeParams[i].Name;
                        }
                        return $"({string.Join(", ", paramNames)}) => {nt.ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
                    }

                    if (nt.ContainingType != null &&
                        fromVisitor != null &&
                        !SymbolEqualityComparer.Default.Equals(nt, fromVisitor.CurrentTypeSymbol))
                    {
                        var scopeArgs = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
                        var currentScope = fromVisitor.CurrentTypeSymbol;
                        while (currentScope != null)
                        {
                            var typeArgs = currentScope.TypeArguments;
                            for (int i = 0; i < typeArgs.Length; i++)
                            {
                                scopeArgs.Add(typeArgs[i]);
                            }
                            currentScope = currentScope.ContainingType;
                        }

                        bool allTypeArgumentsAreAvailableInScope = true;
                        var ntArgs = nt.TypeArguments;
                        for (int i = 0; i < ntArgs.Length; i++)
                        {
                            if (!scopeArgs.Contains(ntArgs[i]))
                            {
                                allTypeArgumentsAreAvailableInScope = false;
                                break;
                            }
                        }

                        if (!allTypeArgumentsAreAvailableInScope)
                        {
                            return NumericTypeHandle(nt);
                        }
                    }
                    var isOpenGeneric = nt.TypeArguments.All(e => e.Kind == SymbolKind.ErrorType);
                    if (!isOpenGeneric && !SymbolEqualityComparer.Default.Equals(nt, nt.OriginalDefinition))
                        return $"{nt.ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
                }

                return NumericTypeHandle(nt);
            }

            if (types.TryGetValue(type.OriginalDefinition, out int typeHandle))
            {
                var nn = type.CreateSignature(global, withGlobalNamespace: true);
                if (global.ImportedSymbols.Types.TryGetValue(nn, out var symbol) && symbol?.Handle != null)
                {
                    return symbol.Handle.Value;
                }

                return (assemblyHandle << ReflectionHandleExtension.AssemblyShift) |
                       ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
            }

            return 0;
        }

        //public Handle TypeHandle(ITypeSymbol type, TranslatorSyntaxVisitor? fromVisitor)
        //{
        //    //if (!global.ShouldExportType(type, null))
        //    //return default!;
        //    if (type.Kind == SymbolKind.TypeParameter)
        //    {
        //        ITypeParameterSymbol tp = (ITypeParameterSymbol)type;
        //        if (tp.DeclaringType != null)
        //        {
        //            return GenericTypeHandle(tp);
        //        }
        //        else
        //        {
        //            //Type declared on a method
        //            var index = tp.DeclaringMethod!.TypeParameters.IndexOf(tp);
        //            return GenericMethodHandle(tp.DeclaringMethod, index);
        //        }
        //    }
        //    if (type is INamedTypeSymbol nt)
        //    {
        //        if (nt.IsGenericType)
        //        {
        //            if (fromVisitor != null && SymbolEqualityComparer.Default.Equals(fromVisitor.CurrentTypeSymbol, type))
        //            {
        //                return $"this.{Constants.PrototypeTypeHandle}";
        //            }
        //            static IEnumerable<ITypeParameterSymbol> GetTypeArguments(INamedTypeSymbol ts)
        //            {
        //                foreach (var tts in ts.TypeArguments)
        //                {
        //                    if (tts is ITypeParameterSymbol tp)
        //                        yield return tp;
        //                    if (tts is INamedTypeSymbol nt)
        //                    {
        //                        foreach (var ttss in GetTypeArguments(nt))
        //                            yield return ttss;
        //                    }
        //                    else if (tts is IArrayTypeSymbol at)
        //                    {
        //                        if (at.ElementType is ITypeParameterSymbol tp2)
        //                            yield return tp2;
        //                        if (at.ElementType is INamedTypeSymbol nt2)
        //                            foreach (var ttss in GetTypeArguments(nt2))
        //                                yield return ttss;
        //                    }
        //                }
        //                if (ts.ContainingType != null)
        //                {
        //                    foreach (var tts in GetTypeArguments(ts.ContainingType))
        //                        yield return tts;
        //                }
        //            }
        //            var allTypeArgs = GetTypeArguments(nt);
        //            IMethodSymbol? declaringMethod = null;
        //            if (allTypeArgs.Any(t => (declaringMethod = t.DeclaringMethod) != null))
        //            {
        //                //at least a generic type argument on this type is defined by method
        //                return $"({string.Join(", ", declaringMethod!.TypeParameters.Select(tp => tp.Name))}) => {nt.ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
        //            }
        //            else
        //            {
        //                //if (fromVisitor != null && nt.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter))
        //                //{
        //                //    static IEnumerable<ITypeSymbol> GetTypeArguments(INamedTypeSymbol ts)
        //                //    {
        //                //        foreach (var tts in ts.TypeArguments)
        //                //            yield return tts;
        //                //        if (ts.ContainingType != null)
        //                //        {
        //                //            foreach (var tts in GetTypeArguments(ts.ContainingType))
        //                //                yield return tts;
        //                //        }
        //                //    }
        //                //    var ts = nt.TypeArguments;
        //                //    var scopTypeArguments = GetTypeArguments(fromVisitor.CurrentTypeSymbol);
        //                //    var allTypeArgumentsAreAvailableInScope = nt.TypeArguments.All(t => scopTypeArguments.Contains(t, SymbolEqualityComparer.Default));
        //                //    //$.$spc.System.Collections.Generic.HashSet$$(T).AlternateLookup$$(TAlternate) should be $.$spc.System.Collections.Generic.HashSet$$(T).AlternateLookup$$()
        //                //    if (!allTypeArgumentsAreAvailableInScope)
        //                //    {
        //                //        return $"{nt.ConstructUnboundGenericType().ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
        //                //    }
        //                //}
        //                if (nt.ContainingType != null && nt.TypeParameters.Length > 0 && fromVisitor != null && !SymbolEqualityComparer.Default.Equals(nt, fromVisitor.CurrentTypeSymbol))
        //                {
        //                    static IEnumerable<ITypeSymbol> GetScopeTypeArguments(INamedTypeSymbol ts)
        //                    {
        //                        foreach (var tts in ts.TypeArguments)
        //                            yield return tts;
        //                        if (ts.ContainingType != null)
        //                        {
        //                            foreach (var tts in GetScopeTypeArguments(ts.ContainingType))
        //                                yield return tts;
        //                        }
        //                    }
        //                    var ts = nt.TypeArguments;
        //                    var scopeTypeArguments = GetScopeTypeArguments(fromVisitor.CurrentTypeSymbol);
        //                    var allTypeArgumentsAreAvailableInScope = nt.TypeArguments.All(t => scopeTypeArguments.Contains(t, SymbolEqualityComparer.Default));
        //                    //$.$spc.System.Collections.Generic.HashSet$$(T).AlternateLookup$$(TAlternate) should be $.$spc.System.Collections.Generic.HashSet$$(T).AlternateLookup$$()
        //                    if (!allTypeArgumentsAreAvailableInScope)
        //                    {
        //                        //return $"{nt.ConstructUnboundGenericType().ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
        //                        return NumericTypeHandle(nt);
        //                    }
        //                }
        //                return $"{nt.ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
        //            }
        //        }
        //        return NumericTypeHandle(nt);
        //    }
        //    int typeHandle = types.GetValueOrDefault(type.OriginalDefinition);
        //    if (typeHandle <= 0)
        //    {
        //        var nn = type.CreateSignature(global, withGlobalNamespace: true);
        //        var symbol = global.ImportedNames.Types.GetValueOrDefault(nn);
        //        if (symbol?.Handle != null)
        //        {
        //            return symbol.Handle.Value;
        //        }
        //    }
        //    if (typeHandle < 0)
        //        return 0;
        //    return (assemblyHandle << ReflectionHandleExtension.AssemblyShift) | ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
        //}

        Handle GenericTypeHandle(int typeIndex)
        {
            var typeHandle = typeIndex + (int)KnownTypeHandle.GenericType1Placeholder;
            return ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
        }

        Handle GenericTypeHandle(ITypeParameterSymbol type)
        {
            int typeIndex = -1;
            var typeArgs = type.DeclaringType!.TypeArguments;
            for (int i = 0; i < typeArgs.Length; i++)
            {
                if (typeArgs[i].Equals(type, SymbolEqualityComparer.Default))
                {
                    typeIndex = i;
                    break;
                }
            }

            if (typeIndex < 0)
            {
                return default;
            }

            return new Handle() { Expression = $"{type.Name}?.{Constants.PrototypeTypeHandle}??{GenericTypeHandle(typeIndex).Expression}" };
        }

        Handle GenericMethodHandle(IMethodSymbol method, int typeIndex)
        {
            var typeHandle = typeIndex + (int)KnownTypeHandle.GenericMethodType1Placeholder;

            int memberIndex = -1;
            var allMembers = method.ContainingType.GetMembers();
            for (int i = 0; i < allMembers.Length; i++)
            {
                if (allMembers[i].Equals(method, SymbolEqualityComparer.Default))
                {
                    memberIndex = i;
                    break;
                }
            }

            if (memberIndex <= 0)
            {
                return 0; // Handle fallback gracefully if method isn't found
            }

            return ((ulong)typeHandle << ReflectionHandleExtension.TypeShift) |
                   ((ulong)memberIndex << ReflectionHandleExtension.MemberShift);
        }

        Handle GenericMethodTypeParameterHandle(IMethodSymbol declaringMethod, ITypeParameterSymbol typeParameter)
        {
            var typeParams = declaringMethod.TypeParameters;
            var paramNames = new string[typeParams.Length];
            for (int i = 0; i < typeParams.Length; i++)
            {
                paramNames[i] = typeParams[i].Name;
            }
            return $"({string.Join(", ", paramNames)}) => {typeParameter.ComputeOutputTypeName(global)}.{Constants.PrototypeTypeHandle}";
        }

        //Handle GenericMethodHandle(IMethodSymbol method, int typeIndex)
        //{
        //    var typeHandle = typeIndex + (int)KnownTypeHandle.GenericMethodType1Placeholder;
        //    var memberIndex = method.ContainingType.GetMembers().IndexOf(method);
        //    return ((ulong)typeHandle << ReflectionHandleExtension.TypeShift) |
        //        ((ulong)memberIndex << ReflectionHandleExtension.MemberShift);
        //}

        public IEnumerable<INamedTypeSymbol> InitializeForAssembly(IAssemblySymbol assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (assemblyHandle != 0)
                throw new InvalidOperationException("Already init for assembly");
            if (!global.HasAttribute(assembly, typeof(AssemblyHandleAttribute).FullName!, null, false, out var args))
            {
                //throw new InvalidOperationException("An AssemblyHandleAttribute must be defined on all assembly");
            }
            //TODO: Wont the handle clash between multiple assemblies, we can use a random number for now, but we should have a better way to generate unique assembly handles
            assemblyHandle = (uint)(args?.ElementAtOrDefault(0).Value ?? (isSystemPrivateCoreLib ? 1u : 0u));

            //Tru use handle generated in the last compilation
            if (assemblyHandle == 0)
            {
                var existingYaml = $"{global.Output.OutputPath}{Path.DirectorySeparatorChar}{global.Project.GetAssemblyName()}.Symbols.yaml";
                if (File.Exists(existingYaml))
                {
                    using var reader = new StreamReader(existingYaml);
                    int i = 0;
                    while (i < 20)
                    {
                        var line = reader.ReadLine();
                        if (line.StartsWith("    handle: "))
                        {
                            var h = line.Substring(12);
                            assemblyHandle = uint.Parse(h);
                            if (assemblyHandle > 0xFFFF)
                                assemblyHandle = 0;
                            break;
                        }
                    }
                }
            }
            if (assemblyHandle == 0)
            {
                var handle = Random.Shared.Next(8192, 65536);
                while (usedAssemblyHandle.Contains(handle))
                {
                    handle = Random.Shared.Next(8192, 65536);
                }
                usedAssemblyHandle.Add(handle);
                assemblyHandle = (uint)handle;
            }
            //uint assemblyHandle = (uint)args[0];
            var assemblies = global.AllSymbols.Values.Where(e => e.Symbol.Kind == SymbolKind.Assembly);
            var types = global.AllSymbols.Values.Where(e => SymbolEqualityComparer.Default.Equals(e.Symbol.ContainingAssembly, assembly) && e.Symbol.Kind == SymbolKind.NamedType);
            //var types = global.AllSymbols.Values.Where(e => e.Symbol.Kind == SymbolKind.NamedType).Cast<INamedTypeSymbol>().ToList();
            //GetAllTypes(assembly.GlobalNamespace).Concat(assembly.GlobalNamespace
            //    .GetNamespaceMembers()
            //    .SelectMany(GetAllTypes))
            //.Distinct(SymbolEqualityComparer.Default)
            //.Cast<INamedTypeSymbol>()
            //.ToList();
            //var _typeNames = new string[] { "" }
            //.Concat(Enumerable.Range(1, isSystemPrivateCoreLib ? 32 : 0)
            //.Select(i => $"$T{i}")).Concat(types.Select(t => t.CreateSignature(global, withGlobalNamespace: false))).Distinct();
            //make sure unknown type is index zero, System.Object is at index 1
            //Order predictably, so we can rebuild an assembly and type handles can be guaranteed to be the same. Unless a nw type was added or one removed
            this.types = (isSystemPrivateCoreLib ? types.OrderBy(t =>
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemDynamic) ? (int)KnownTypeHandle.SystemDynamic :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemVoid) ? (int)KnownTypeHandle.SystemVoid :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemObject) ? (int)KnownTypeHandle.SystemObject :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemValueType) ? (int)KnownTypeHandle.SystemValueType :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemBoolean) ? (int)KnownTypeHandle.SystemBool :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemChar) ? (int)KnownTypeHandle.SystemChar :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemSByte) ? (int)KnownTypeHandle.SystemSByte :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemByte) ? (int)KnownTypeHandle.SystemByte :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemInt16) ? (int)KnownTypeHandle.SystemInt16 :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemUInt16) ? (int)KnownTypeHandle.SystemUInt16 :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemInt32) ? (int)KnownTypeHandle.SystemInt32 :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemUInt32) ? (int)KnownTypeHandle.SystemUInt32 :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemIntPtr) ? (int)KnownTypeHandle.SystemIntPtr :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemUIntPtr) ? (int)KnownTypeHandle.SystemUIntPtr :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemInt64) ? (int)KnownTypeHandle.SystemInt64 :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemUInt64) ? (int)KnownTypeHandle.SystemUInt64 :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemEnum) ? (int)KnownTypeHandle.SystemEnum :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemSingle) ? (int)KnownTypeHandle.SystemSingle :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemDouble) ? (int)KnownTypeHandle.SystemDouble :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemArray) ? (int)KnownTypeHandle.SystemArray :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemString) ? (int)KnownTypeHandle.SystemString :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemPointer) ? (int)KnownTypeHandle.SystemPointer :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemReference) ? (int)KnownTypeHandle.SystemReference :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemNullable) ? (int)KnownTypeHandle.SystemNullable :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT1) ? (int)KnownTypeHandle.GenericType1Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT2) ? (int)KnownTypeHandle.GenericType2Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT3) ? (int)KnownTypeHandle.GenericType3Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT4) ? (int)KnownTypeHandle.GenericType4Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT5) ? (int)KnownTypeHandle.GenericType5Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT6) ? (int)KnownTypeHandle.GenericType6Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT7) ? (int)KnownTypeHandle.GenericType7Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT8) ? (int)KnownTypeHandle.GenericType8Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT9) ? (int)KnownTypeHandle.GenericType9Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT10) ? (int)KnownTypeHandle.GenericType10Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT11) ? (int)KnownTypeHandle.GenericType11Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT12) ? (int)KnownTypeHandle.GenericType12Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT13) ? (int)KnownTypeHandle.GenericType13Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT14) ? (int)KnownTypeHandle.GenericType14Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT15) ? (int)KnownTypeHandle.GenericType15Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT16) ? (int)KnownTypeHandle.GenericType16Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT17) ? (int)KnownTypeHandle.GenericType17Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT18) ? (int)KnownTypeHandle.GenericType18Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT19) ? (int)KnownTypeHandle.GenericType19Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT20) ? (int)KnownTypeHandle.GenericType20Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT21) ? (int)KnownTypeHandle.GenericType21Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT22) ? (int)KnownTypeHandle.GenericType22Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT23) ? (int)KnownTypeHandle.GenericType23Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT24) ? (int)KnownTypeHandle.GenericType24Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT25) ? (int)KnownTypeHandle.GenericType25Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT26) ? (int)KnownTypeHandle.GenericType26Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT27) ? (int)KnownTypeHandle.GenericType27Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT28) ? (int)KnownTypeHandle.GenericType28Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT29) ? (int)KnownTypeHandle.GenericType29Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT30) ? (int)KnownTypeHandle.GenericType30Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT31) ? (int)KnownTypeHandle.GenericType31Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.SystemT32) ? (int)KnownTypeHandle.GenericType32Placeholder :
            SymbolEqualityComparer.Default.Equals(t.Symbol, global.AnonymousType) ? (int)KnownTypeHandle.AnonymousType :
            int.MaxValue).ThenBy(type => type.Symbol.CreateSignature(global, withTypeParameterNames: false, withGlobalNamespace: false)) : types.OrderBy(type => type.Symbol.CreateSignature(global, withTypeParameterNames: false, withGlobalNamespace: false)))
            .Select((o, i) => (o, i))
            .ToDictionary(x => (ITypeSymbol)x.o.Symbol, x => x.i + (isSystemPrivateCoreLib ? 0/*only dynamic can have handle 0*/ : 1), (IEqualityComparer<ITypeSymbol>)SymbolEqualityComparer.Default);
            //var symbolDictionary = global.Symbols.Types.ToDictionary(e => e.Value.Signature, e => e.Value);
            var asm = global.GetRequiredMetadata(global.Compilation.Assembly);
            asm.Export.Handle = assemblyHandle;
            foreach (var type in types)
            {
                var ttype = (INamedTypeSymbol)type.Symbol;
                bool isDynamic = SymbolEqualityComparer.Default.Equals(ttype, global.SystemDynamic);
                var typeExport = type.Export;
                // global.GetRequiredMetadata(type).Export;
                //var typeSignature = type.CreateSignature(global, withTypeParameterNames: true, withGlobalNamespace: true);
                //var typeMetadata = global.Symbols.Types.FirstOrDefault(e => e.Value.Signature == typeSignature).Value;// symbolDictionary.GetValueOrDefault(name);
                var typeHandle = (ulong)NumericTypeHandle(ttype, false);
                if (typeExport != null)
                {
                    if (isDynamic)
                        typeExport.IsDynamic();
                    typeExport.Handle = typeHandle;
                }
                var members = ttype.GetMembers().Where(e => e.Kind != SymbolKind.NamedType).SelectMany(e =>
                {
                    if (e.Kind == SymbolKind.Property && e is IPropertySymbol ps)
                    {
                        return ((IEnumerable<ISymbol?>)[ps, ps.GetMethod, ps.SetMethod]).Where(e => e != null)!;
                    }
                    else if (e.Kind == SymbolKind.Event && e is IEventSymbol es)
                    {
                        return ((IEnumerable<ISymbol?>)[es, es.AddMethod, es.RemoveMethod]).Where(e => e != null)!;
                    }
                    return [e];
                })
                .Select(e => global.GetRequiredMetadata(e!))
                //Order predictably, so when we can rebuild, handles can be guaranteed to be the same. Unless a member type was added or one removed
                .OrderBy(e => e.Signature);
                int memberIndex = 1;
                foreach (var member in members)
                {
                    var memberExport = member.Export;
                    //var memberSignature = member.CreateSignature(global, withTypeParameterNames: true, withGlobalNamespace: true);
                    //var memberMetadata = global.Symbols.Members.GetValueOrDefault(typeSignature)?.FirstOrDefault(e => e.Value.Signature == metadata?.Signature).Value;
                    //if (memberExport != null)
                    //{
                    ulong memberHandle = typeHandle | ((ulong)memberIndex << ReflectionHandleExtension.MemberShift);
                    //var memberHandle = MemberHandle(member, null, false);
                    if (isDynamic)
                        memberExport.IsDynamic();
                    memberExport.Handle = memberHandle;
                    //}
                    memberIndex++;
                }
            }
            //foreach (var kv in global.AllSymbols)
            //{
            //    var metadata = kv.Value;
            //    var symbol = metadata.Symbol;
            //    bool isImportedType = !SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, global.Compilation.Assembly);
            //    if (isImportedType)
            //        continue;

            //    if (symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol methodSymbol)
            //    {
            //        if (methodSymbol.IsOverride)
            //        {
            //            var baseMethod = methodSymbol.OverriddenMethod;
            //            while (baseMethod != null)
            //            {
            //                global.RegisterDependencyGraph(baseMethod, methodSymbol, WeakDependencyReason.TargetConstructorCalled);
            //                baseMethod = baseMethod.OverriddenMethod;
            //            }
            //        }
            //        else if (methodSymbol.ExplicitInterfaceImplementations.Length > 0)
            //        {
            //            var interfaceMethod = methodSymbol.ExplicitInterfaceImplementations.First();
            //            global.RegisterDependencyGraph(interfaceMethod, methodSymbol, interfaceMethod.IsStatic ? WeakDependencyReason.TargetTypeUsed : WeakDependencyReason.TargetConstructorCalled);
            //        }
            //    }
            //    else if (symbol.Kind == SymbolKind.Property && symbol is IPropertySymbol propertySymbol)
            //    {
            //        if (propertySymbol.IsOverride)
            //        {
            //            var baseProperty = propertySymbol.OverriddenProperty;
            //            while (baseProperty != null)
            //            {
            //                global.RegisterDependencyGraph(baseProperty, propertySymbol, WeakDependencyReason.TargetConstructorCalled);
            //                baseProperty = baseProperty.OverriddenProperty;
            //            }
            //        }
            //        else if (propertySymbol.ExplicitInterfaceImplementations.Length > 0)
            //        {
            //            var interfaceProperty = propertySymbol.ExplicitInterfaceImplementations.First();
            //            global.RegisterDependencyGraph(interfaceProperty, propertySymbol, interfaceProperty.IsStatic ? WeakDependencyReason.TargetTypeUsed : WeakDependencyReason.TargetConstructorCalled);
            //        }
            //    }
            //    else if (symbol.Kind == SymbolKind.Event && symbol is IEventSymbol eventSymbol )
            //    {
            //        if (eventSymbol.IsOverride)
            //        {
            //            var baseEvent = eventSymbol.OverriddenEvent;
            //            while (baseEvent != null)
            //            {
            //                global.RegisterDependencyGraph(baseEvent, eventSymbol, WeakDependencyReason.TargetConstructorCalled);
            //                baseEvent = baseEvent.OverriddenEvent;
            //            }
            //        }
            //        else if (eventSymbol.ExplicitInterfaceImplementations.Length > 0)
            //        {
            //            var interfaceEvent = eventSymbol.ExplicitInterfaceImplementations.First();
            //            global.RegisterDependencyGraph(interfaceEvent, eventSymbol, interfaceEvent.IsStatic ? WeakDependencyReason.TargetTypeUsed : WeakDependencyReason.TargetConstructorCalled);
            //        }
            //    }
            //}
            return types.Select(t => (INamedTypeSymbol)t.Symbol);
        }

        public AssemblyModel FromAssemblySymbol(IAssemblySymbol assembly)
        {
            if (assemblyHandle == 0)
                throw new InvalidOperationException("Not initialized");
            //var types = this.types.Keys;// InitializeForAssembly(assembly);
            var model = new AssemblyModel
            {
                AssemblyFlags = global.MainEntry != null ? NetJs.AssemblyFlags.Entry : NetJs.AssemblyFlags.None,
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
                    .Select(s => FromAttribute(assembly, s, null))
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
            if (!Constants.ResourceNamesInlined)
            {
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
            }
            //}
            //if (embeddedFiles != null)
            //{
            foreach (var file in embeddedFiles.Where(e => !e.EndsWith(".resx")))
            {
                if (File.Exists(file))
                {
                    var manifest = new AssemblyManifestModel();
                    manifest.Name = Path.GetFileName(file);
                    var data = File.ReadAllBytes(file);
                    //var dataLen = data.Length;
                    //var finalData = new byte[4 + data.Length];
                    //finalData[0] = (byte)((dataLen >> 0) & 0xFF);
                    //finalData[1] = (byte)((dataLen >> 8) & 0xFF);
                    //finalData[2] = (byte)((dataLen >> 16) & 0xFF);
                    //finalData[3] = (byte)((dataLen >> 24) & 0xFF);
                    //Array.Copy(data, 0, finalData, 4, dataLen);
                    //manifest.Data = Convert.ToBase64String(finalData);
                    manifest.Data = Convert.ToBase64String(data);
                    manifests.Add(manifest);
                }
            }
            //}
            model.Manifests = manifests.ToArray();
            return model;
        }
        public TypeModel FromTypeSymbol(ITypeSymbol? symbol, TranslatorSyntaxVisitor? fromVisitor, bool minimal = false)
        {
            if (symbol == null) return new TypeModel { };

            List<PropertyModel>? propertiesList = null;
            List<MethodModel>? methodsList = null;
            List<ConstructorModel>? constructorsList = null;
            List<FieldModel>? fieldsList = null;
            List<EventModel>? eventsList = null;

            if (!minimal)
            {
                propertiesList = new List<PropertyModel>(8);
                methodsList = new List<MethodModel>(16);
                constructorsList = new List<ConstructorModel>(4);
                fieldsList = new List<FieldModel>(8);
                eventsList = new List<EventModel>(2);

                var members = symbol.GetMembers();
                for (int i = 0; i < members.Length; i++)
                {
                    var m = members[i];

                    if (m.IsExtern || !global.IsReflectable(m, null))
                    {
                        continue;
                    }

                    switch (m)
                    {
                        case IPropertySymbol propertySymbol:
                            {
                                var markers = propertySymbol.TrimmableBoundary(global);
                                propertiesList.Add(PropertyModel.TrimmableMarker.Open(markers.Start));
                                propertiesList.Add(FromPropertySymbol(propertySymbol, fromVisitor));
                                propertiesList.Add(PropertyModel.TrimmableMarker.Close(markers.End));
                            }
                            break;

                        case IMethodSymbol methodSymbol:
                            if (methodSymbol.MethodKind == MethodKind.Constructor)
                            {
                                if (!global.LinkTrimOutMethod(methodSymbol))
                                {
                                    var markers = methodSymbol.TrimmableBoundary(global);
                                    constructorsList.Add(ConstructorModel.TrimmableMarker.Open(markers.Start));
                                    constructorsList.Add(FromConstructorSymbol(methodSymbol, fromVisitor));
                                    constructorsList.Add(ConstructorModel.TrimmableMarker.Close(markers.End));
                                }
                            }
                            else if (methodSymbol.MethodKind == MethodKind.Ordinary || methodSymbol.MethodKind == MethodKind.DelegateInvoke)
                            {
                                if (!global.LinkTrimOutMethod(methodSymbol))
                                {
                                    var markers = methodSymbol.TrimmableBoundary(global);
                                    methodsList.Add(MethodModel.TrimmableMarker.Open(markers.Start));
                                    methodsList.Add(FromMethodSymbol(methodSymbol, fromVisitor));
                                    methodsList.Add(MethodModel.TrimmableMarker.Close(markers.End));
                                }
                            }
                            break;

                        case IFieldSymbol fieldSymbol:
                            if (fieldSymbol.Name.IndexOf("k__BackingField", StringComparison.Ordinal) < 0)
                            {
                                var markers = fieldSymbol.TrimmableBoundary(global);
                                fieldsList.Add(FieldModel.TrimmableMarker.Open(markers.Start));
                                fieldsList.Add(FromFieldSymbol(fieldSymbol, fromVisitor));
                                fieldsList.Add(FieldModel.TrimmableMarker.Close(markers.End));
                            }
                            break;

                        case IEventSymbol eventSymbol:
                            {
                                var markers = eventSymbol.TrimmableBoundary(global);
                                eventsList.Add(EventModel.TrimmableMarker.Open(markers.Start));
                                eventsList.Add(FromEventSymbol(eventSymbol, fromVisitor));
                                eventsList.Add(EventModel.TrimmableMarker.Close(markers.End));
                            }
                            break;
                    }
                }
            }

            //Handle[]? genericArguments = null;
            GenericParameterModel[]? genericParameters = null;
            int genericParameterCount = 0;

            if (symbol is INamedTypeSymbol namedSymbol)
            {
                var typeParams = namedSymbol.TypeParameters;
                genericParameterCount = typeParams.Length;

                if (!minimal && genericParameterCount > 0)
                {
                    //genericArguments = new Handle[genericParameterCount];
                    for (int i = 0; i < genericParameterCount; i++)
                    {
                        //genericArguments[i] = GenericTypeHandle(typeParams[i]);
                        //if (typeParams[i].Variance != VarianceKind.None ||
                        //    typeParams[i].HasNotNullConstraint ||
                        //    typeParams[i].HasReferenceTypeConstraint ||
                        //    typeParams[i].HasValueTypeConstraint ||
                        //    typeParams[i].HasUnmanagedTypeConstraint ||
                        //    typeParams[i].HasConstructorConstraint)
                        //{
                        genericParameters ??= new GenericParameterModel[genericParameterCount];
                        Handle[]? typeConstraints = null;
                        if (typeParams[i].ConstraintTypes.Length > 0)
                        {
                            for (int j = 0; j < typeParams[i].ConstraintTypes.Length; j++)
                            {
                                var constraint = typeParams[i].ConstraintTypes[j];
                                if (global.ShouldExportType(constraint, null))
                                {
                                    typeConstraints ??= new Handle[typeParams[i].ConstraintTypes.Length];
                                    typeConstraints[j] = TypeHandle(constraint, fromVisitor: fromVisitor);
                                }
                            }
                        }
                        genericParameters[i] = new GenericParameterModel
                        {
                            Handle = GenericTypeHandle(typeParams[i]),
                            ParameterName = typeParams[i].Name,
                            Flags =
                                (typeParams[i].Variance == VarianceKind.In ? GenericConstraintFlagsModel.HasInConstraint : GenericConstraintFlagsModel.None) |
                                (typeParams[i].Variance == VarianceKind.Out ? GenericConstraintFlagsModel.HasOutConstraint : GenericConstraintFlagsModel.None) |
                                (typeParams[i].HasNotNullConstraint ? GenericConstraintFlagsModel.HasNotNullConstraint : GenericConstraintFlagsModel.None) |
                                (typeParams[i].HasReferenceTypeConstraint ? GenericConstraintFlagsModel.HasClassConstraint : GenericConstraintFlagsModel.None) |
                                (typeParams[i].HasValueTypeConstraint ? GenericConstraintFlagsModel.HasStructConstraint : GenericConstraintFlagsModel.None) |
                                (typeParams[i].HasUnmanagedTypeConstraint ? GenericConstraintFlagsModel.HasUnmanagedConstraint : GenericConstraintFlagsModel.None) |
                                (typeParams[i].HasConstructorConstraint ? GenericConstraintFlagsModel.HasNewConstraint : GenericConstraintFlagsModel.None),
                            TypeConstraints = typeConstraints
                        };
                        //}
                    }
                }
            }


            Handle[]? interfacesArray = null;
            if (!minimal)
            {
                var allInterfaces = symbol.AllInterfaces;
                if (allInterfaces.Length > 0)
                {
                    var ifaceList = new List<Handle>(allInterfaces.Length);
                    for (int i = 0; i < allInterfaces.Length; i++)
                    {
                        var iface = allInterfaces[i];
                        if (global.ShouldExportType(iface, null))
                        {
                            ifaceList.Add(TypeHandle(iface, fromVisitor: fromVisitor));
                        }
                    }
                    interfacesArray = ifaceList.Count > 0 ? ifaceList.ToArray() : null;
                }
            }

            AttributeModel[]? attributesArray = null;
            if (!minimal)
            {
                var attributes = symbol.GetAttributes();
                if (attributes.Length > 0)
                {
                    var attrList = new List<AttributeModel>(attributes.Length);
                    for (int i = 0; i < attributes.Length; i++)
                    {
                        var attr = attributes[i];
                        if (attr.AttributeClass != null && global.ShouldExportType(attr.AttributeClass, null))
                        {
                            attrList.Add(FromAttribute(symbol, attr, fromVisitor));
                        }
                    }
                    attributesArray = attrList.Count > 0 ? attrList.ToArray() : null;
                }
            }

            Handle[]? nestedTypesArray = null;
            if (!minimal)
            {
                var typeMembers = symbol.GetTypeMembers();
                if (typeMembers.Length > 0)
                {
                    var nestedList = new List<Handle>(typeMembers.Length);
                    for (int i = 0; i < typeMembers.Length; i++)
                    {
                        var t = typeMembers[i];
                        if (global.ShouldExportType(t, null) && global.IsReflectable(t, null))
                        {
                            nestedList.Add(TypeHandle(t, fromVisitor));
                        }
                    }
                    nestedTypesArray = nestedList.Count > 0 ? nestedList.ToArray() : null;
                }
            }

            var signature = symbol.CreateSignature(global, withGlobalNamespace: false);

            var model = new TypeModel
            {
                OriginalHandle = symbol is INamedTypeSymbol ts && ts.IsGenericType ? NumericTypeHandle(symbol) : default,
                Handle = TypeHandle(symbol, fromVisitor: fromVisitor),
                //BaseType = symbol.BaseType != null ? NullIfZero(TypeHandle(symbol.BaseType, fromVisitor: fromVisitor)) : null,
                DeclaringType = symbol.ContainingType != null ? TypeHandle(symbol.ContainingType, fromVisitor: fromVisitor) : default,
                UnderlyingType = (symbol is INamedTypeSymbol nt && nt.EnumUnderlyingType != null)
                    ? TypeHandle(nt.EnumUnderlyingType, fromVisitor: fromVisitor)
                    : default,
                //KnownType = global.KnownTypeFromName(signature),

                Properties = minimal ? null : NullIfEmpty(propertiesList!.ToArray()),
                Methods = minimal ? null : NullIfEmpty(methodsList!.ToArray()),
                Constructors = minimal ? null : NullIfEmpty(constructorsList!.ToArray()),
                Fields = minimal ? null : NullIfEmpty(fieldsList!.ToArray()),
                Events = minimal ? null : NullIfEmpty(eventsList!.ToArray()),

                //Interfaces = minimal ? null : NullIfEmpty(interfacesArray),
                Attributes = minimal ? null : NullIfEmpty(attributesArray),
                //GenericArguments = minimal ? null : NullIfEmpty(genericArguments),
                GenericParameters = minimal ? null : NullIfEmpty(genericParameters),
                //NestedTypes = minimal ? null : NullIfEmpty(nestedTypesArray),
                GenericParameterCount = genericParameterCount
            };

            return model;
        }
        //public TypeModel FromTypeSymbol(ITypeSymbol? symbol, TranslatorSyntaxVisitor? fromVisitor, bool minimal = false)
        //{
        //    if (symbol == null) return new TypeModel { };
        //    var members = symbol.GetMembers();
        //    var model = new TypeModel
        //    {
        //        //Name = symbol.Name,
        //        Handle = TypeHandle(symbol, fromVisitor: fromVisitor),
        //        //AssemblyQualifiedName = $"{RemoveGlobal(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}, {symbol.ContainingAssembly?.Name}",
        //        BaseType = symbol.BaseType != null ? NullIfZero(TypeHandle(symbol.BaseType, fromVisitor: fromVisitor)) : null,
        //        DeclaringType = symbol.ContainingType != null ? TypeHandle(symbol.ContainingType, fromVisitor: fromVisitor) : default,
        //        UnderlyingType = (symbol is INamedTypeSymbol nt && nt.EnumUnderlyingType != null)
        //            ? TypeHandle(nt.EnumUnderlyingType, fromVisitor: fromVisitor)
        //            : default,
        //        //Kind = global.MapTypeKind(symbol.TypeKind),
        //        //Flags = global.GetTypeFlags(symbol),
        //        //TypeAttributes = 0,
        //        KnownType = global.KnownTypeFromName(symbol.CreateSignature(global, withGlobalNamespace: false)),
        //        Properties = minimal ? null : NullIfEmpty(members
        //                    .Where(m => m.Kind == SymbolKind.Property && global.IsReflectable(m, null))
        //                    .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
        //                    //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
        //                    .OfType<IPropertySymbol>()
        //                    .Select(e => FromPropertySymbol(e, fromVisitor)).ToArray()),
        //        Methods = minimal ? null : NullIfEmpty(members
        //                    .Where(m => m.Kind == SymbolKind.Method && global.IsReflectable(m, null))
        //                    .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
        //                    //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
        //                    .OfType<IMethodSymbol>()
        //                    .Where(m => m.MethodKind == MethodKind.Ordinary || m.MethodKind == MethodKind.DelegateInvoke)
        //                    .Where(m => !global.LinkTrimOutMethod(m))
        //                    .Select(e => FromMethodSymbol(e, fromVisitor))
        //                    .ToArray()),
        //        Constructors = minimal ? null : NullIfEmpty(members
        //                    .Where(m => m.Kind == SymbolKind.Method && m.Name == ".ctor" && global.IsReflectable(m, null))
        //                    .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
        //                    //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
        //                    .OfType<IMethodSymbol>()
        //                    .Where(m => m.MethodKind == MethodKind.Constructor)
        //                    .Where(m => !global.LinkTrimOutMethod(m))
        //                    .Select(e => FromConstructorSymbol(e, fromVisitor)).ToArray()),
        //        Fields = minimal ? null : NullIfEmpty(members
        //                    .Where(m => m.Kind == SymbolKind.Field && global.IsReflectable(m, null))
        //                    .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
        //                    //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
        //                    .Where(m => !m.Name.Contains("k__BackingField")/*Property backing fields are not needed*/)
        //                    .OfType<IFieldSymbol>().Select(e => FromFieldSymbol(e, fromVisitor)).ToArray()),
        //        Events = minimal ? null : NullIfEmpty(members
        //                    .Where(m => m.Kind == SymbolKind.Event && global.IsReflectable(m, null))
        //                     .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
        //                    //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
        //                    .OfType<IEventSymbol>().Select(e => FromEventSymbol(e, fromVisitor)).ToArray()),
        //        Interfaces = minimal ? null : NullIfEmpty(symbol.AllInterfaces.Where(i => global.ShouldExportType(i, null)).Select(i => TypeHandle(i, fromVisitor: fromVisitor)).ToArray()),
        //        Attributes = minimal ? null : NullIfEmpty(symbol.GetAttributes()
        //        .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
        //        .Select(a => FromAttribute(a, fromVisitor)).ToArray()),
        //        GenericArguments = minimal ? null : NullIfEmpty(symbol is INamedTypeSymbol g && g.TypeParameters.Any()
        //            ? g.TypeParameters.Select((t, i) =>
        //            {
        //                //var handle = TypeHandle(t);
        //                //if (handle == 0)
        //                //{
        //                var handle = GenericTypeHandle(t);
        //                //}
        //                return handle;
        //            }).ToArray()
        //            : Array.Empty<Handle>()),
        //        GenericConstraints = minimal ? null : NullIfEmpty(Array.Empty<GenericParameterConstraintModel>()),
        //        NestedTypes = minimal ? null : NullIfEmpty(symbol.GetTypeMembers().Where(t => global.ShouldExportType(t, null) && global.IsReflectable(t, null)).Select(t => TypeHandle(t, fromVisitor)).ToArray()),
        //        GenericParameterCount = symbol is INamedTypeSymbol ng ? ng.TypeParameters.Length : 0,
        //        //Size = global.SizeOf(symbol)
        //    };

        //    return model;
        //}

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
            return json;
        }

        private IEnumerable<INamedTypeSymbol> GetInnerTypes(ITypeSymbol ns)
        {
            foreach (var nested in ns.GetTypeMembers())
            {
                //if (global.ShouldExportType(nested, null) /*&& global.IsReflectable(nested, null)*/)
                yield return nested;
                foreach (var inner in GetInnerTypes(nested))
                    //if (global.ShouldExportType(inner, null)/* && global.IsReflectable(inner, null)*/)
                    yield return inner;
            }
        }
        private IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
        {
            foreach (var type in ns.GetTypeMembers())
            {
                //if (global.ShouldExportType(type, null) /*&& global.IsReflectable(type, null)*/)
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

            var explicitImpls = prop.ExplicitInterfaceImplementations;

            if (explicitImpls.Length > 0)
            {
                var ex = explicitImpls[0].ContainingType;
                var handle = TypeHandle(ex, fromVisitor);

                int lastDotIndex = name.LastIndexOf('.');
                var strippedName = lastDotIndex >= 0 ? name.Substring(lastDotIndex + 1) : name;

                name = handle.Expression != null ? $"{{{handle}}}.{strippedName}" : strippedName;
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

            string? finalizedOutputName = null;
            if (outputName != name)
            {
                if (outputName.StartsWith(name, StringComparison.Ordinal))
                {
                    finalizedOutputName = "@" + outputName.Substring(name.Length);
                }
                else
                {
                    finalizedOutputName = outputName;
                }
            }

            List<ParameterModel>? indexParamsList = null;
            var parameters = prop.Parameters;
            if (parameters.Length > 0)
            {
                indexParamsList = new List<ParameterModel>(parameters.Length);
                for (int i = 0; i < parameters.Length; i++)
                {
                    indexParamsList.Add(FromParameterSymbol(parameters[i], fromVisitor));
                }
            }

            List<AttributeModel>? attributesList = null;
            var attributes = prop.GetAttributes();
            if (attributes.Length > 0)
            {
                attributesList = new List<AttributeModel>(attributes.Length);
                for (int i = 0; i < attributes.Length; i++)
                {
                    var a = attributes[i];
                    if (a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                    {
                        attributesList.Add(FromAttribute(prop, a, fromVisitor));
                    }
                }
            }

            return new PropertyModel
            {
                Name = name,
                OutputName = finalizedOutputName,
                DeclaringType = TypeHandle(prop.ContainingType, fromVisitor),
                Flags = prop.GetMemberFlags(),
                PropertyType = TypeHandle(prop.Type, fromVisitor),

                IndexParameters = NullIfEmpty(indexParamsList?.ToArray()),
                Attributes = NullIfEmpty(attributesList?.ToArray()),

                GetMethod = prop.GetMethod != null ? FromMethodSymbol(prop.GetMethod, fromVisitor, prop) : null,
                SetMethod = prop.SetMethod != null ? FromMethodSymbol(prop.SetMethod, fromVisitor, prop) : null,
                Handle = MemberHandle(prop, fromVisitor),
                OriginalHandle = prop.ContainingType.IsGenericType ? MemberHandle(prop, fromVisitor, numericHandle: true) : default,
            };
            ////if (propertyTypeHandle == 0 && prop.ContainingType.Arity > 0)
            ////{
            ////    var args = prop.ContainingType.TypeArguments;
            ////    var index = args.IndexOf(prop.Type, 0, SymbolEqualityComparer.Default);
            ////    if (index >= 0)
            ////        propertyTypeHandle = GenericTypeHandle(index);
            ////}
            //return new PropertyModel
            //{
            //    Name = name,
            //    OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
            //    DeclaringType = TypeHandle(prop.ContainingType, fromVisitor),
            //    Flags = prop.GetSymbolFlags(),
            //    PropertyType = propertyTypeHandle,
            //    IndexParameters = NullIfEmpty(prop.Parameters.Select(e => FromParameterSymbol(e, fromVisitor)).ToArray()),
            //    GetMethod = prop.GetMethod != null ? FromMethodSymbol(prop.GetMethod, fromVisitor, prop) : null,
            //    SetMethod = prop.SetMethod != null ? FromMethodSymbol(prop.SetMethod, fromVisitor, prop) : null,
            //    Handle = MemberHandle(prop, fromVisitor),
            //    Attributes = NullIfEmpty(prop.GetAttributes()
            //    .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
            //    .Select(a => FromAttribute(a, fromVisitor)).ToArray())
            //};
        }

        static string? NullIfVoid(string t)
        {
            if (t == "void")
                return null;
            return t;
        }
        public MethodModel FromMethodSymbol(IMethodSymbol method, TranslatorSyntaxVisitor? fromVisitor, IPropertySymbol? fromProperty = null)
        {
            var name = method.Name;
            var explicitImpls = method.ExplicitInterfaceImplementations;

            if (explicitImpls.Length > 0)
            {
                var ex = explicitImpls[0].ContainingType;
                var handle = TypeHandle(ex, fromVisitor);

                int lastDotIndex = name.LastIndexOf('.');
                var strippedName = lastDotIndex >= 0 ? name.Substring(lastDotIndex + 1) : name;

                name = handle > 0 ? $"{{{handle}}}.{strippedName}" : strippedName;
            }

            var metadata = global.GetMetadata(method);
            var outputName = metadata?.OverloadName ?? name;

            string? finalizedOutputName = null;
            if (fromProperty == null && outputName != name)
            {
                if (outputName.StartsWith(name, StringComparison.Ordinal))
                {
                    finalizedOutputName = "@" + outputName.Substring(name.Length);
                }
                else
                {
                    finalizedOutputName = outputName;
                }
            }

            List<ParameterModel>? paramsList = null;
            var parameters = method.Parameters;
            if (fromProperty == null && parameters.Length > 0)
            {
                paramsList = new List<ParameterModel>(parameters.Length);
                for (int i = 0; i < parameters.Length; i++)
                {
                    paramsList.Add(FromParameterSymbol(parameters[i], fromVisitor));
                }
            }

            List<string>? genericArgsList = null;
            var typeArgs = method.TypeParameters;
            if (fromProperty == null && typeArgs.Length > 0)
            {
                genericArgsList = new List<string>(typeArgs.Length);
                for (int i = 0; i < typeArgs.Length; i++)
                {
                    var t = typeArgs[i];
                    //var sig = !global.ShouldExportType(t, null) ? "object" : t.CreateSignature(global, withGlobalNamespace: false);
                    genericArgsList.Add(t.Name);
                }
            }

            List<AttributeModel>? attributesList = null;
            var attributes = method.GetAttributes();
            if (attributes.Length > 0)
            {
                attributesList = new List<AttributeModel>(attributes.Length);
                for (int i = 0; i < attributes.Length; i++)
                {
                    var a = attributes[i];
                    if (a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                    {
                        attributesList.Add(FromAttribute(method, a, fromVisitor));
                    }
                }
            }

            return new MethodModel
            {
                Name = fromProperty == null ? name : null!,
                OutputName = finalizedOutputName,
                DeclaringType = fromProperty == null ? TypeHandle(method.ContainingType, fromVisitor) : 0,
                Flags = method.GetMemberFlags(),
                ReturnType = fromProperty == null ? TypeHandle(method.ReturnType, fromVisitor) : 0,
                Parameters = NullIfEmpty(paramsList?.ToArray()),
                GenericArguments = NullIfEmpty(genericArgsList?.ToArray()),
                Attributes = NullIfEmpty(attributesList?.ToArray()),
                Handle = MemberHandle(method, fromVisitor),
                OriginalHandle = method.ContainingType.IsGenericType ? MemberHandle(method, fromVisitor, numericHandle: true) : default,
            };
        }

        public string FromMethodSymbolAsJson(IMethodSymbol method, TranslatorSyntaxVisitor? fromVisitor, IPropertySymbol? fromProperty = null)
        {
            var model = FromMethodSymbol(method, fromVisitor, fromProperty);
            return JsonSerializer.Serialize(model, new JsonSerializerOptions(SerializationOption) { WriteIndented = false });
        }
        //MethodModel FromMethodSymbol(IMethodSymbol method, TranslatorSyntaxVisitor? fromVisitor, IPropertySymbol? fromProperty = null)
        //{
        //    var name = method.Name;
        //    //a method that implements explicitly will have a long name qualified by the interface it is defined on
        //    //Let's shrink it by removing the interface name
        //    if (method.ExplicitInterfaceImplementations.Any())
        //    {
        //        var ex = method.ExplicitInterfaceImplementations.First().ContainingType;
        //        var handle = TypeHandle(ex, fromVisitor);
        //        name = (handle > 0 ? $"{{{handle}}}." : "") + name.Split('.').Last();
        //    }
        //    var metadata = global.GetRequiredMetadata(method);
        //    var outputName = metadata.OverloadName ?? name;
        //    var methodReturnTypeHandle = !global.ShouldExportType(method.ReturnType, null) ? default : TypeHandle(method.ReturnType, fromVisitor);
        //    //if (methodReturnTypeHandle == 0 && method.ContainingType.Arity > 0)
        //    //{
        //    //    var args = method.ContainingType.TypeArguments;
        //    //    var index = args.IndexOf(method.ReturnType, 0, SymbolEqualityComparer.Default);
        //    //    if (index >= 0)
        //    //        methodReturnTypeHandle = GenericTypeHandle(index);
        //    //}
        //    return new MethodModel
        //    {
        //        Name = fromProperty == null ? name : null!,
        //        OutputName = fromProperty == null ? (outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null) : null,
        //        DeclaringType = fromProperty == null ? TypeHandle(method.ContainingType, fromVisitor) : 0,
        //        Flags = method.GetSymbolFlags(),
        //        ReturnType = fromProperty == null ? methodReturnTypeHandle : 0,
        //        Parameters = fromProperty == null ? NullIfEmpty(method.Parameters.Select(e => FromParameterSymbol(e, fromVisitor)).ToArray()) : null,
        //        GenericArguments = fromProperty == null ? NullIfEmpty(method.TypeArguments.Select(t => !global.ShouldExportType(t, null) ? "object" : t.CreateSignature(global, withGlobalNamespace: false)).ToArray()) : null,
        //        Handle = MemberHandle(method, fromVisitor),
        //        Attributes = NullIfEmpty(method.GetAttributes()
        //        .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
        //        .Select(a => FromAttribute(a, fromVisitor)).ToArray())
        //    };
        //}

        ConstructorModel FromConstructorSymbol(IMethodSymbol ctor, TranslatorSyntaxVisitor? fromVisitor)
        {
            var metadata = global.GetRequiredMetadata(ctor);
            var name = ctor.Name;
            var outputName = metadata.OverloadName ?? name;

            string? finalizedOutputName = null;
            if (outputName != name)
            {
                if (outputName.StartsWith(name, StringComparison.Ordinal))
                {
                    finalizedOutputName = "@" + outputName.Substring(name.Length);
                }
                else
                {
                    finalizedOutputName = outputName;
                }
            }

            List<ParameterModel>? paramsList = null;
            var parameters = ctor.Parameters;
            if (parameters.Length > 0)
            {
                paramsList = new List<ParameterModel>(parameters.Length);
                for (int i = 0; i < parameters.Length; i++)
                {
                    paramsList.Add(FromParameterSymbol(parameters[i], fromVisitor));
                }
            }

            List<AttributeModel>? attributesList = null;
            var attributes = ctor.GetAttributes();
            if (attributes.Length > 0)
            {
                attributesList = new List<AttributeModel>(attributes.Length);
                for (int i = 0; i < attributes.Length; i++)
                {
                    var a = attributes[i];
                    if (a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                    {
                        attributesList.Add(FromAttribute(ctor, a, fromVisitor));
                    }
                }
            }

            return new ConstructorModel
            {
                Name = name,
                OutputName = finalizedOutputName,
                DeclaringType = TypeHandle(ctor.ContainingType, fromVisitor),
                Flags = ctor.GetMemberFlags(),
                Handle = MemberHandle(ctor, fromVisitor),
                OriginalHandle = ctor.ContainingType.IsGenericType ? MemberHandle(ctor, fromVisitor, numericHandle: true) : default,
                Parameters = NullIfEmpty(paramsList?.ToArray()),
                Attributes = NullIfEmpty(attributesList?.ToArray())
            };
        }

        //ConstructorModel FromConstructorSymbol(IMethodSymbol ctor, TranslatorSyntaxVisitor? fromVisitor)
        //{
        //    var metadata = global.GetRequiredMetadata(ctor);
        //    var name = ctor.Name;
        //    var outputName = metadata.OverloadName ?? name;
        //    return new ConstructorModel
        //    {
        //        Name = name,
        //        OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
        //        DeclaringType = TypeHandle(ctor.ContainingType, fromVisitor),
        //        Flags = ctor.GetSymbolFlags(),
        //        Parameters = NullIfEmpty(ctor.Parameters.Select(e => FromParameterSymbol(e, fromVisitor)).ToArray()),
        //        Handle = MemberHandle(ctor, fromVisitor),
        //        Attributes = NullIfEmpty(ctor.GetAttributes()
        //        .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
        //        .Select(a => FromAttribute(a, fromVisitor)).ToArray())
        //    };
        //}

        FieldModel FromFieldSymbol(IFieldSymbol field, TranslatorSyntaxVisitor? fromVisitor)
        {
            var metadata = global.GetRequiredMetadata(field);
            var name = field.Name;
            var outputName = metadata.OverloadName ?? name;

            string? finalizedOutputName = null;
            if (outputName != name)
            {
                if (outputName.StartsWith(name, StringComparison.Ordinal))
                {
                    finalizedOutputName = "@" + outputName.Substring(name.Length);
                }
                else
                {
                    finalizedOutputName = outputName;
                }
            }

            List<AttributeModel>? attributesList = null;
            var attributes = field.GetAttributes();
            if (attributes.Length > 0)
            {
                attributesList = new List<AttributeModel>(attributes.Length);
                for (int i = 0; i < attributes.Length; i++)
                {
                    var a = attributes[i];
                    if (a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                    {
                        attributesList.Add(FromAttribute(field, a, fromVisitor));
                    }
                }
            }

            return new FieldModel
            {
                Name = name,
                OutputName = finalizedOutputName,
                DeclaringType = TypeHandle(field.ContainingType, fromVisitor),
                Flags = field.GetMemberFlags(),
                FieldType = TypeHandle(field.Type, fromVisitor),
                Handle = MemberHandle(field, fromVisitor),
                OriginalHandle = field.ContainingType.IsGenericType ? MemberHandle(field, fromVisitor, numericHandle: true) : default,
                Attributes = NullIfEmpty(attributesList?.ToArray())
            };
        }

        //FieldModel FromFieldSymbol(IFieldSymbol field, TranslatorSyntaxVisitor? fromVisitor)
        //{
        //    var metadata = global.GetRequiredMetadata(field);
        //    var name = field.Name;
        //    var outputName = metadata.OverloadName ?? name;
        //    var fieldTypeHandle = !global.ShouldExportType(field.Type, null) ? default : TypeHandle(field.Type, fromVisitor);
        //    //if (fieldTypeHandle == 0 && field.ContainingType.Arity > 0)
        //    //{
        //    //    var args = field.ContainingType.TypeArguments;
        //    //    var index = args.IndexOf(field.Type, 0, SymbolEqualityComparer.Default);
        //    //    if (index >= 0)
        //    //        fieldTypeHandle = GenericTypeHandle(index);
        //    //}
        //    return new FieldModel
        //    {
        //        Name = name,
        //        OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
        //        DeclaringType = !global.ShouldExportType(field.ContainingType, null) ? default : TypeHandle(field.ContainingType, fromVisitor),
        //        Flags = field.GetSymbolFlags(),
        //        FieldType = fieldTypeHandle,
        //        Handle = MemberHandle(field, fromVisitor),
        //        Attributes = NullIfEmpty(field.GetAttributes()
        //        .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
        //        .Select(a => FromAttribute(a, fromVisitor)).ToArray())
        //    };
        //}
        EventModel FromEventSymbol(IEventSymbol ev, TranslatorSyntaxVisitor? fromVisitor)
        {
            var metadata = global.GetRequiredMetadata(ev);
            var name = ev.Name;
            var outputName = metadata.OverloadName ?? name;

            string? finalizedOutputName = null;
            if (outputName != name)
            {
                if (outputName.StartsWith(name, StringComparison.Ordinal))
                {
                    finalizedOutputName = "@" + outputName.Substring(name.Length);
                }
                else
                {
                    finalizedOutputName = outputName;
                }
            }

            List<AttributeModel>? attributesList = null;
            var attributes = ev.GetAttributes();
            if (attributes.Length > 0)
            {
                attributesList = new List<AttributeModel>(attributes.Length);
                for (int i = 0; i < attributes.Length; i++)
                {
                    var a = attributes[i];
                    if (a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                    {
                        attributesList.Add(FromAttribute(ev, a, fromVisitor));
                    }
                }
            }

            return new EventModel
            {
                Name = name,
                OutputName = finalizedOutputName,
                DeclaringType = TypeHandle(ev.ContainingType, fromVisitor),
                Flags = ev.GetMemberFlags(),
                EventHandlerType = TypeHandle(ev.Type, fromVisitor),

                AddMethod = ev.AddMethod != null ? FromMethodSymbol(ev.AddMethod, fromVisitor) : null,
                RemoveMethod = ev.RemoveMethod != null ? FromMethodSymbol(ev.RemoveMethod, fromVisitor) : null,
                RaiseMethod = ev.RaiseMethod != null ? FromMethodSymbol(ev.RaiseMethod, fromVisitor) : null,
                Handle = MemberHandle(ev, fromVisitor),
                OriginalHandle = ev.ContainingType.IsGenericType ? MemberHandle(ev, fromVisitor, numericHandle: true) : default,

                Attributes = NullIfEmpty(attributesList?.ToArray())
            };
        }

        //EventModel FromEventSymbol(IEventSymbol ev, TranslatorSyntaxVisitor? fromVisitor)
        //{
        //    var metadata = global.GetRequiredMetadata(ev);
        //    var name = ev.Name;
        //    var outputName = metadata.OverloadName ?? name;
        //    var eventTypeHandle = !global.ShouldExportType(ev.Type, null) ? default : TypeHandle(ev.Type, fromVisitor);
        //    //if (eventTypeHandle == 0 && ev.ContainingType.Arity > 0)
        //    //{
        //    //    var args = ev.ContainingType.TypeArguments;
        //    //    var index = args.IndexOf(ev.Type, 0, SymbolEqualityComparer.Default);
        //    //    if (index >= 0)
        //    //        eventTypeHandle = GenericTypeHandle(index);
        //    //}
        //    return new EventModel
        //    {
        //        Name = name,
        //        OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
        //        DeclaringType = TypeHandle(ev.ContainingType, fromVisitor),
        //        Flags = ev.GetSymbolFlags(),
        //        EventHandlerType = !global.ShouldExportType(ev.Type, null) ? default : TypeHandle(ev.Type, fromVisitor),
        //        AddMethod = ev.AddMethod != null ? FromMethodSymbol(ev.AddMethod, fromVisitor) : null,
        //        RemoveMethod = ev.RemoveMethod != null ? FromMethodSymbol(ev.RemoveMethod, fromVisitor) : null,
        //        RaiseMethod = ev.RaiseMethod != null ? FromMethodSymbol(ev.RaiseMethod, fromVisitor) : null,
        //        Handle = MemberHandle(ev, fromVisitor),
        //        Attributes = NullIfEmpty(ev.GetAttributes()
        //        .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
        //        .Select(a => FromAttribute(a, fromVisitor)).ToArray())
        //    };
        //}
        ParameterModel FromParameterSymbol(IParameterSymbol param, TranslatorSyntaxVisitor? fromVisitor)
        {
            var flags = ParameterFlagsModel.None;
            if (param.IsOptional) flags |= ParameterFlagsModel.Optional;
            if (param.IsParams) flags |= ParameterFlagsModel.Params;
            if (param.HasExplicitDefaultValue) flags |= ParameterFlagsModel.HasDefaultValue;

            var refKind = param.RefKind;
            if (refKind == RefKind.Out) flags |= ParameterFlagsModel.Out;
            else if (refKind == RefKind.Ref) flags |= ParameterFlagsModel.Ref;
            else if (refKind == RefKind.In) flags |= ParameterFlagsModel.In;

            if (param.Type.Kind == SymbolKind.TypeParameter)
            {
                var tp = (ITypeParameterSymbol)param.Type;
                if (tp.Variance == VarianceKind.In) flags |= ParameterFlagsModel.ContravariantIn;
                //else if (tp.Variance == VarianceKind.Out) flags |= ParameterFlagsModel.CovariantOut;
            }

            // 2. Optimization: Allocation-free Attributes extraction loop
            List<AttributeModel>? attributesList = null;
            var attributes = param.GetAttributes();
            if (attributes.Length > 0)
            {
                attributesList = new List<AttributeModel>(attributes.Length);
                for (int i = 0; i < attributes.Length; i++)
                {
                    var a = attributes[i];
                    if (a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                    {
                        attributesList.Add(FromAttribute(param, a, fromVisitor));
                        global.RegisterDependencyGraph(param.ContainingSymbol, a.AttributeClass);
                    }
                }
            }
            var typeHandle = TypeHandle(param.Type, fromVisitor);
            return new ParameterModel
            {
                Name = param.Name,
                ParameterType = typeHandle,
                Flags = flags,
                DefaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue : null,
                Attributes = NullIfEmpty(attributesList?.ToArray())
            };
        }

        //ParameterModel FromParameterSymbol(IParameterSymbol param, TranslatorSyntaxVisitor? fromVisitor)
        //{
        //    var paramTypeHandle = !global.ShouldExportType(param.Type, null) ? default : TypeHandle(param.Type, fromVisitor);
        //    //if (paramTypeHandle == 0 && param.ContainingType.Arity > 0)
        //    //{
        //    //    var args = param.ContainingType.TypeArguments;
        //    //    var index = args.IndexOf(param.Type, 0, SymbolEqualityComparer.Default);
        //    //    if (index >= 0)
        //    //        paramTypeHandle = GenericTypeHandle(index);
        //    //}
        //    return new ParameterModel
        //    {
        //        Name = param.Name,
        //        ParameterType = paramTypeHandle,
        //        //Position = param.Ordinal,
        //        Flags =
        //        (param.IsOptional ? ParameterFlagsModel.Optional : ParameterFlagsModel.None) |
        //        (param.RefKind == RefKind.Out ? ParameterFlagsModel.Out : ParameterFlagsModel.None) |
        //        (param.RefKind == RefKind.Ref ? ParameterFlagsModel.Ref : ParameterFlagsModel.None) |
        //        (param.IsParams ? ParameterFlagsModel.Params : ParameterFlagsModel.None),
        //        DefaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue ?? "__typeDefault__" : null,
        //        Attributes = NullIfEmpty(param.GetAttributes()
        //        .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
        //        .Select(a => FromAttribute(a, fromVisitor)).ToArray())
        //    };
        //}

        object? AdaptAttrValue(ISymbol symbol, object? a, TranslatorSyntaxVisitor? fromVisitor)
        {
            if (a == null)
            {
                return null;
            }

            object? BuildIt(object? value)
            {
                if (value is ITypeSymbol t)
                {
                    global.RegisterDependencyGraph(symbol, t);
                    return TypeHandle(t, fromVisitor);
                }
                if (value is TypedConstant tc)
                {
                    if (tc.Kind == TypedConstantKind.Array)
                    {
                        return tc.Values.Select(v => BuildIt(v));
                    }
                    else
                    {
                        return BuildIt(tc.Value);
                    }
                }
                if (value is System.Collections.Immutable.ImmutableArray<TypedConstant> tcArray)
                {
                    return tcArray.Select(v => BuildIt(v));
                }
                if (value is System.Collections.Immutable.ImmutableArray<ITypeSymbol> ttArray)
                {
                    return ttArray.Select(v => BuildIt(v));
                }
                if (value is IEnumerable<TypedConstant> tcArray2)
                {
                    return tcArray2.Select(v => BuildIt(v));
                }
                if (value is IEnumerable<ITypeSymbol> ttArray2)
                {
                    return ttArray2.Select(v => BuildIt(v));
                }
                return value;
            }

            return BuildIt(a);

            if (a is ITypeSymbol t)
            {
                global.RegisterDependencyGraph(symbol, t);
                return TypeHandle(t, fromVisitor);
            }

            if (a is TypedConstant tc)
            {
                return tc.Value is ITypeSymbol ts ? TypeHandle(ts, fromVisitor) : tc.Value;// tc.Type != null ? TypeHandle(tc.Type, fromVisitor) : default;
            }

            if (a is System.Collections.Immutable.ImmutableArray<TypedConstant> tcArray)
            {
                if (tcArray.Length == 0) return Array.Empty<Handle>();

                var result = new Handle[tcArray.Length];
                for (int i = 0; i < tcArray.Length; i++)
                {
                    var type = tcArray[i].Type;
                    if (type != null)
                        global.RegisterDependencyGraph(symbol, type);
                    result[i] = type != null ? TypeHandle(type, fromVisitor) : default;
                }
                return result;
            }

            if (a is System.Collections.Immutable.ImmutableArray<ITypeSymbol> tArray)
            {
                if (tArray.Length == 0) return Array.Empty<Handle>();

                var result = new Handle[tArray.Length];
                for (int i = 0; i < tArray.Length; i++)
                {
                    global.RegisterDependencyGraph(symbol, tArray[i]);
                    result[i] = TypeHandle(tArray[i], fromVisitor);
                }
                return result;
            }

            if (a is IEnumerable<TypedConstant> tcc)
            {
                var list = new List<Handle>();
                foreach (var tcItem in tcc)
                {
                    if (tcItem.Type != null)
                    {
                        global.RegisterDependencyGraph(symbol, tcItem.Type);
                        list.Add(TypeHandle(tcItem.Type, fromVisitor));
                    }
                }
                return list.ToArray();
            }

            if (a is IEnumerable<ITypeSymbol> tt)
            {
                var list = new List<Handle>();
                foreach (var tItem in tt)
                {
                    global.RegisterDependencyGraph(symbol, tItem);
                    list.Add(TypeHandle(tItem, fromVisitor));
                }
                return list.ToArray();
            }

            return a;
        }

        //object? AdaptAttrValue(object? a, TranslatorSyntaxVisitor? fromVisitor)
        //{
        //    //if (a is not string && a is not byte && a is not short && a is not int && a is not long && a is not bool && a is not ITypeSymbol && a is not TypedConstant && a is not IEnumerable<ITypeSymbol> && a is not IEnumerable<TypedConstant>)
        //    //{

        //    //}
        //    if (a == null)
        //        return null;
        //    if (a is ITypeSymbol t)
        //        return TypeHandle(t, fromVisitor);
        //    if (a is TypedConstant tc)
        //        return TypeHandle(tc.Type!, fromVisitor);
        //    if (a is IEnumerable<ITypeSymbol> tt)
        //        return tt.Select(t => TypeHandle(t, fromVisitor));
        //    if (a is IEnumerable<TypedConstant> tcc)
        //        return tcc.Select(t => TypeHandle(t.Type!, fromVisitor));
        //    return a;
        //}

        AttributeModel FromAttribute(ISymbol symbol, AttributeData att, TranslatorSyntaxVisitor? fromVisitor)
        {
            var attributeClass = att.AttributeClass;
            var attributeConstructor = att.AttributeConstructor;

            if (attributeClass != null)
                global.RegisterDependencyGraph(symbol, attributeClass);
            if (attributeConstructor != null)
                global.RegisterDependencyGraph(symbol, attributeConstructor);

            if (attributeClass?.Name == "MarkdownReaderAttribute")
            {

            }
            List<AttributeConstructorArgumentModel>? constructorArgsList = null;
            var constructorArgs = att.ConstructorArguments;
            if (constructorArgs.Length > 0)
            {
                constructorArgsList = new List<AttributeConstructorArgumentModel>(constructorArgs.Length);
                for (int i = 0; i < constructorArgs.Length; i++)
                {
                    var arg = constructorArgs[i];

                    if (arg.Type != null)
                    {
                        global.RegisterDependencyGraph(symbol, arg.Type);
                    }

                    constructorArgsList.Add(new AttributeConstructorArgumentModel
                    {
                        Type = arg.Type != null ? TypeHandle(arg.Type, fromVisitor) : default,
                        Value = AdaptAttrValue(symbol, arg.Kind == TypedConstantKind.Array ? arg.Values : arg.Value, fromVisitor),
                    });
                }
            }

            List<AttributeNamedArgumentModel>? namedArgsList = null;
            var namedArgs = att.NamedArguments;
            if (namedArgs.Length > 0)
            {
                namedArgsList = new List<AttributeNamedArgumentModel>(namedArgs.Length);
                for (int i = 0; i < namedArgs.Length; i++)
                {
                    var arg = namedArgs[i];
                    var argValue = arg.Value;

                    var property = attributeClass?.GetMembers(arg.Key).OfType<IPropertySymbol>().FirstOrDefault();
                    if (property != null)
                    {
                        global.RegisterDependencyGraph(symbol, property);
                    }

                    if (argValue.Type != null)
                    {
                        global.RegisterDependencyGraph(symbol, argValue.Type);
                    }

                    namedArgsList.Add(new AttributeNamedArgumentModel
                    {
                        Name = arg.Key,
                        Type = argValue.Type != null ? TypeHandle(argValue.Type, fromVisitor) : default,
                        Value = AdaptAttrValue(symbol, argValue.Kind == TypedConstantKind.Array ? argValue.Values : argValue.Value, fromVisitor),
                    });
                }
            }

            return new AttributeModel
            {
                TypeHandle = attributeClass == null ? default : TypeHandle(attributeClass, fromVisitor),
                ConstructorHandle = attributeConstructor == null ? default : MemberHandle(attributeConstructor, fromVisitor),

                // Leverage the optimized List-based NullIfEmpty implementation introduced earlier
                ConstructorArguments = NullIfEmpty(constructorArgsList?.ToArray()),
                NamedArguments = NullIfEmpty(namedArgsList?.ToArray())
            };
        }

        //AttributeModel FromAttribute(AttributeData att, TranslatorSyntaxVisitor? fromVisitor)
        //{
        //    return new AttributeModel
        //    {
        //        TypeHandle = att.AttributeClass == null ? default : TypeHandle(att.AttributeClass, fromVisitor),
        //        ConstructorHandle = att.AttributeConstructor == null ? default : MemberHandle(att.AttributeConstructor, fromVisitor),
        //        ConstructorArguments = NullIfEmpty(att.ConstructorArguments.Select(arg => new AttributeConstructorArgumentModel
        //        {
        //            Type = arg.Type != null ? TypeHandle(arg.Type, fromVisitor) : default,
        //            Value = AdaptAttrValue(arg.Kind == TypedConstantKind.Array ? arg.Values : arg.Value, fromVisitor),
        //        }).ToArray()),
        //        NamedArguments = NullIfEmpty(att.NamedArguments.Select(arg => new AttributeNamedArgumentModel
        //        {
        //            Name = arg.Key,
        //            Type = arg.Value.Type != null ? TypeHandle(arg.Value.Type, fromVisitor) : default,
        //            Value = AdaptAttrValue(arg.Value.Kind == TypedConstantKind.Array ? arg.Value.Values : arg.Value.Value, fromVisitor),
        //        }).ToArray())
        //    };
        //}

    }
}
