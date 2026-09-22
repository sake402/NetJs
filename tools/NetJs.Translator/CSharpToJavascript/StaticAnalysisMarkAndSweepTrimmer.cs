using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Resources;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace NetJs.Translator.CSharpToJavascript
{
    public class StaticAnalysisMarkAndSweepTrimmer
    {
        GlobalCompilationVisitor _global;
        Dictionary<string, (HashSet<ulong> Sweep, HashSet<ulong> Keep)> states;
        HashSet<ISymbol> kept = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        public List<ISymbol> RootSymbols { get; } = new List<ISymbol>();
        Dictionary<ulong, SymbolMetadata> metadataByHandle;
        public List<ulong> ForceRemove { get; } = new List<ulong>();
        public StaticAnalysisMarkAndSweepTrimmer(GlobalCompilationVisitor global)
        {
            if (global.MainEntry != null)
                RootSymbols.Add(global.MainEntry);
            //RootSymbols.Add(global.Compilation.Assembly);
            _global = global;
            var blazorIComponent = _global.TryGetSymbol("Microsoft.AspNetCore.Components.IComponent", null);
            var layoutComponentBase = _global.TryGetSymbol("Microsoft.AspNetCore.Components.LayoutComponentBase", null);
            states = global.AllSymbols
                .Where(e =>
                {
                    return e.Value.Symbol.Kind == SymbolKind.NamedType ||
                    e.Value.Symbol.Kind == SymbolKind.Field ||
                    e.Value.Symbol.Kind == SymbolKind.Property ||
                    e.Value.Symbol.Kind == SymbolKind.Event ||
                    e.Value.Symbol.Kind == SymbolKind.Method;
                })
                .GroupBy(e => e.Value.Symbol.ContainingAssembly.Name)
                //.Select(ee => ee.ToLookup(eee => eee.Value.OverloadName!.StartsWith('$')))
                .ToDictionary(e => e.Key, e =>
                {
                    var keepLookup = e.ToLookup(eee =>
                    {
                        //Root all blazor page components constructor
                        if (blazorIComponent != null)
                        {
                            bool InheritsFromLayoutComponentBase(INamedTypeSymbol? nt)
                            {
                                while(nt != null)
                                {
                                    if (SymbolEqualityComparer.Default.Equals(nt, layoutComponentBase))
                                        return true;
                                    nt = nt.BaseType;
                                }
                                return false;
                            }
                            var isImportedType = !SymbolEqualityComparer.Default.Equals(_global.Compilation.Assembly, eee.Value.Symbol.ContainingAssembly);
                            if (eee.Value.Symbol.Kind == SymbolKind.Method &&
                                ((IMethodSymbol)eee.Value.Symbol).MethodKind == MethodKind.Constructor &&
                                eee.Value.Symbol.ContainingType.AllInterfaces.Contains(blazorIComponent, SymbolEqualityComparer.Default))
                            {
                                if (_global.HasAttribute(eee.Value.Symbol.ContainingType, "Microsoft.AspNetCore.Components.RouteAttribute", null, false, out _)||
                                    InheritsFromLayoutComponentBase(eee.Value.Symbol.ContainingType))
                                {
                                    RootSymbols.Add(eee.Value.Symbol);
                                    RootSymbols.Add(eee.Value.Symbol.ContainingType);
                                    return true;
                                }
                            }
                        }
                        if (global.HasAttribute(eee.Value.Symbol, typeof(NameAttribute).FullName!, null, false, out var args))
                        {
                            var name = args[0]!.ToString()!;
                            if (name.StartsWith('$'))
                            {
                                //All dollar prefixed symbol are expected to be accessible externally, mark it as root so all its dependencies are not trimmed
                                RootSymbols.Add(eee.Value.Symbol);
                                return true;
                            }
                        }
                        if (global.HasAttribute(eee.Value.Symbol, typeof(TrimmerKeepAttribute).FullName!, null, false, out var argss))
                        {
                            //if (eee.Value.Symbol.Kind == SymbolKind.NamedType &&
                            //    argss.Count == 2 &&
                            //    argss[0] is int filter &&
                            //    filter != (int)DynamicallyAccessedMemberTypes.All)
                            //{
                            //    RootSymbols.Add(eee.Value.Symbol);
                            //    var members = ((INamedTypeSymbol)eee.Value.Symbol).GetMembers();
                            //    foreach (var member in members)
                            //    {
                            //        if (_global.MatchMemberTypes(member, (DynamicallyAccessedMemberTypes)filter))
                            //        {
                            //            RootSymbols.Add(member);
                            //        }
                            //    }
                            //    return true;
                            //}
                            RootSymbols.Add(eee.Value.Symbol);
                            return true;
                        }
                        return false;
                    });
                    var keep = keepLookup[true].Select(e => e.Value.Export.Handle ?? 0).ToHashSet();
                    var sweep = keepLookup[false].Select(e => e.Value.Export.Handle ?? 0).ToHashSet();
                    return (sweep, keep);
                });
            //Dynamic type has a handle of 1(AssemblyHandle|TypeHandle) too because its typehandle is zero
            metadataByHandle = global.AllSymbols.Where(e => !SymbolEqualityComparer.Default.Equals(e.Value.Symbol, global.SystemDynamic) && e.Value.Export.Handle != null && e.Value.Export.Handle != 0).ToDictionary(e => e.Value.Export.Handle!.Value, e => e.Value);
        }

        void Log(SymbolMetadata metadata)
        {
            var handle = metadata.Export.Handle ?? 0;
            var signature = metadata.Signature;
            if (metadata.Symbol.Kind != SymbolKind.NamedType)
            {
                var type = metadata.Symbol.ContainingType;
                if (type != null)
                {
                    var tmetadata = _global.GetMetadata(type);
                    if (tmetadata != null)
                    {
                        signature = tmetadata.Signature + "." + signature;
                    }
                }
            }
            var inReferences = metadataByHandle.Count(s => s.Value.Export.Dependencies?.Contains(handle) ?? false);
            Console.WriteLine($"{metadata.Export.Handle,-16}: {signature}, in:{inReferences}, out:{metadata.Export.Dependencies?.Count ?? 0}");
        }

        bool PrintPathToRoot(ulong handle, SymbolMetadata metadata, HashSet<SymbolMetadata> seen)
        {
            if (metadata.Export.Handle != null && ForceRemove.Contains(metadata.Export.Handle!.Value))
                return false;
            if (!seen.Add(metadata))
                return false;
            if (metadata.Export.Handle == handle)
            {
                Log(metadata);
                return true;
            }
            var dependencies = metadata.Export.Dependencies;
            if (dependencies != null)
            {
                foreach (var dep in dependencies)
                {
                    if (metadataByHandle.TryGetValue(dep, out var depMetadata))
                    {
                        if (PrintPathToRoot(handle, depMetadata, seen))
                        {
                            Log(metadata);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void PrintPathToRoot(ulong handle)
        {
            Console.WriteLine("");
            Console.WriteLine("");
            int found = 0;
            foreach (var root in RootSymbols)
            {
                var metadata = _global.GetMetadata(root);
                if (metadata != null)
                {
                    //Console.WriteLine("");
                    //Console.Write("Path from ");
                    //Log(metadata);
                    var seen = new HashSet<SymbolMetadata>();
                    if (PrintPathToRoot(handle, metadata, seen))
                    {
                        found++;
                        break;
                    }
                }
            }
        }

        public void Keep(ulong handle)
        {
            if (metadataByHandle.TryGetValue(handle, out var metadata))
            {
                Keep(metadata);
                //Keeping a member must keep its containing type
                var contanier = metadata.Symbol.ContainingSymbol;
                int depth = 0;
                while (contanier != null)
                {
                    if (contanier.Kind == SymbolKind.Namespace)
                        break;
                    var cmetadata = _global.GetMetadata(contanier);
                    if (cmetadata?.Export.Handle != null)
                    {
                        Keep(cmetadata);
                    }

                    //Keeping a type must keep its base type and interfaces
                    if (cmetadata != null && cmetadata.Symbol.Kind == SymbolKind.NamedType && cmetadata.Symbol is INamedTypeSymbol nt)
                    {
                        var baseType = nt.BaseType;
                        while (baseType != null)
                        {
                            var bmetadata = _global.GetMetadata(baseType);
                            if (bmetadata?.Export.Handle != null)
                            {
                                Keep(bmetadata);
                            }
                            baseType = baseType.BaseType;
                        }
                        var its = nt.AllInterfaces;
                        foreach (var it in its)
                        {
                            var imetadata = _global.GetMetadata(it);
                            if (imetadata?.Export.Handle != null)
                            {
                                Keep(imetadata);
                            }
                        }
                        //We currently have no way of tracking field reference during profiling. So we keep all fields in the member containing type
                        if (depth == 0)
                        {
                            var fields = nt.GetMembers().Where(e => e.Kind == SymbolKind.Field || e.Kind == SymbolKind.Event);
                            foreach (var f in fields)
                            {
                                var fmetadata = _global.GetMetadata(f);
                                if (fmetadata?.Export.Handle != null)
                                {
                                    Keep(fmetadata);
                                }
                            }
                        }
                    }

                    contanier = contanier.ContainingSymbol;
                    depth++;
                }
            }
        }

        bool IsKept(ulong handle)
        {
            if (metadataByHandle.TryGetValue(handle, out var metadata))
            {
                var asm = metadata.Symbol.ContainingAssembly ?? metadata.Symbol as IAssemblySymbol;
                return states[asm.Name].Keep.Contains(handle);
            }
            return false;
        }

        void Keep(SymbolMetadata metadata)
        {
            var asm = metadata.Symbol.ContainingAssembly ?? metadata.Symbol as IAssemblySymbol;
            if (asm == null)
                return;
            if (!kept.Add(metadata.Symbol)) //already processed symbol, avoid recursion
                return;
            states[asm.Name].Sweep.Remove(metadata.Export.Handle!.Value);
            states[asm.Name].Keep.Add(metadata.Export.Handle.Value);
            if (metadata.Symbol is IAssemblySymbol iasm)
            {
                //dependencies of an assembly are its attribute type, keep them
                var assemblyMetadata = _global.GetRequiredMetadata(iasm).Export;
                if (assemblyMetadata.Dependencies != null)
                {
                    foreach (var a in assemblyMetadata.Dependencies)
                    {
                        Keep(a);
                    }
                }
            }
            //TryKeepOverrideMembers(metadata);
        }

        bool ConstructorKept(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol.IsValueType && kept.Contains(typeSymbol)) //keep default constructor in primitive type like byte, char as long as the type is kept
                return true;
            if (typeSymbol.IsArray(out _))
                return true;
            var constructors = typeSymbol.InstanceConstructors;
            for (int i = 0; i < constructors.Length; i++)
            {
                if (kept.Contains(constructors[i]))
                {
                    return true;
                }
            }
            return false;
        }

        void TryKeepOverrideMembers(SymbolMetadata virtualMember)
        {
            var symbol = virtualMember.Symbol;
            if (symbol == null) return;

            var containingType = symbol.ContainingType;
            if (containingType == null) return;

            if (symbol.Name.EndsWith("MoveNext") && containingType.Name == "IEnumerator")
            {

            }
            // Fast-path short circuit
            bool isPolymorphic = symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride || symbol.ContainingType.TypeKind == TypeKind.Interface;
            //bool isInterfaceMember = containingType.TypeKind == TypeKind.Interface;
            //bool implicitlyImplementsInterfaceMember = symbol.ContainingType
            //    .AllInterfaces
            //    .Any(i => i.GetMembers().Any(m => SymbolEqualityComparer.Default.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(m)?.OriginalDefinition, symbol.OriginalDefinition)));

            if (!isPolymorphic) return;

            // Get only the types that actually implement/inherit the containing type
            if (!inheritanceCache.TryGetValue(containingType.OriginalDefinition, out var inheritors))
            {
                return; // No one inherits or implements this type in the entire codebase
            }

            // Scan ONLY the valid inheritors instead of thousands of unrelated global types
            foreach (var inheritor in inheritors)
            {
                ISymbol? implementationSymbol = null;

                if (symbol.ContainingType.TypeKind == TypeKind.Interface)
                {
                    ISymbol iSymbol = symbol;
                    if (symbol.ContainingType.IsGenericType && SymbolEqualityComparer.Default.Equals(iSymbol.OriginalDefinition, iSymbol))
                    {
                        var symbolContainers = inheritor.AllInterfaces.Where(e => SymbolEqualityComparer.Default.Equals(e.OriginalDefinition, symbol.ContainingType));
                        iSymbol = symbolContainers.SelectMany(e => e.GetMembers()).FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(m.OriginalDefinition, symbol)) ?? iSymbol;
                    }
                    //Interface mapping lookup
                    implementationSymbol = inheritor.FindImplementationForInterfaceMember(iSymbol);
                }
                else
                {
                    // Targeted Member Lookup (No strings, direct kind and origin match)
                    var members = inheritor.GetMembers(symbol.Name);
                    for (int i = 0; i < members.Length; i++)
                    {
                        var m = members[i];
                        if (m.Kind != symbol.Kind) continue;

                        if (m.Kind == SymbolKind.Method && m is IMethodSymbol method)
                        {
                            var current = method.OverriddenMethod;
                            while (current != null)
                            {
                                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, symbol.OriginalDefinition))
                                {
                                    implementationSymbol = m;
                                    break;
                                }
                                current = current.OverriddenMethod;
                            }
                        }
                        else if (m.Kind == SymbolKind.Property && m is IPropertySymbol property)
                        {
                            var current = property.OverriddenProperty;
                            while (current != null)
                            {
                                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, symbol.OriginalDefinition))
                                {
                                    implementationSymbol = m;
                                    break;
                                }
                                current = current.OverriddenProperty;
                            }
                        }
                        else if (m.Kind == SymbolKind.Event && m is IEventSymbol @event)
                        {
                            var current = @event.OverriddenEvent;
                            while (current != null)
                            {
                                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, symbol.OriginalDefinition))
                                {
                                    implementationSymbol = m;
                                    break;
                                }
                                current = current.OverriddenEvent;
                            }
                        }

                        if (implementationSymbol != null) break;
                    }
                }

                if (implementationSymbol == null) continue;

                // Verify if constructor is kept before queuing up dependency markings
                if (ConstructorKept(inheritor))
                {
                    if (_global.SymbolMetadatas.TryGetValue(implementationSymbol, out var overrideMetadata))
                    {
                        if (processed.Add(overrideMetadata))
                        {
                            toProcess.Enqueue(overrideMetadata);
                        }
                    }
                }
            }
        }

        HashSet<SymbolMetadata> processed = new HashSet<SymbolMetadata>();
        Queue<SymbolMetadata> toProcess = new Queue<SymbolMetadata>();
        HashSet<SymbolMetadata> symbolsWithWeakDependencies = new HashSet<SymbolMetadata>();
        Dictionary<ISymbol, List<INamedTypeSymbol>> inheritanceCache = default!;

        void GarbageSymbols()
        {
            static IEnumerable<INamedTypeSymbol> WalkBaseTypes(INamedTypeSymbol t)
            {
                var curr = t.BaseType;
                while (curr != null) { yield return curr; curr = curr.BaseType; }
            }

            inheritanceCache ??= _global.AllSymbols
                .Where(e => e.Value.Symbol is INamedTypeSymbol)
                .Select(e => (INamedTypeSymbol)e.Value.Symbol)
                .SelectMany(t => t.AllInterfaces.Concat(WalkBaseTypes(t)).Select(baseType => new { Base = baseType.OriginalDefinition, Derived = t }))
                .GroupBy(x => x.Base, SymbolEqualityComparer.Default)
                .ToDictionary(g => g.Key!, g => g.Select(x => x.Derived).ToList(), SymbolEqualityComparer.Default);
            processed.Clear();
            toProcess.Clear();
            symbolsWithWeakDependencies.Clear();

            foreach (var root in RootSymbols)
            {
                var metadata = _global.GetMetadata(root);
                if (metadata != null)
                {
                    if (processed.Add(metadata))
                        toProcess.Enqueue(metadata);
                }
            }
            void ProcessQueue()
            {
                while (toProcess.Count > 0)
                {
                    var metadata = toProcess.Dequeue();
                    if (metadata.Export.Handle != null)
                    {
                        Keep(metadata);
                    }
                    // if we are keeping a member, we keep the type
                    var contanier = metadata.Symbol.ContainingSymbol;
                    while (contanier != null)
                    {
                        //if (contanier.Kind == SymbolKind.Namespace)
                        //break;
                        var cmetadata = _global.GetMetadata(contanier);
                        if (cmetadata?.Export.Handle != null)
                        {
                            Keep(cmetadata);
                        }
                        contanier = contanier.ContainingSymbol;
                    }
                    var dependencies = metadata.Export.Dependencies;
                    if (dependencies != null)
                    {
                        foreach (var dep in dependencies)
                        {
                            if (metadataByHandle.TryGetValue(dep, out var depMetadata))
                            {
                                if (processed.Add(depMetadata))
                                    toProcess.Enqueue(depMetadata);
                            }
                        }
                    }
                    if (metadata.Export.WeakDependencies?.Any() ?? false)
                    {
                        symbolsWithWeakDependencies.Add(metadata);
                    }
                }
            }
            void ProcessWeakReferences()
            {
                //we have kept all hard references, now we evaluate weak references and if the condition is satisfied we keep
                //We keep doing this to pull all referenced weak reference in
                while (symbolsWithWeakDependencies.Count > 0)
                {
                    //Clone the current weak dependencies and clear
                    var clonedSymbolsWithWeakDependencies = symbolsWithWeakDependencies.ToHashSet();
                    symbolsWithWeakDependencies.Clear();
                    foreach (var mt in clonedSymbolsWithWeakDependencies)
                    {
                        var dependencies = mt.Export.WeakDependencies;
                        if (dependencies != null)
                        {
                            foreach (var dep in dependencies)
                            {
                                if (metadataByHandle.TryGetValue(dep.Handle, out var depMetadata))
                                {
                                    switch (dep.Condition)
                                    {
                                        case KeepDependencyFlags.TargetConstructorCalled:
                                            var ctors = depMetadata.Symbol.ContainingType.GetMembers(".ctor");
                                            foreach (var ctor in ctors)
                                            {
                                                if (kept.Contains(ctor))
                                                {
                                                    if (processed.Add(depMetadata))
                                                        toProcess.Enqueue(depMetadata);
                                                }
                                            }
                                            break;
                                        case KeepDependencyFlags.TargetTypeUsed:
                                            if (kept.Contains(depMetadata.Symbol.ContainingType))
                                            {
                                                if (processed.Add(depMetadata))
                                                    toProcess.Enqueue(depMetadata);
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            HashSet<ISymbol> processedOverride = new(SymbolEqualityComparer.Default);
            void ProcessOverrides()
            {
                foreach (var kp in kept.ToList())
                {
                    if (_global.SymbolMetadatas.TryGetValue(kp, out var metadata))
                        //if (processedOverride.Add(metadata.Symbol))
                        TryKeepOverrideMembers(metadata);
                }
            }
            int originalKept = 0;
            while (toProcess.Count > 0 || symbolsWithWeakDependencies.Count > 0 || originalKept != kept.Count)
            {
                originalKept = kept.Count;
                ProcessQueue();
                ProcessWeakReferences();
                ProcessOverrides();
            }

            //foreach (var kp in kept.ToList())
            //{
            //    if (_global.SymbolMetadatas.TryGetValue(kp, out var metadata))
            //        TryKeepOverrideMembers(metadata, processed, toProcess, inheritanceCache);
            //}

            //while (toProcess.Count > 0 || symbolsWithWeakDependencies.Count > 0)
            //{
            //    ProcessQueue();
            //    ProcessWeakReferences();
            //}
        }

        //public Task RunTrimming(IProjectOutputProvider outputProvider, IEnumerable<string> jsAssemblyFiles)
        //{
        //    GarbageSymbols();

        //    ReadOnlySpan<char> startMarkerText = "//--TRIMMABLE-BOUNDARY-START(".AsSpan();
        //    ReadOnlySpan<char> endMarkerText = "//--TRIMMABLE-BOUNDARY-END(".AsSpan();

        //    List<Task> pendingTasks = new();

        //    foreach (var assembly in jsAssemblyFiles)
        //    {
        //        var assemblyName = Path.GetFileNameWithoutExtension(assembly);
        //        if (!states.TryGetValue(assemblyName, out var state))
        //            continue;

        //        var filePath = Path.Combine(outputProvider.OutputPath, assembly);
        //        var fileContent = File.ReadAllText(filePath);
        //        var originalLen = fileContent.Length;

        //        ReadOnlySpan<char> fileSpan = fileContent.AsSpan();

        //        var intervalsToRemove = new List<(int Start, int End)>();
        //        var typesSwept = new HashSet<ulong>();

        //        // Track the hierarchy of nested markers we are currently standing inside
        //        // Handle stored on stack represents the PRIMARY handle (first number) used for pairing END tags
        //        var nestingStack = new Stack<(ulong PrimaryHandle, int LineStartIndex, bool IsBeingTrimmed)>();

        //        int position = 0;
        //        while (position < fileSpan.Length)
        //        {
        //            ReadOnlySpan<char> remainder = fileSpan.Slice(position);

        //            int nextStart = remainder.IndexOf(startMarkerText, StringComparison.Ordinal);
        //            int nextEnd = remainder.IndexOf(endMarkerText, StringComparison.Ordinal);

        //            if (nextStart == -1 && nextEnd == -1) break;

        //            if (nextStart != -1 && (nextEnd == -1 || nextStart < nextEnd))
        //            {
        //                // --- PROCESS START MARKER ---
        //                int markerIndexInFile = position + nextStart;
        //                int handleStartIndex = markerIndexInFile + startMarkerText.Length;

        //                int trueClosingParen = fileContent.IndexOf(')', handleStartIndex);
        //                if (trueClosingParen != -1)
        //                {
        //                    var paramContent = fileContent.AsSpan(handleStartIndex, trueClosingParen - handleStartIndex);

        //                    if (TryParseMarkerPayload(paramContent, state.Sweep, out ulong primaryHandle, out bool targetSweepMatch))
        //                    {
        //                        bool shouldTrimThisNode = false;

        //                        if (nestingStack.Count > 0 && nestingStack.Peek().IsBeingTrimmed)
        //                        {
        //                            shouldTrimThisNode = true;
        //                        }
        //                        else if (targetSweepMatch)
        //                        {
        //                            var typeHandle = primaryHandle & ReflectionHandleExtension.TypeMask;
        //                            shouldTrimThisNode = true;

        //                            // Check if the primary type was already globally swept
        //                            if ((primaryHandle & ReflectionHandleExtension.MemberMask) != 0UL && typesSwept.Contains(typeHandle))
        //                            {
        //                                shouldTrimThisNode = false;
        //                            }
        //                        }

        //                        // Adjust start backward to the absolute beginning of the comment line
        //                        int lineStartIndex = markerIndexInFile;
        //                        while (lineStartIndex > 0 && fileContent[lineStartIndex - 1] != '\n' && fileContent[lineStartIndex - 1] != '\r')
        //                        {
        //                            lineStartIndex--;
        //                        }

        //                        nestingStack.Push((primaryHandle, lineStartIndex, shouldTrimThisNode));
        //                    }

        //                    position = trueClosingParen + 1;
        //                }
        //                else
        //                {
        //                    position = handleStartIndex;
        //                }
        //            }
        //            else
        //            {
        //                // --- PROCESS END MARKER ---
        //                int markerIndexInFile = position + nextEnd;
        //                int handleStartIndex = markerIndexInFile + endMarkerText.Length;

        //                int trueClosingParen = fileContent.IndexOf(')', handleStartIndex);
        //                if (trueClosingParen != -1)
        //                {
        //                    // Adjust end forward to include everything remaining on this line
        //                    int fullEndIndex = trueClosingParen + 1;
        //                    while (fullEndIndex < originalLen && fileContent[fullEndIndex] != '\n' && fileContent[fullEndIndex] != '\r')
        //                    {
        //                        fullEndIndex++;
        //                    }
        //                    if (fullEndIndex < originalLen && fileContent[fullEndIndex] == '\r') fullEndIndex++;
        //                    if (fullEndIndex < originalLen && fileContent[fullEndIndex] == '\n') fullEndIndex++;

        //                    var paramContent = fileContent.AsSpan(handleStartIndex, trueClosingParen - handleStartIndex);

        //                    // Note: state.Sweep lookup pass is omitted here because targetSweepMatch is irrelevant on the END tag context
        //                    if (TryParseMarkerPayload(paramContent, null, out ulong primaryHandle, out _))
        //                    {
        //                        if (nestingStack.Count > 0 && nestingStack.Peek().PrimaryHandle == primaryHandle)
        //                        {
        //                            var popped = nestingStack.Pop();

        //                            if (popped.IsBeingTrimmed)
        //                            {
        //                                if (nestingStack.Count == 0 || !nestingStack.Peek().IsBeingTrimmed)
        //                                {
        //                                    intervalsToRemove.Add((popped.LineStartIndex, fullEndIndex));
        //                                }

        //                                if ((primaryHandle & ReflectionHandleExtension.MemberMask) == 0UL)
        //                                {
        //                                    var typeHandle = primaryHandle & ReflectionHandleExtension.TypeMask;
        //                                    typesSwept.Add(typeHandle);
        //                                }
        //                            }
        //                        }
        //                    }
        //                    position = trueClosingParen + 1;
        //                }
        //                else
        //                {
        //                    position = handleStartIndex;
        //                }
        //            }
        //        }

        //        // Reconstruct file if items need to be removed
        //        if (intervalsToRemove.Count > 0)
        //        {
        //            var optimizedBuilder = new StringBuilder(originalLen);
        //            int currentPointer = 0;

        //            foreach (var interval in intervalsToRemove)
        //            {
        //                optimizedBuilder.Append(fileContent, currentPointer, interval.Start - currentPointer);
        //                currentPointer = interval.End;
        //            }

        //            if (currentPointer < originalLen)
        //            {
        //                optimizedBuilder.Append(fileContent, currentPointer, originalLen - currentPointer);
        //            }

        //            fileContent = optimizedBuilder.ToString();
        //        }

        //        // Task pipeline execution hooks
        //        var outFilePath = Path.ChangeExtension(assembly, ".trim.js");
        //        pendingTasks.Add(outputProvider.Output(_global, outFilePath, new MemoryStream(Encoding.UTF8.GetBytes(fileContent))));

        //        var minified = NUglify.Uglify.Js(fileContent, new NUglify.JavaScript.CodeSettings
        //        {
        //            IgnoreErrorList = "JS1306"
        //        });

        //        var laxEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        //        byte[] fileBytes = laxEncoding.GetBytes(minified.Code);
        //        var uglyStream = new MemoryStream(fileBytes);
        //        var uglyFileName = Path.ChangeExtension(assembly, ".trim.min.js");
        //        pendingTasks.Add(outputProvider.Output(_global, uglyFileName, uglyStream));
        //    }

        //    return Task.WhenAll(pendingTasks);
        //}

        private bool TryParseMarkerPayload(
            ReadOnlySpan<char> payload,
            HashSet<ulong>? sweepCollection,
            out ulong[] handles,
            out bool sweep,
            out bool primaryHandleSweep,
            out bool virtualHandleSweep)
        {
            handles = Array.Empty<ulong>();
            sweep = false;
            primaryHandleSweep = false;
            virtualHandleSweep = false;

            payload = payload.Trim();
            if (payload.Length == 0) return false;

            bool evaluateAll = payload.StartsWith("ALL", StringComparison.OrdinalIgnoreCase);
            bool evaluateAny = payload.StartsWith("ANY", StringComparison.OrdinalIgnoreCase);

            if (evaluateAll || evaluateAny)
            {
                // Slice out the directive ("ALL" or "ANY") to read the following comma-separated list
                ReadOnlySpan<char> listContent = payload.Slice(3).TrimStart(',');

                // Use conditional defaults: ALL mode defaults to true (failing if an exception is missing), ANY defaults to false
                sweep = evaluateAll;
                List<ulong> handless = new();
                while (listContent.Length > 0)
                {
                    int commaIdx = listContent.IndexOf(',');
                    ReadOnlySpan<char> segment = commaIdx == -1 ? listContent : listContent.Slice(0, commaIdx);
                    segment = segment.Trim();

                    if (ulong.TryParse(segment, out ulong currentHandle))
                    {
                        if (sweepCollection != null)
                        {
                            bool iSweep = sweepCollection.Contains(currentHandle);
                            if (handless.Count == 0 && iSweep)
                            {
                                primaryHandleSweep = true;
                            }
                            else if (handless.Count != 0 && iSweep)
                            {
                                virtualHandleSweep = true;
                            }
                            if (evaluateAll) sweep &= iSweep;
                            if (evaluateAny) sweep |= iSweep;
                        }
                        handless.Add(currentHandle);
                    }

                    listContent = commaIdx == -1 ? ReadOnlySpan<char>.Empty : listContent.Slice(commaIdx + 1);
                }
                handles = handless.ToArray();
                return handles.Length > 0;
            }
            else
            {
                // Default Strategy: Extracts the first available index as normal
                int commaIdx = payload.IndexOf(',');
                ReadOnlySpan<char> firstParam = commaIdx == -1 ? payload : payload.Slice(0, commaIdx);
                firstParam = firstParam.Trim();
                if (ulong.TryParse(firstParam, out var primaryHandle))
                {
                    if (sweepCollection != null)
                    {
                        sweep = sweepCollection.Contains(primaryHandle);
                    }
                    handles = [primaryHandle];
                    return true;
                }
                return false;
            }
        }

        public Task RunTrimming(IProjectOutputProvider outputProvider, IEnumerable<string> jsAssemblyFiles, bool runGarbageSymbols = true, bool trimTraceHooks = true)
        {
            if (runGarbageSymbols)
                GarbageSymbols();

            ReadOnlySpan<char> startMarkerText = "//--TRIMMABLE-BOUNDARY-START(".AsSpan();
            ReadOnlySpan<char> endMarkerText = "//--TRIMMABLE-BOUNDARY-END(".AsSpan();
            char[] handleTerminators = [')', ','];

            List<Task> pendingTasks = new();

            foreach (var assembly in jsAssemblyFiles)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assembly);
                if (!states.TryGetValue(assemblyName, out var state))
                    continue;
                var filePath = Path.Combine(outputProvider.OutputPath, assembly);
                var fileContent = File.ReadAllText(filePath);
                var originalLen = fileContent.Length;

                ReadOnlySpan<char> fileSpan = fileContent.AsSpan();

                var intervalsToRemove = new List<(int Start, int End)>();
                var typesSwept = new HashSet<ulong>();

                // Tracks hierarchy. For TRACE elements, we flag IsTrace = true and leave PrimaryHandle as 0UL.
                var nestingStack = new Stack<(ulong PrimaryHandle, int LineStartIndex, bool IsBeingTrimmed, bool IsTrace)>();

                int position = 0;
                while (position < fileSpan.Length)
                {
                    ReadOnlySpan<char> remainder = fileSpan.Slice(position);

                    int nextStart = remainder.IndexOf(startMarkerText, StringComparison.Ordinal);
                    int nextEnd = remainder.IndexOf(endMarkerText, StringComparison.Ordinal);

                    if (nextStart == -1 && nextEnd == -1) break;

                    if (nextStart != -1 && (nextEnd == -1 || nextStart < nextEnd))
                    {
                        // --- PROCESS START MARKER ---
                        int markerIndexInFile = position + nextStart;
                        int handleStartIndex = markerIndexInFile + startMarkerText.Length;

                        int trueClosingParen = fileContent.IndexOf(')', handleStartIndex);
                        if (trueClosingParen != -1)
                        {
                            var paramContent = fileContent.AsSpan(handleStartIndex, trueClosingParen - handleStartIndex).Trim();

                            // Check if this is a trace boundary spec
                            if (paramContent.Equals("TRACE", StringComparison.OrdinalIgnoreCase))
                            {
                                bool shouldTrimThisNode = trimTraceHooks || (nestingStack.Count > 0 && nestingStack.Peek().IsBeingTrimmed);

                                int lineStartIndex = markerIndexInFile;
                                while (lineStartIndex > 0 && fileContent[lineStartIndex - 1] != '\n' && fileContent[lineStartIndex - 1] != '\r')
                                {
                                    lineStartIndex--;
                                }

                                nestingStack.Push((0UL, lineStartIndex, shouldTrimThisNode, true));
                            }
                            else if (TryParseMarkerPayload(paramContent, state.Sweep, out var handles, out bool sweep, out bool primaryHandleSweep, out bool virtualHandleSweep))
                            {
                                var primaryHandle = handles[0];
                                bool shouldTrimThisNode = false;

                                if (nestingStack.Count > 0 && nestingStack.Peek().IsBeingTrimmed)
                                {
                                    shouldTrimThisNode = true;
                                }
                                else if (sweep)
                                {
                                    var typeHandle = primaryHandle & ReflectionHandleExtension.TypeMask;
                                    shouldTrimThisNode = true;

                                    if ((primaryHandle & ReflectionHandleExtension.MemberMask) != 0UL && typesSwept.Contains(typeHandle))
                                    {
                                        shouldTrimThisNode = false;
                                    }
                                }
                                else if (primaryHandleSweep && !virtualHandleSweep && handles.Length > 1) //member is not referenced directly, so swept. But its overriden memeber is referenced
                                {
                                    if (metadataByHandle.TryGetValue(primaryHandle, out var metadata) && metadata.Symbol.ContainingType != null)
                                    {
                                        if (!ConstructorKept(metadata.Symbol.ContainingType))
                                        {
                                            shouldTrimThisNode = true;
                                        }
                                    }
                                }

                                int lineStartIndex = markerIndexInFile;
                                while (lineStartIndex > 0 && fileContent[lineStartIndex - 1] != '\n' && fileContent[lineStartIndex - 1] != '\r')
                                {
                                    lineStartIndex--;
                                }

                                nestingStack.Push((primaryHandle, lineStartIndex, shouldTrimThisNode, false));
                            }

                            position = trueClosingParen + 1;
                        }
                        else
                        {
                            position = handleStartIndex;
                        }
                    }
                    else
                    {
                        // --- PROCESS END MARKER ---
                        int markerIndexInFile = position + nextEnd;
                        int handleStartIndex = markerIndexInFile + endMarkerText.Length;

                        int trueClosingParen = fileContent.IndexOf(')', handleStartIndex);
                        if (trueClosingParen != -1)
                        {
                            int fullEndIndex = trueClosingParen + 1;
                            while (fullEndIndex < originalLen && fileContent[fullEndIndex] != '\n' && fileContent[fullEndIndex] != '\r')
                            {
                                fullEndIndex++;
                            }
                            if (fullEndIndex < originalLen && fileContent[fullEndIndex] == '\r') fullEndIndex++;
                            if (fullEndIndex < originalLen && fileContent[fullEndIndex] == '\n') fullEndIndex++;

                            var paramContent = fileContent.AsSpan(handleStartIndex, trueClosingParen - handleStartIndex).Trim();
                            bool isEndTrace = paramContent.Equals("TRACE", StringComparison.OrdinalIgnoreCase);

                            if (nestingStack.Count > 0)
                            {
                                var peekNode = nestingStack.Peek();
                                bool matches = isEndTrace
                                    ? peekNode.IsTrace
                                    : (!peekNode.IsTrace && TryParseMarkerPayload(paramContent, null, out var handles, out _, out _, out _) && peekNode.PrimaryHandle == handles[0]);

                                if (matches)
                                {
                                    var popped = nestingStack.Pop();

                                    if (popped.IsBeingTrimmed)
                                    {
                                        if (nestingStack.Count == 0 || !nestingStack.Peek().IsBeingTrimmed)
                                        {
                                            intervalsToRemove.Add((popped.LineStartIndex, fullEndIndex));
                                        }

                                        if (!popped.IsTrace && (popped.PrimaryHandle & ReflectionHandleExtension.MemberMask) == 0UL)
                                        {
                                            var typeHandle = popped.PrimaryHandle & ReflectionHandleExtension.TypeMask;
                                            typesSwept.Add(typeHandle);
                                        }
                                    }
                                }
                            }
                            position = trueClosingParen + 1;
                        }
                        else
                        {
                            position = handleStartIndex;
                        }
                    }
                }

                if (state.Keep.Count <= 1) //keeping only the assembly handle, we can remove the entire file if no other handles are kept
                {
                    fileContent = "";
                }
                // Reconstruct file if items need to be removed
                else if (intervalsToRemove.Count > 0)
                {
                    var optimizedBuilder = new StringBuilder(originalLen);
                    int currentPointer = 0;

                    foreach (var interval in intervalsToRemove)
                    {
                        optimizedBuilder.Append(fileContent, currentPointer, interval.Start - currentPointer);
                        currentPointer = interval.End;
                    }

                    if (currentPointer < originalLen)
                    {
                        optimizedBuilder.Append(fileContent, currentPointer, originalLen - currentPointer);
                    }

                    fileContent = optimizedBuilder.ToString();
                }

                var outFilePath = Path.ChangeExtension(assembly, ".trimmed.js");
                pendingTasks.Add(outputProvider.Output(_global, outFilePath, new MemoryStream(Encoding.UTF8.GetBytes(fileContent))));

                var minified = NUglify.Uglify.Js(fileContent, new NUglify.JavaScript.CodeSettings
                {
                    IgnoreErrorList = "JS1306"
                });

                var laxEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
                byte[] fileBytes = laxEncoding.GetBytes(minified.Code);
                var uglyStream = new MemoryStream(fileBytes);
                var uglyFileName = Path.ChangeExtension(assembly, ".trimmed.min.js");
                pendingTasks.Add(outputProvider.Output(_global, uglyFileName, uglyStream));
            }

            return Task.WhenAll(pendingTasks);
        }

    }
}