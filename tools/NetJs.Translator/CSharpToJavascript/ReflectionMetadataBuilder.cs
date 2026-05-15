using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace NetJs.Translator.CSharpToJavascript
{

    public class ReflectionMetadataBuilder
    {
        GlobalCompilationVisitor global;
        bool isSystemPrivateCoreLib;
        string[] embeddedFiles;
        string[] resxFiles;

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
        string[] typeNames = default!;
        ulong TypeHandle(ITypeSymbol type)
        {
            //if (type.IsArray(out var elementType))
            //{
            //    var th = TypeHandle(elementType);
            //    return new ReflectionHandleModel { Value = th.Value | (ulong)TypeHandleFlags.Array };
            //}
            var name = type.CreateFullTypeName(global, withGlobalNamespace: false);
            int typeHandle = Array.IndexOf(typeNames, name);
            if (typeHandle < 0)
                return 0;
            return (assemblyHandle << ReflectionHandleExtension.AssemblyShift) | ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
        }

        ulong GenericTypeHandle(int typeIndex)
        {
            var typeHandle = typeIndex + (int)KnownTypeHandle.GenericType1Placeholder;
            //if (type.IsArray(out var elementType))
            //{
            //    var th = TypeHandle(elementType);
            //    return new ReflectionHandleModel { Value = th.Value | (ulong)TypeHandleFlags.Array };
            //}
            return (assemblyHandle << ReflectionHandleExtension.AssemblyShift) | ((ulong)typeHandle << ReflectionHandleExtension.TypeShift);
        }

        ulong MemberHandle(ISymbol type)
        {
            var typeHandle = TypeHandle(type.ContainingType);
            var index = type.ContainingType.GetMembers().IndexOf(type);
            return typeHandle | ((ulong)index << ReflectionHandleExtension.MemberShift);
        }

        KnownTypeHandle KnownTypeFromName(string t)
        {
            return t == "System.Object" ? KnownTypeHandle.SystemObject :
                        t == "System.Boolean" ? KnownTypeHandle.SystemBool :
                        t == "System.Char" ? KnownTypeHandle.SystemChar :
                        t == "System.SByte" ? KnownTypeHandle.SystemSByte :
                        t == "System.Byte" ? KnownTypeHandle.SystemByte :
                        t == "System.Int16" ? KnownTypeHandle.SystemInt16 :
                        t == "System.UInt16" ? KnownTypeHandle.SystemUInt16 :
                        t == "System.Int32" ? KnownTypeHandle.SystemInt32 :
                        t == "System.UInt32" ? KnownTypeHandle.SystemUint32 :
                        t == "System.IntPtr" ? KnownTypeHandle.SystemIntPtr :
                        t == "System.UIntPtr" ? KnownTypeHandle.SystemUintPtr :
                        t == "System.Int64" ? KnownTypeHandle.SystemInt64 :
                        t == "System.UInt64" ? KnownTypeHandle.SystemUint64 :
                        t == "System.Float" ? KnownTypeHandle.SystemFloat :
                        t == "System.Single" ? KnownTypeHandle.SystemSingle :
                        t == "System.Double" ? KnownTypeHandle.SystemDouble :
                        t == "System.String" ? KnownTypeHandle.SystemString :
                        t == "System.Array" ? KnownTypeHandle.SystemArray : KnownTypeHandle.Unknown;
        }

        public AssemblyModel FromAssemblySymbol(IAssemblySymbol assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (!global.HasAttribute(assembly, typeof(AssemblyHandleAttribute).FullName, null, false, out var args))
            {
                //throw new InvalidOperationException("An AssemblyHandleAttribute must be defined on all assembly");
            }
            assemblyHandle = (uint)(args?.ElementAtOrDefault(0).Value ?? (isSystemPrivateCoreLib ? 1 : (uint)new Random().Next(32768, 65536)));
            //uint assemblyHandle = (uint)args[0];
            var types = GetAllTypes(assembly.GlobalNamespace).Concat(assembly.GlobalNamespace
                    .GetNamespaceMembers()
                    .SelectMany(GetAllTypes));
            typeNames = new string[] { "" }.Concat(Enumerable.Range(1, isSystemPrivateCoreLib ? 32 : 0).Select(i => $"$T{i}")).Concat(types.Select(t => t.CreateFullTypeName(global, withGlobalNamespace: false))).ToArray();
            //make sure unknown type is index zero, System.Object is at index 1
            typeNames = typeNames.OrderBy(t =>
            t == "" ? (int)KnownTypeHandle.Unknown :
            t == "System.Object" ? (int)KnownTypeHandle.SystemObject :
            t == "System.Boolean" ? (int)KnownTypeHandle.SystemBool :
            t == "System.Char" ? (int)KnownTypeHandle.SystemChar :
            t == "System.SByte" ? (int)KnownTypeHandle.SystemSByte :
            t == "System.Byte" ? (int)KnownTypeHandle.SystemByte :
            t == "System.Int16" ? (int)KnownTypeHandle.SystemInt16 :
            t == "System.UInt16" ? (int)KnownTypeHandle.SystemUInt16 :
            t == "System.Int32" ? (int)KnownTypeHandle.SystemInt32 :
            t == "System.UInt32" ? (int)KnownTypeHandle.SystemUint32 :
            t == "System.Int64" ? (int)KnownTypeHandle.SystemInt64 :
            t == "System.UInt64" ? (int)KnownTypeHandle.SystemUint64 :
            t == "System.Float" ? (int)KnownTypeHandle.SystemFloat :
            t == "System.Single" ? (int)KnownTypeHandle.SystemSingle :
            t == "System.Double" ? (int)KnownTypeHandle.SystemDouble :
            t == "System.Array" ? (int)KnownTypeHandle.SystemArray :
            t == "System.String" ? (int)KnownTypeHandle.SystemString :
            t == "$T1" ? (int)KnownTypeHandle.GenericType1Placeholder :
            t == "$T2" ? (int)KnownTypeHandle.GenericType2Placeholder :
            t == "$T3" ? (int)KnownTypeHandle.GenericType3Placeholder :
            t == "$T4" ? (int)KnownTypeHandle.GenericType4Placeholder :
            t == "$T5" ? (int)KnownTypeHandle.GenericType5Placeholder :
            t == "$T6" ? (int)KnownTypeHandle.GenericType6 :
            t == "$T7" ? (int)KnownTypeHandle.GenericType7 :
            t == "$T8" ? (int)KnownTypeHandle.GenericType8 :
            t == "$T9" ? (int)KnownTypeHandle.GenericType9 :
            t == "$T10" ? (int)KnownTypeHandle.GenericType10 :
            t == "$T11" ? (int)KnownTypeHandle.GenericType11 :
            t == "$T12" ? (int)KnownTypeHandle.GenericType12 :
            t == "$T13" ? (int)KnownTypeHandle.GenericType13 :
            t == "$T14" ? (int)KnownTypeHandle.GenericType14 :
            t == "$T15" ? (int)KnownTypeHandle.GenericType15 :
            t == "$T16" ? (int)KnownTypeHandle.GenericType16 :
            t == "$T17" ? (int)KnownTypeHandle.GenericType17 :
            t == "$T18" ? (int)KnownTypeHandle.GenericType18 :
            t == "$T19" ? (int)KnownTypeHandle.GenericType19 :
            t == "$T20" ? (int)KnownTypeHandle.GenericType20 :
            t == "$T21" ? (int)KnownTypeHandle.GenericType21 :
            t == "$T22" ? (int)KnownTypeHandle.GenericType22 :
            t == "$T23" ? (int)KnownTypeHandle.GenericType23 :
            t == "$T24" ? (int)KnownTypeHandle.GenericType24 :
            t == "$T25" ? (int)KnownTypeHandle.GenericType25 :
            t == "$T26" ? (int)KnownTypeHandle.GenericType26 :
            t == "$T27" ? (int)KnownTypeHandle.GenericType27 :
            t == "$T28" ? (int)KnownTypeHandle.GenericType28 :
            t == "$T29" ? (int)KnownTypeHandle.GenericType29 :
            t == "$T30" ? (int)KnownTypeHandle.GenericType30 :
            t == "$T31" ? (int)KnownTypeHandle.GenericType31Placeholder :
            t == "$T32" ? (int)KnownTypeHandle.GenericType32Placeholder :
            int.MaxValue).ToArray();
            var model = new AssemblyModel
            {
                AssemblyFlags = global.MainEntry != null ? System.AssemblyFlags.Entry : System.AssemblyFlags.None,
                Handle = assemblyHandle,
                FullName = assembly.Identity.Name.Replace("NetJs.", ""),
                Version = assembly.Identity.Version?.ToString() ?? "0.0.0.0",
                TypeNames = typeNames,
                Types = new ITypeSymbol?[] { null }.Concat(types)
                    .Select(FromTypeSymbol)
                    .ToArray(),
                Attributes = NullIfEmpty(assembly.GetAttributes()
                    .Where(a => a.AttributeClass != null)
                    .Where(a => global.ShouldExportType(a.AttributeClass!, null))
                    .Select(s => FromAttribute(s))
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

        public TypeModel FromTypeSymbol(ITypeSymbol? symbol)
        {
            if (symbol == null) return new TypeModel { };
            var model = new TypeModel
            {
                //Name = symbol.Name,
                Handle = TypeHandle(symbol),
                //AssemblyQualifiedName = $"{RemoveGlobal(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}, {symbol.ContainingAssembly?.Name}",
                BaseType = symbol.BaseType != null ? TypeHandle(symbol.BaseType) : null,
                DeclaringType = symbol.ContainingType != null ? TypeHandle(symbol.ContainingType) : default,
                UnderlyingType = (symbol is INamedTypeSymbol nt && nt.EnumUnderlyingType != null)
                    ? TypeHandle(nt.EnumUnderlyingType)
                    : null,
                Kind = MapTypeKind(symbol.TypeKind),
                Flags = symbol.GetTypeFlags(),
                //TypeAttributes = 0,
                KnownType = KnownTypeFromName(symbol.CreateFullTypeName(global, withGlobalNamespace: false)),
                Properties = NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                            .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .OfType<IPropertySymbol>().Select(FromPropertySymbol).ToArray()),
                Methods = NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                            .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .OfType<IMethodSymbol>()
                            .Where(m => m.MethodKind == MethodKind.Ordinary)
                            .Where(m => !global.LinkTrimOutMethod(m))
                            .Select(e => FromMethodSymbol(e))
                            .ToArray()),
                Constructors = NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                            .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .OfType<IMethodSymbol>()
                            .Where(m => m.MethodKind == MethodKind.Constructor)
                            .Where(m => !global.LinkTrimOutMethod(m))
                            .Select(FromConstructorSymbol).ToArray()),
                Fields = NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                            .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .Where(m => !m.Name.Contains("k__BackingField")/*Property backing fields are not needed*/)
                            .OfType<IFieldSymbol>().Select(FromFieldSymbol).ToArray()),
                Events = NullIfEmpty(symbol.GetMembers()
                            .Where(m => global.IsReflectable(m, null))
                             .Where(m => !m.IsExtern/*Extern methods are called via templates, not reflectable*/)
                            //.Where(m => !m.DeclaredAccessibility.HasFlag(Accessibility.Internal)/*Internal methods are used by compiler only*/)
                            .OfType<IEventSymbol>().Select(FromEventSymbol).ToArray()),
                Interfaces = NullIfEmpty(symbol.AllInterfaces.Where(i => global.ShouldExportType(i, null)).Select(i => TypeHandle(i)).ToArray()),
                Attributes = NullIfEmpty(symbol.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a)).ToArray()),
                GenericArguments = NullIfEmpty(symbol is INamedTypeSymbol g && g.TypeArguments.Any()
                    ? g.TypeArguments.Select((t, i) =>
                    {
                        var handle = TypeHandle(t);
                        if (handle == 0)
                        {
                            handle = GenericTypeHandle(i);
                        }
                        return handle;
                    }).ToArray()
                    : Array.Empty<ulong>()),
                GenericConstraints = NullIfEmpty(Array.Empty<GenericParameterConstraintModel>()),
                NestedTypes = NullIfEmpty(symbol.GetTypeMembers().Where(t => global.ShouldExportType(t, null) && global.IsReflectable(t, null)).Select(t => TypeHandle(t)).ToArray()),
                GenericParameterCount = symbol is INamedTypeSymbol ng ? ng.TypeParameters.Length : 0,
                Size = symbol.SizeOf()
            };

            return model;
        }

        // --- Internal helpers ---
        private IEnumerable<INamedTypeSymbol> GetInnerTypes(ITypeSymbol ns)
        {
            foreach (var nested in ns.GetTypeMembers())
            {
                if (global.ShouldExportType(nested, null) && global.IsReflectable(nested, null))
                    yield return nested;
                foreach (var inner in GetInnerTypes(nested))
                    if (global.ShouldExportType(inner, null) && global.IsReflectable(inner, null))
                        yield return inner;
            }
        }
        private IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
        {
            foreach (var type in ns.GetTypeMembers())
            {
                if (global.ShouldExportType(type, null) && global.IsReflectable(type, null))
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

        private static TypeKindModel MapTypeKind(Microsoft.CodeAnalysis.TypeKind roslynKind) =>
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

        PropertyModel FromPropertySymbol(IPropertySymbol prop)
        {
            var name = prop.Name;
            //a method that implements explicitly will have a long name qualified by the interface it is defined on
            //Let's shrink it by removing the interface name
            if (prop.ExplicitInterfaceImplementations.Any())
            {
                var ex = prop.ExplicitInterfaceImplementations.First().ContainingType;
                var handle = TypeHandle(ex);
                name = (handle > 0 ? $"{{{handle}}}." : "") + name.Split('.').Last();
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
            var propertyTypeHandle = !global.ShouldExportType(prop.Type, null) ? default : TypeHandle(prop.Type);
            if (propertyTypeHandle == 0 && prop.ContainingType.Arity > 0)
            {
                var args = prop.ContainingType.TypeArguments;
                var index = args.IndexOf(prop.Type, 0, SymbolEqualityComparer.Default);
                if (index >= 0)
                    propertyTypeHandle = GenericTypeHandle(index);
            }
            return new PropertyModel
            {
                Name = name,
                OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
                DeclaringType = TypeHandle(prop.ContainingType),
                Flags = prop.GetSymbolFlags(),
                PropertyType = propertyTypeHandle,
                IndexParameters = prop.Parameters.Select(FromParameterSymbol).ToArray(),
                GetMethod = prop.GetMethod != null ? FromMethodSymbol(prop.GetMethod, prop) : null,
                SetMethod = prop.SetMethod != null ? FromMethodSymbol(prop.SetMethod, prop) : null,
                Handle = MemberHandle(prop),
                Attributes = NullIfEmpty(prop.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a)).ToArray())
            };
        }

        static string? NullIfVoid(string t)
        {
            if (t == "void")
                return null;
            return t;
        }

        MethodModel FromMethodSymbol(IMethodSymbol method, IPropertySymbol? fromProperty = null)
        {
            var name = method.Name;
            //a method that implements explicitly will have a long name qualified by the interface it is defined on
            //Let's shrink it by removing the interface name
            if (method.ExplicitInterfaceImplementations.Any())
            {
                var ex = method.ExplicitInterfaceImplementations.First().ContainingType;
                var handle = TypeHandle(ex);
                name = (handle > 0 ? $"{{{handle}}}." : "") + name.Split('.').Last();
            }
            var metadata = global.GetRequiredMetadata(method);
            var outputName = metadata.OverloadName ?? name;
            var methodReturnTypeHandle = !global.ShouldExportType(method.ReturnType, null) ? default : TypeHandle(method.ReturnType);
            if (methodReturnTypeHandle == 0 && method.ContainingType.Arity > 0)
            {
                var args = method.ContainingType.TypeArguments;
                var index = args.IndexOf(method.ReturnType, 0, SymbolEqualityComparer.Default);
                if (index >= 0)
                    methodReturnTypeHandle = GenericTypeHandle(index);
            }
            return new MethodModel
            {
                Name = fromProperty == null ? name : null!,
                OutputName = fromProperty == null ? (outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null) : null,
                DeclaringType = fromProperty == null ? TypeHandle(method.ContainingType) : 0,
                Flags = method.GetSymbolFlags(),
                ReturnType = fromProperty == null ? methodReturnTypeHandle : 0,
                Parameters = fromProperty == null ? NullIfEmpty(method.Parameters.Select(FromParameterSymbol).ToArray()) : null,
                GenericArguments = fromProperty == null ? NullIfEmpty(method.TypeArguments.Select(t => !global.ShouldExportType(t, null) ? "object" : t.CreateFullTypeName(global, withGlobalNamespace: false)).ToArray()) : null,
                Handle = MemberHandle(method),
                Attributes = NullIfEmpty(method.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a)).ToArray())
            };
        }

        ConstructorModel FromConstructorSymbol(IMethodSymbol ctor)
        {
            var metadata = global.GetRequiredMetadata(ctor);
            var name = ctor.Name;
            var outputName = metadata.OverloadName ?? name;
            return new ConstructorModel
            {
                Name = name,
                OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
                DeclaringType = TypeHandle(ctor.ContainingType),
                Flags = ctor.GetSymbolFlags(),
                Parameters = NullIfEmpty(ctor.Parameters.Select(FromParameterSymbol).ToArray()),
                Handle = MemberHandle(ctor),
                Attributes = NullIfEmpty(ctor.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a)).ToArray())
            };
        }

        FieldModel FromFieldSymbol(IFieldSymbol field)
        {
            var metadata = global.GetRequiredMetadata(field);
            var name = field.Name;
            var outputName = metadata.OverloadName ?? name;
            var fieldTypeHandle = !global.ShouldExportType(field.Type, null) ? default : TypeHandle(field.Type);
            if (fieldTypeHandle == 0 && field.ContainingType.Arity > 0)
            {
                var args = field.ContainingType.TypeArguments;
                var index = args.IndexOf(field.Type, 0, SymbolEqualityComparer.Default);
                if (index >= 0)
                    fieldTypeHandle = GenericTypeHandle(index);
            }
            return new FieldModel
            {
                Name = name,
                OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
                DeclaringType = !global.ShouldExportType(field.Type, null) ? default : TypeHandle(field.ContainingType),
                Flags = field.GetSymbolFlags(),
                FieldType = fieldTypeHandle,
                Handle = MemberHandle(field),
                Attributes = NullIfEmpty(field.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a)).ToArray())
            };
        }

        EventModel FromEventSymbol(IEventSymbol ev)
        {
            var metadata = global.GetRequiredMetadata(ev);
            var name = ev.Name;
            var outputName = metadata.OverloadName ?? name;
            var eventTypeHandle = !global.ShouldExportType(ev.Type, null) ? default : TypeHandle(ev.Type);
            if (eventTypeHandle == 0 && ev.ContainingType.Arity > 0)
            {
                var args = ev.ContainingType.TypeArguments;
                var index = args.IndexOf(ev.Type, 0, SymbolEqualityComparer.Default);
                if (index >= 0)
                    eventTypeHandle = GenericTypeHandle(index);
            }
            return new EventModel
            {
                Name = name,
                OutputName = outputName != name ? (outputName.StartsWith(name) ? outputName.Replace(name, "@") : outputName) : null,
                DeclaringType = TypeHandle(ev.ContainingType),
                Flags = ev.GetSymbolFlags(),
                EventHandlerType = !global.ShouldExportType(ev.Type, null) ? default : TypeHandle(ev.Type),
                AddMethod = ev.AddMethod != null ? FromMethodSymbol(ev.AddMethod) : null,
                RemoveMethod = ev.RemoveMethod != null ? FromMethodSymbol(ev.RemoveMethod) : null,
                RaiseMethod = ev.RaiseMethod != null ? FromMethodSymbol(ev.RaiseMethod) : null,
                Handle = MemberHandle(ev),
                Attributes = NullIfEmpty(ev.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a)).ToArray())
            };
        }

        ParameterModel FromParameterSymbol(IParameterSymbol param)
        {
            var paramTypeHandle = !global.ShouldExportType(param.Type, null) ? default : TypeHandle(param.Type);
            if (paramTypeHandle == 0 && param.ContainingType.Arity > 0)
            {
                var args = param.ContainingType.TypeArguments;
                var index = args.IndexOf(param.Type, 0, SymbolEqualityComparer.Default);
                if (index >= 0)
                    paramTypeHandle = GenericTypeHandle(index);
            }
            return new ParameterModel
            {
                Name = param.Name,
                ParameterType = paramTypeHandle,
                Position = param.Ordinal,
                Flags =
                (param.IsOptional ? ParameterFlagsModel.Optional : ParameterFlagsModel.None) |
                (param.RefKind == RefKind.Out ? ParameterFlagsModel.Out : ParameterFlagsModel.None) |
                (param.RefKind == RefKind.Ref ? ParameterFlagsModel.Ref : ParameterFlagsModel.None) |
                (param.IsParams ? ParameterFlagsModel.Params : ParameterFlagsModel.None),
                DefaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue ?? "__typeDefault__" : null,
                Attributes = NullIfEmpty(param.GetAttributes()
                .Where(a => a.AttributeClass != null && global.ShouldExportType(a.AttributeClass, null))
                .Select(a => FromAttribute(a)).ToArray())
            };
        }

        object? AdaptAttrValue(object? a)
        {
            //if (a is not string && a is not byte && a is not short && a is not int && a is not long && a is not bool && a is not ITypeSymbol && a is not TypedConstant && a is not IEnumerable<ITypeSymbol> && a is not IEnumerable<TypedConstant>)
            //{

            //}
            if (a == null)
                return null;
            if (a is ITypeSymbol t)
                return TypeHandle(t);
            if (a is TypedConstant tc)
                return TypeHandle(tc.Type!);
            if (a is IEnumerable<ITypeSymbol> tt)
                return tt.Select(t => TypeHandle(t));
            if (a is IEnumerable<TypedConstant> tcc)
                return tcc.Select(t => TypeHandle(t.Type!));
            return a;
        }

        AttributeModel FromAttribute(AttributeData att)
        {
            return new AttributeModel
            {
                TypeHandle = att.AttributeClass == null ? default : TypeHandle(att.AttributeClass),
                ConstructorHandle = att.AttributeConstructor == null ? default : MemberHandle(att.AttributeConstructor),
                ConstructorArguments = NullIfEmpty(att.ConstructorArguments.Select(arg => new AttributeConstructorArgumentModel
                {
                    Type = arg.Type != null ? TypeHandle(arg.Type) : default,
                    Value = AdaptAttrValue(arg.Kind == TypedConstantKind.Array ? arg.Values : arg.Value),
                }).ToArray()),
                NamedArguments = NullIfEmpty(att.NamedArguments.Select(arg => new AttributeNamedArgumentModel
                {
                    Name = arg.Key,
                    Type = arg.Value.Type != null ? TypeHandle(arg.Value.Type) : default,
                    Value = AdaptAttrValue(arg.Value.Kind == TypedConstantKind.Array ? arg.Value.Values : arg.Value.Value),
                }).ToArray())
            };
        }

    }
}
