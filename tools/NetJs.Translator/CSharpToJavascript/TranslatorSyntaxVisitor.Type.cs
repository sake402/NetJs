using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public HashSet<INamedTypeSymbol> Dependencies { get; private set; } = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        public Stack<BaseTypeDeclarationSyntax> CurrentTypes { get; } = new Stack<BaseTypeDeclarationSyntax>();
        public Stack<INamedTypeSymbol> CurrentTypeSymbols { get; } = new Stack<INamedTypeSymbol>();

        BaseTypeDeclarationSyntax CurrentType => CurrentTypes.Peek();
        public INamedTypeSymbol CurrentTypeSymbol => CurrentTypeSymbols.Peek();

        public override void VisitPredefinedType(PredefinedTypeSyntax node)
        {
            EnsureImported(node);
            var symbol = _global.GetTypeSymbol(node, this);
            var meta = _global.GetRequiredMetadata(symbol);
            var name = meta.OverloadName ?? Utilities.ResolvePredefinedTypeName(node);
            CurrentTypeWriter.Write(node, name);
            base.VisitPredefinedType(node);
        }

        public override void VisitNullableType(NullableTypeSyntax node)
        {
            var nullable = _global.GetSymbol("System.Nullable<>", this/*, out _, out _*/);
            var metadata = _global.GetRequiredMetadata(nullable);
            EnsureImported(node.ElementType);
            //Writer.Write(node, $"{metadata.InvocationName}(");
            Visit(node.ElementType);
            //Writer.Write(node, ")");
            //base.VisitNullableType(node);
        }

        public override void VisitFunctionPointerType(FunctionPointerTypeSyntax node)
        {
            CurrentTypeWriter.Write(node, _global.SystemObject.ComputeOutputTypeName(_global));
            //base.VisitFunctionPointerType(node);
        }

        static bool IsInnerTypeDeclaration(MemberDeclarationSyntax member)
        {
            return member is BaseTypeDeclarationSyntax && member is not ExtensionBlockDeclarationSyntax;
        }

        List<IMethodSymbol> discriminatedInterfaceMethodImplemented = new List<IMethodSymbol>();
        void WriteInterfaceImplementations(CSharpSyntaxNode node, ITypeSymbol type)
        {
            IEnumerable<INamedTypeSymbol> interfaces = type.AllInterfaces;
            if (type.BaseType != null)
            {
                interfaces = interfaces.Except(type.BaseType.AllInterfaces);
            }
            foreach (var it in interfaces)
            {
                foreach (var interfaceMember in it.GetMembers())
                {
                    var implementation = type.FindImplementationForInterfaceMember(interfaceMember);
                    if (implementation != null)
                    {
                        if (implementation.ContainingType.TypeKind == TypeKind.Interface)
                        {
                            continue;
                        }
                        if (implementation.Kind == SymbolKind.Property) //implemented via its method get set component
                        {
                            continue;
                        }
                        if (implementation.Kind == SymbolKind.Method && ((IMethodSymbol)implementation).ExplicitInterfaceImplementations.Any())
                        {
                            continue;
                        }
                        if (implementation.Kind == SymbolKind.Property && ((IPropertySymbol)implementation).ExplicitInterfaceImplementations.Any())
                        {
                            continue;
                        }
                        if (!implementation.IsExtern && !_global.HasAttribute(implementation, typeof(ExternalAttribute).FullName, this, false, out _) && !_global.HasAttribute(implementation.ContainingSymbol, typeof(ExternalAttribute).FullName, this, false, out _))
                        {
                            //var methodMember = ((IMethodSymbol)interfaceMember);
                            //var methodImplementation = implementation;
                            var interfaceMemberMetadata = _global.GetRequiredMetadata(interfaceMember);
                            ISymbol? memberAssociatedProperty = null;
                            bool isIndexer = false;
                            if (interfaceMember.Kind == SymbolKind.Method && (memberAssociatedProperty = ((IMethodSymbol)interfaceMember).AssociatedSymbol) != null)
                            {
                                if (memberAssociatedProperty.Kind == SymbolKind.Property && !((IPropertySymbol)memberAssociatedProperty).IsIndexer)
                                    interfaceMemberMetadata = _global.GetRequiredMetadata(memberAssociatedProperty);
                                else
                                    isIndexer = true;
                            }
                            var implementationMetadata = _global.GetRequiredMetadata(implementation);
                            ISymbol? implementationAssociatedProperty = null;
                            if (implementation.Kind == SymbolKind.Method && (implementationAssociatedProperty = ((IMethodSymbol)implementation).AssociatedSymbol) != null)
                            {
                                if (implementationAssociatedProperty.Kind == SymbolKind.Property && !((IPropertySymbol)implementationAssociatedProperty).IsIndexer)
                                    implementationMetadata = _global.GetRequiredMetadata(implementationAssociatedProperty);
                                else
                                    isIndexer = true;
                            }
                            OpenClosure(node);
                            CurrentTypeWriter.WriteLine(node, $"//Generated interface implemetation for {interfaceMember}", true);
                            CurrentTypeWriter.Write(node, $"{(implementation.IsStatic || implementation.IsStaticCallConvention(_global) ? "static " : "")}{(implementation.IsStaticCallConvention(_global) ? "/*conventional*/ " : "")}{(!isIndexer && implementation is IMethodSymbol ms && ms.MethodKind == MethodKind.PropertyGet ? "get " : !isIndexer && implementation is IMethodSymbol ms2 && ms2.MethodKind == MethodKind.PropertySet ? "set " : "")}{interfaceMemberMetadata.OverloadName}", true);

                            CurrentTypeWriter.Write(node, $"(", false);
                            //No need to write method argument explicitly since we use sptread operator on arguments
                            //WriteMethodDeclarationParameters(node, parameters?.Parameters ?? default);
                            //for property set though, we need to
                            bool isPropertySet = false;
                            if (!isIndexer && implementation is IMethodSymbol ms22 && ms22.MethodKind == MethodKind.PropertySet)
                            {
                                isPropertySet = true;
                                CurrentTypeWriter.Write(node, $"value", false);
                            }
                            CurrentTypeWriter.WriteLine(node, $")", false);

                            CurrentTypeWriter.WriteLine(node, $"{{", true);

                            ////For implemented interface members call that may conflict in name:
                            ////eg if a class implement both IComparer<string> and IComparer<char>
                            ////the compare implementation methods for both implementation will conflictly be named System$Collections$Generic$IComparer$$$Compare
                            ////even though one will receive a string and the other a char
                            ////We need to discriminate the one we intend to call by checking the type Ts passed as a last argument to this
                            //var conflictingInterfaces = member.ContainingType.AllInterfaces.GroupBy(e => e.OriginalDefinition, SymbolEqualityComparer.Default).Where(a => a.Count() > 1);
                            //if (conflictingInterfaces.Any())
                            //{
                            //    var conflictingMethods = conflictingInterfaces.SelectMany(g => g.SelectMany(i => i.GetMembers().Where(m => m.OriginalDefinition.Equals(implementation.OriginalDefinition, SymbolEqualityComparer.Default))))
                            //        .Cast<IMethodSymbol>();
                            //    var conflctingTs = conflictingMethods.Select(m => (m.ContainingType, m.ContainingType.TypeArguments)).ToList();
                            //    if (implementation.ContainingType.TypeKind == TypeKind.Interface && implementation.ContainingType.TypeArguments.Any() && implementation.ContainingType.TypeArguments.All(a => a is INamedTypeSymbol))
                            //    {
                            //        CurrentTypeWriter.WriteLine(node, $"let $ts = arguments[arguments.length-1];", true);
                            //        foreach (var method in conflictingMethods.Except([implementation]))
                            //        {
                            //            discriminatedInterfaceMethodImplemented.Add(member);
                            //            var ifs = string.Join(" && ", method.ContainingType.TypeArguments.Select((t, i) => $"$ts[{i}] === {t.ComputeOutputTypeName(_global)}"));
                            //            CurrentTypeWriter.WriteLine(node, $"//Merged and discriminated with method implemetation for {method}", true);
                            //            CurrentTypeWriter.WriteLine(node, $"if ({ifs})", true);
                            //            var implementation = methodSymbol.ContainingType.FindImplementationForInterfaceMember(method)!;
                            //            var implementationMetadata = _global.GetRequiredMetadata(implementation);
                            //            if (implementation.IsStaticCallConvention(_global))
                            //            {
                            //                CurrentTypeWriter.WriteLine(node, $"    return {implementationMetadata.InvocationName}.apply(this, arguments);", true);
                            //            }
                            //            else
                            //            {
                            //                CurrentTypeWriter.WriteLine(node, $"    return {(!methodSymbol.IsStatic ? "this." : "")}{implementationMetadata.InvocationName}(...arguments);", true);
                            //            }
                            //        }
                            //    }
                            //}
                            if (implementation.IsStaticCallConvention(_global))
                            {
                                CurrentTypeWriter.WriteLine(node, $"{(interfaceMember.Kind == SymbolKind.Method && interfaceMember is IMethodSymbol method && method.ReturnType != null && method.ReturnType.Name != "void" ? "return " : "")}{implementationMetadata.InvocationName}{((implementation is IMethodSymbol ms3 && ms3.MethodKind != MethodKind.PropertyGet && ms3.MethodKind != MethodKind.PropertySet) || isIndexer ? ".apply(this, arguments)" : "")};", true);
                            }
                            else
                            {
                                CurrentTypeWriter.WriteLine(node, $"{(!isPropertySet && interfaceMember.Kind == SymbolKind.Method && interfaceMember is IMethodSymbol method && method.ReturnType != null && method.ReturnType.Name != "void" ? "return " : "")}{(!interfaceMember.IsStatic ? "this." : "")}{implementationMetadata.InvocationName}{((implementation is IMethodSymbol ms4 && ms4.MethodKind != MethodKind.PropertyGet && ms4.MethodKind != MethodKind.PropertySet) || isIndexer ? "(...arguments)" : "")}{(isPropertySet ? " = value" : "")};", true);
                            }
                            CurrentTypeWriter.WriteLine(node, $"}}", true);
                            if (implementation.IsStaticCallConvention(_global) && !interfaceMember.IsStatic)
                            {
                                CurrentTypeWriter.WriteLine(node, $"//Static convention instance redirect", true);
                                CurrentTypeWriter.Write(node, $"{interfaceMemberMetadata.OverloadName}", true);
                                CurrentTypeWriter.Write(node, $"(", false);
                                //No need to write method argument explicitly since we use spread operator on arguments
                                //WriteMethodDeclarationParameters(node, parameters?.Parameters ?? default);
                                CurrentTypeWriter.WriteLine(node, $")", false);
                                CurrentTypeWriter.WriteLine(node, $"{{", true);
                                CurrentTypeWriter.WriteLine(node, $"{(interfaceMember.Kind == SymbolKind.Method && interfaceMember is IMethodSymbol method && method.ReceiverType != null && method.ReturnType.Name != "void" ? "return " : "")}{implementationMetadata.InvocationName}{((implementation is IMethodSymbol ms5 && ms5.MethodKind == MethodKind.Ordinary) || isIndexer ? ".apply(this, arguments)" : "")};", true);
                                //CurrentTypeWriter.Write(node, $"return {symbol.InvocationName}.apply(this, ...arguments);", true);
                                CurrentTypeWriter.WriteLine(node, $"}}", true);
                            }
                            CloseClosure(node);
                        }

                    }
                }
            }
        }

        void WriteTypeMetadata(CSharpSyntaxNode node, INamedTypeSymbol typeSymbol)
        {
            bool isDefinedTypeParameter = _global.IsDefinedTypeParameter(typeSymbol);
            string GetFullName(MemberDeclarationSyntax type)
            {
                string? parent = null;
                if (type.Parent is BaseTypeDeclarationSyntax ts)
                {
                    parent = GetFullName(ts) + ".";
                }
                else if (type.Parent is NamespaceDeclarationSyntax ns)
                {
                    parent = _global.ResolveFullNamespace(ns) + ".";
                }
                var ret = parent +
                    (type is BaseTypeDeclarationSyntax bt ?
                    bt.Identifier.ValueText.Trim().TrimEnd('?') :
                    type is DelegateDeclarationSyntax dt ? dt.Identifier.ValueText :
                    throw new InvalidOperationException());
                string? nameGArgs = null;
                var nameArg =
                    type is TypeDeclarationSyntax btt ? btt.TypeParameterList?.Parameters.Select(p => $"${{{p.Identifier.ValueText}?.{Constants.PrototypeFullName}}}") :
                    type is DelegateDeclarationSyntax dtt ? dtt.TypeParameterList?.Parameters.Select(p => $"${{{p.Identifier.ValueText}?.{Constants.PrototypeFullName}}}") :
                    null;
                if (nameArg != null)
                {
                    nameGArgs = $"<{string.Join(",", nameArg)}>";
                }
                return ret + nameGArgs;
            }
            //string GetFullName(BaseTypeDeclarationSyntax node)
            //{
            //    string? nameGArgs = null;
            //    var nameArg = (node as TypeDeclarationSyntax)?.TypeParameterList?.Parameters.Select(p => $"({p.Identifier.ValueText}?.{Constants.PrototypeFullName}??\"\")");
            //    if (nameArg != null)
            //    {
            //        nameGArgs = " + \"<\" + " + string.Join(" + \",\" + ", nameArg) + " + \">\"";
            //    }
            //    string name = "";
            //    if (node.Parent is BaseTypeDeclarationSyntax par)
            //    {
            //        name = GetFullName(par);
            //    }
            //    name += (name.Length > 0 ? "+ " : "") + "\"" + _global.ResolveFullTypeName(node) + "\"";
            //    return $"{name}{nameGArgs}";
            //}

            //lets generic a runtime type handle for a generic type, so we can change it at runtime when a concrete type is made from the generic type
            CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeTypeHandle} = {_global.Reflection.NumericTypeHandle(typeSymbol)};", true);
            //if (typeSymbol.BaseType != null)
            //CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeBaseTypeHandle} = {_global.Reflection.NumericTypeHandle(typeSymbol.BaseType)};", true);
            if (typeSymbol.Arity > 0)
            {
                CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeGenericArgumentCount} = {typeSymbol.Arity};", true);
            }
            if (typeSymbol.IsValueType)
            {
                var sz = _global.SizeOf(typeSymbol);
                if (sz != null)
                {
                    CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeStructSize} = {sz};", true);
                }
            }
            var knownType = _global.KnownTypeFrom(typeSymbol);
            if (knownType != KnownTypeHandle.SystemDynamic)
            {
                CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeKnownType} = {(int)knownType};", true);
            }
            CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeTypeFlags} = 0b{(int)_global.GetTypeFlags(typeSymbol):B};", true);
            CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeKind} = {(int)_global.MapTypeKind(typeSymbol.TypeKind)};", true);
            if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                CurrentTypeWriter.WriteLine(node, $"static get {Constants.EnumUnderlyingName}() {{ return {typeSymbol.EnumUnderlyingType!.ComputeOutputTypeName(_global)}; }}", true);
            }
            CurrentTypeWriter.WriteLine(node, $"static ${Constants.PrototypeMetadata};", true);
            if (isDefinedTypeParameter || _global.IsReflectable(typeSymbol, null))
                CurrentTypeWriter.WriteLine(node, $"static get {Constants.PrototypeMetadata}() {{ return this.${Constants.PrototypeMetadata} ??= {_global.Reflection.FromTypeSymbolAsJson(typeSymbol, this)}; }}", true);
            else
                CurrentTypeWriter.WriteLine(node, $"static get {Constants.PrototypeMetadata}() {{ return this.${Constants.PrototypeMetadata} ??= {_global.Reflection.FromTypeSymbolAsJson(typeSymbol, this, minimal: true)}; }}", true);

            if (typeSymbol.BaseType != null && _global.ShouldExportType(typeSymbol.BaseType, this))
            {
                CurrentTypeWriter.WriteLine(node, $"static get {Constants.PrototypeBaseType}() {{ return {typeSymbol.BaseType.ComputeOutputTypeName(_global)}; }}", true);
            }
            //if (useInterfaceMixin == InterfaceMixinMode.None)
            //{
            var interfaces = typeSymbol.AllInterfaces;
            var _interfaces = interfaces.Concat(typeSymbol.IsAwaitable() ? [_global.AwaitableInterface] : [])
                .Where(i => _global.ShouldExportType(i, this));
            var mInterfaces = _interfaces
                .Select(e =>
                {
                    var ret = e.ComputeOutputTypeName(_global);
                    return ret;
                });
            if (mInterfaces.Any())
            {
                CurrentTypeWriter.WriteLine(node, $"static ${Constants.PrototypeInterfaces};", true);
                CurrentTypeWriter.WriteLine(node, $"static get {Constants.PrototypeInterfaces}() {{ return this.${Constants.PrototypeInterfaces} ??= [ {string.Join(", ", mInterfaces)} ]; }}", true);
            }
            //}
            //else if (isBootClass)
            //{
            //    CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeMetadata} = {JsonSerializer.Serialize(new TypeModel { Flags = typeSymbol.GetTypeFlags() }, ReflectionMetadataBuilder.SerializationOption)};", true);
            //    //CurrentTypeWriter.WriteLine(node, "static $bf()", true);
            //    //CurrentTypeWriter.WriteLine(node, "{", true);
            //    //CurrentTypeWriter.WriteLine(node, $"return {(int)typeSymbol.GetTypeFlags()};", true);
            //    //CurrentTypeWriter.WriteLine(node, "}", true);
            //}
            //else
            //{
            //    //We must write a metadata, else we will be using the base prototype metadata for this class. We write it as undefined
            //    CurrentTypeWriter.WriteLine(node, $"static {Constants.PrototypeMetadata};", true);
            //}

            if (isDefinedTypeParameter)
            {
                CurrentTypeWriter.WriteLine(node, $"static get {Constants.PrototypeFullName}() {{ return \"\"; }}", true);
            }
            else
            {
                CurrentTypeWriter.WriteLine(node, $"static get {Constants.PrototypeFullName}() {{ return `{(node is MemberDeclarationSyntax mnode ? GetFullName(mnode) : typeSymbol.Name)}`; }}", true);
            }
            //if (!_static && !typeSymbol.GetMembers("Is", _global).Any())
            //{
            //    Writer.WriteLine(node, "static $is(value)", true);
            //    Writer.WriteLine(node, "{", true);
            //    Writer.WriteLine(node, $"if (value instanceof {typeMetadata.InvocationName})", true);
            //    Writer.WriteLine(node, "    return true;", true);
            //    if (hasMixin)
            //    {
            //        Writer.WriteLine(node, $"if (Mixin.$is(value))", true);
            //        Writer.WriteLine(node, "    return true;", true);
            //    }
            //    Writer.WriteLine(node, "return false;", true);
            //    Writer.WriteLine(node, "}", true);
            //}

            //if (!_static && !node.IsKind(SyntaxKind.EnumDeclaration) && !typeSymbol.GetMembers("Default", _global).Any())
            //{
            //    var defaultValue = _global.GetDefaultValue(typeSymbol, true);
            //    if (defaultValue != null && defaultValue != "null")
            //    {
            //        CurrentTypeWriter.WriteLine(node, $"static {Constants.DefaultTypeName}() {{ return {defaultValue}; }}", true);
            //    }
            //}

            //if (!isInnerClass)
            //{
            //    Writer.WriteLine(node, $"}});", true);
            //}

            //if (isBootClass) //run the static initializer and constructor immmediately as the runtime wont run it for boot class
            //{
            //    if (CurrentClosure.TypeInitializers.Any(e => e.Static))
            //        Writer.WriteLine(node, $"{_global.GlobalName}.{fullClassName}.{Constants.StaticInitializerName}();", true);
            //    if (members.Where(e => e.IsKind(SyntaxKind.ConstructorDeclaration)/* is ConstructorDeclarationSyntax*/).Cast<ConstructorDeclarationSyntax>()
            //        .Where(c => c.Modifiers.IsStatic()).Any())
            //    {
            //        Writer.WriteLine(node, $"{_global.GlobalName}.{fullClassName}.{Constants.StaticConstructorName}();", true);
            //    }
            //}

        }
        public void WriteTypeDeclaration(MemberDeclarationSyntax node, IEnumerable<ParameterSyntax>? primaryConstructorParameters, IEnumerable<MemberDeclarationSyntax>? members, Action? writePrologue = null, Action? writeEpilogue = null)
        {
            var previousClosure = CurrentClosure;

            var typeSymbol = (INamedTypeSymbol)OpenClosure(node);
            bool export = _global.ShouldExportType(typeSymbol, this);
            var nestedClassAsNestedStaticObject = Constants.NestedClassAsNestedStaticObject;
            if (export)
            {
                //must nest an inner class of generic type, so it has access to the generic arguments
                //if (!nestedClassAsNestedStaticObject)
                //{
                //    if (node is TypeDeclarationSyntax ttype)
                //    {
                //        if (ttype.Arity > 0)
                //        {
                //            nestedClassAsNestedStaticObject = true;
                //        }
                //    }
                //}
                var typeMetadata = _global.GetRequiredMetadata(typeSymbol);// SyntaxSymbols.GetValueOrDefault(node)?.First() ?? default;
                                                                           //we already processed a partial member
                lock (_global)
                {
                    if (_global.ProcessedTypeNodes.Contains(typeSymbol, SymbolEqualityComparer.Default))
                    {
                        CloseClosure(node);
                        return;
                    }
                    if (!nestedClassAsNestedStaticObject || node.Parent is not BaseTypeDeclarationSyntax)
                    {
                        CurrentTypeWriter = new ScriptWriter();
                        TypeWriters.Add(typeSymbol, CurrentTypeWriter);
                        _global.TypeVisitors.Add(typeSymbol, this);
                        _global.TypeWriters.Add(typeSymbol, CurrentTypeWriter);
                    }
                    else
                    {
                        var mclassName = node is BaseTypeDeclarationSyntax btd ? btd.Identifier.ValueText :
                            node is DelegateDeclarationSyntax dt ? dt.Identifier.ValueText :
                            throw new InvalidOperationException();
                        if (typeSymbol.Arity > 0)
                        {
                            mclassName += "<" + string.Join(",", Enumerable.Range(1, typeSymbol.Arity)) + ">";
                        }
                        previousClosure.DefineIdentifierType(/*classMetadata.LocalName ?? */mclassName, CodeSymbol.From(typeSymbol));
                    }

                    //add all member symbol to this scope
                    members ??= node.ChildNodes().OfType<MemberDeclarationSyntax>();
                    if (node.Modifiers.IsPartial())
                    {
                        var type = node.GetType();
                        //var partialTypes = global.TypeNodes.GetValueOrDefault(fullTypeName);
                        if (typeMetadata.DeclaringReferences.Count() > 1)
                        {
                            foreach (var partial in typeMetadata.DeclaringReferences)
                            {
                                var sm = _global.Compilation.GetSemanticModel(partial.SyntaxTree);
                                if (!_semanticModels.Contains(sm))
                                    _semanticModels.Add(sm);
                            }
                            var usingNamespaces = typeMetadata.DeclaringReferences.SelectMany(c => c.SyntaxTree.GetRoot().ChildNodes().OfType<UsingDirectiveSyntax>());
                            foreach (var us in usingNamespaces)
                            {
                                Visit(us);
                            }
                            members = typeMetadata.DeclaringReferences.Select(e => e.GetSyntax()).SelectMany(c => c.ChildNodes().OfType<MemberDeclarationSyntax>()).ToList();
                            _global.ProcessedTypeNodes.Add(typeSymbol);
                        }
                    }
                }
                bool _static = typeSymbol.IsStatic;// node.Modifiers.Any(m => m.ValueText == "static");// || node.IsKind(SyntaxKind.EnumDeclaration)/* is EnumDeclarationSyntax*/;
                bool isBootClass = _global.IsBootClass(typeSymbol);
                bool isDefinedTypeParameter = _global.IsDefinedTypeParameter(typeSymbol);
                string? _base = null;
                string? mixImplementedInterfaces = null;
                string[]? implementedInterfaces = null;
                var useInterfaceMixin = Constants.UseInterfaceMixin;
                if (useInterfaceMixin == InterfaceMixinMode.ProxyLookup)
                {
                    var minterfaces = typeSymbol.Interfaces;
                    //string InterfaceOutputName(ITypeSymbol _interface)
                    //{
                    //    string? Ts = null;
                    //    if (_interface is INamedTypeSymbol nt && nt.Arity > 0)
                    //    {
                    //        Ts = string.Join(", ", nt.TypeArguments.Select(t =>
                    //        {
                    //            return InterfaceOutputName(t);
                    //        }));
                    //    }
                    //    if (Ts != null)
                    //    {
                    //        Ts = "(" + Ts + ")";
                    //    }
                    //    return $"{_interface.ComputeOutputTypeName(_global)}{Ts}";
                    //}
                    implementedInterfaces = minterfaces.Concat(typeSymbol.IsAwaitable() ? [_global.AwaitableInterface] : [])
                        .Where(i => _global.ShouldExportType(i, this))
                        .Select(e =>
                    {
                        if (isBootClass && e.IsGenericType)
                        {
                            //Handle $.$spc.System.IEquatable$$($self) in boot class where $self isnt defined
                            return null;
                        }
                        var ret = e.ComputeOutputTypeName(_global);
                        return ret;
                    }).Where(t => t != null).ToArray()!;
                    //If a class interface has a reference to the class, rename the self reference with $self
                    for (int i = 0; i < implementedInterfaces.Length; i++)
                    {
                        implementedInterfaces[i] = implementedInterfaces[i]
                            .Replace("(" + typeMetadata.InvocationName + ")", "($self)")
                            .Replace("(" + typeMetadata.InvocationName + ",", "($self,")
                            .Replace(", " + typeMetadata.InvocationName + ", ", ", $self, ")
                            .Replace(", " + typeMetadata.InvocationName + ")", ", $self)")
                            .Replace("(" + typeMetadata.InvocationName + ".", "($self.$itype$")
                            .Replace(", " + typeMetadata.InvocationName + ".", ", $self.$itype$");
                    }
                }
                else if (useInterfaceMixin == InterfaceMixinMode.MixinMethod)
                {
                    var minterfaces = typeSymbol.Interfaces;
                    foreach (var _interface in minterfaces.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
                    {
                        Dependencies.Add(_interface);
                        if (!_global.ShouldExportType(_interface!, this))
                            continue;
                        if (mixImplementedInterfaces == null)
                            mixImplementedInterfaces = $"{{0}}";
                        EnsureImported((ITypeSymbol)_interface);
                        var interfaceName = _global.GetRequiredMetadata(_interface).OverloadName;
                        string? Ts = null;
                        if (_interface.Arity > 0)
                        {
                            Ts = string.Join(", ", _interface.TypeArguments.Select(t =>
                            {
                                return t.ComputeOutputTypeName(_global);
                            }));
                        }
                        mixImplementedInterfaces = $"{interfaceName}({Ts}{(Ts != null ? ", " : "")}{mixImplementedInterfaces})";
                    }
                }
                if (typeSymbol.Arity > 0)
                {
                    foreach (var a in typeSymbol.TypeParameters)
                    {
                        CurrentClosure.DefineIdentifierType(a.Name, CodeSymbol.From(a));
                    }
                }
                string? classDefinition = null;
                string? closingClassDeclaration = null;
                string? closingClassDeclaration2 = null;
                var fullClassName = (typeSymbol.Arity == 0 ? typeMetadata.OverloadName : null) ?? typeMetadata.UniqueFullName.RemoveGenericParameterNames(out _) ??
                    (node is BaseTypeDeclarationSyntax bt ? _global.ResolveTypeName(bt) : throw new InvalidOperationException());
                if (_global.OutputMode.HasFlag(OutputMode.Global) && fullClassName.StartsWith(_global.GlobalName + "."))
                {
                    fullClassName = fullClassName.Substring(_global.GlobalName.Length + 1);
                }
                var classNameSegments = fullClassName.Split('.');
                var className = classNameSegments[classNameSegments.Length - 1];
                var classNamespace = string.Join(".", classNameSegments.Take(classNameSegments.Length - 1));
                //bool isInnerClass = false;
                //bool hasMixin = false;
                if (!_static)
                {
                    var systemObject = _global.GetSymbol("System.Object", this/*, out _, out _*/);
                    var systemValueType = _global.GetSymbol("System.ValueType", this/*, out _, out _*/);
                    if (typeSymbol.BaseType != null)
                    {
                        //if (isBootClass &&
                        //    (typeSymbol.BaseType.Equals(systemObject, SymbolEqualityComparer.Default) || typeSymbol.BaseType.Equals(systemValueType, SymbolEqualityComparer.Default)))
                        //{

                        //}
                        //else
                        //{
                        Dependencies.Add(typeSymbol.BaseType);
                        var baseMeta = _global.GetRequiredMetadata(typeSymbol.BaseType);
                        var baseName = typeSymbol.BaseType.ComputeOutputTypeName(_global);
                        if (!string.IsNullOrEmpty(baseName))
                        {
                            //If a class base has a reference to the class, rename the self reference with $self
                            baseName = baseName
                            .Replace("(" + typeMetadata.InvocationName + ")", "($self)")
                            .Replace("(" + typeMetadata.InvocationName + ",", "($self,")
                            .Replace(", " + typeMetadata.InvocationName + ", ", ", $self, ")
                            .Replace(", " + typeMetadata.InvocationName + ")", ", $self)")
                            .Replace("(" + typeMetadata.InvocationName + ".", "($self.$itype$")
                            .Replace(", " + typeMetadata.InvocationName + ".", ", $self.$itype$");
                            //if (typeSymbol.BaseType.Arity > 0)
                            //{
                            //    string Ts = string.Join(", ", typeSymbol.BaseType.TypeArguments.Select(t =>
                            //    {
                            //        return t.ComputeOutputTypeName(_global);
                            //    }));
                            //    baseName += "(" + Ts + ")";
                            //}
                            EnsureImported(typeSymbol.BaseType);
                            if (useInterfaceMixin == InterfaceMixinMode.MixinMethod && mixImplementedInterfaces != null)
                            {
                                _base = $" extends {string.Format(mixImplementedInterfaces, baseName)}";
                            }
                            else if (useInterfaceMixin == InterfaceMixinMode.ProxyLookup && implementedInterfaces?.Length > 0)
                            {
                                _base = $" extends $.$mix({baseName}, {string.Join(", ", implementedInterfaces)})";
                            }
                            else if (typeSymbol.BaseType.Arity > 0)
                            {
                                //string genericTypes = string.Join(", ", symbol.BaseType.TypeArguments.Select(t => t.ComputeOutputTypeName(global)));
                                //_base = $" extends {baseName}({genericTypes})";
                                _base = $" extends {baseName}";
                            }
                            else
                            {
                                _base = $" extends {baseName}";
                            }
                        }
                        //}
                    }
                    else if (!isBootClass && node.IsKind(SyntaxKind.ClassDeclaration) || node.IsKind(SyntaxKind.InterfaceDeclaration))
                    {
                        if (mixImplementedInterfaces != null)
                        {
                            _base = $" extends {string.Format(mixImplementedInterfaces, node.IsKind(SyntaxKind.InterfaceDeclaration) ? "Mixin" : _global.OutputMode.HasFlag(OutputMode.Global) ? _global.GlobalName + ".System_Object" : "Object")}";
                        }
                        else if (implementedInterfaces?.Length > 1)
                        {
                            _base = $" extends {_global.GlobalName}.$mix({string.Join(", ", implementedInterfaces)})";
                        }
                        else if (implementedInterfaces?.Length > 0)
                        {
                            _base = $" extends {implementedInterfaces.Single()}";
                        }
                        else if (className != "System.Object" && className != "Object")
                        {
                            var systemObjectMetadata = _global.GetRequiredMetadata(systemObject);
                            if (!node.IsKind(SyntaxKind.InterfaceDeclaration))
                            {
                                if (!typeSymbol.Equals(systemObject, SymbolEqualityComparer.Default))
                                    _base = $" extends " + systemObjectMetadata.InvocationName;
                            }
                            else if (useInterfaceMixin == InterfaceMixinMode.MixinMethod)
                            {
                                _base = $" extends (Mixin??{_global.GlobalName}.$nomix)";
                            }
                        }
                    }
                    else if (!isBootClass && node.IsKind(SyntaxKind.EnumDeclaration))
                    {
                        var _enum = _global.GetSymbol("System.Enum", this);
                        var enumMetadata = _global.GetRequiredMetadata(_enum);
                        _base = $" extends " + enumMetadata.InvocationName;
                    }
                }
                var typeParameters = (node as TypeDeclarationSyntax)?.TypeParameterList?.Parameters ?? (node as DelegateDeclarationSyntax)?.TypeParameterList?.Parameters;
                string genericArgs = string.Join(", ", typeParameters?.Select(t => $"{(t.VarianceKeyword.ValueText?.Length > 0 ? $"/*{t.VarianceKeyword.ValueText}*/ " : "")}{t.Identifier.ValueText}") ?? Enumerable.Empty<string>());
                bool isInterface = /*useInterfaceMixin &&*/ node.IsKind(SyntaxKind.InterfaceDeclaration);
                bool usingInterfaceMixin = isInterface && useInterfaceMixin == InterfaceMixinMode.MixinMethod;
                bool hasGenericArguments = genericArgs?.Length > 0;
                bool hasMixinOrGeneric = isInterface || hasGenericArguments;
                bool isNested = nestedClassAsNestedStaticObject && node.Parent is TypeDeclarationSyntax;
                var classCreate = typeSymbol.IsValueType ? (isNested ? Constants.AssemblyNestedStructName : Constants.AssemblyStructName) :
                    (isNested ? Constants.AssemblyNestedClassName : Constants.AssemblyDefineClassName);
                if (isBootClass)
                {
                    if (hasGenericArguments)
                    {
                        Console.WriteLine($"WARNING: A generic boot class {typeSymbol.Name} currently may not work as expected! It currently creates a new copy of the class everytime it is accessed even with the same generic type.");
                    }
                    //for (int i = 1; i < classNameSegments.Length; i++)
                    //{
                    //    var path = string.Join(".", classNameSegments.Take(i));
                    //    Writer.WriteLine(node, $"$.{path} ??= {{ }};", true);
                    //}
                    classDefinition = $"$.{Constants.AssemblyBootClassName}(\"{fullClassName}\", {(typeSymbol.Arity > 0 ? $"({genericArgs}{(hasGenericArguments && isInterface ? ", " : "")}{(isInterface ? "Mixin" : "")}) => " : "")}class {(Constants.ExportClassName ? className.Replace("<", "$").Replace(",", "$").Replace(">", "$") : "")}{_base}";
                    closingClassDeclaration = ");";
                }
                else if (hasGenericArguments)
                {
                    classDefinition = $"$asm.{classCreate}(\"{fullClassName}\", ({genericArgs}) => $asm.$gt(\"{fullClassName}\", [{genericArgs}], ($self) => class {(Constants.ExportClassName ? className.Replace("<", "$").Replace(",", "$").Replace(">", "$") : "")}{_base}";
                    closingClassDeclaration = "));";
                }
                else if (!hasGenericArguments)
                {
                    classDefinition = $"$asm.{classCreate}(\"{fullClassName}\", ($self) => class {(Constants.ExportClassName ? className.Replace("<", "$").Replace(",", "$").Replace(">", "$") : "")}{_base}";
                    closingClassDeclaration = ");";
                }
                else
                {
                    classDefinition = $"$asm.{classCreate}(\"{fullClassName}\", ({(!hasGenericArguments ? "$self" : "")}{(!hasGenericArguments && !string.IsNullOrEmpty(genericArgs) ? ", " : "")}{genericArgs}) => {(hasGenericArguments ? $"$asm.$gt(\"{fullClassName}\", [{genericArgs}], ($self) => " : "")}class {(Constants.ExportClassName ? className.Replace("<", "$").Replace(",", "$").Replace(">", "$") : "")}{_base}";
                    //classDefinition = $"$asm.{classCreate}(\"{fullClassName}\", ({genericArgs}{(hasGeneric && usingInterfaceMixin ? ", " : "")}{(usingInterfaceMixin ? "Mixin" : "")}) => {(hasGeneric || isInterface ? $"$asm.${(isInterface && hasGeneric ? "gm" : isInterface && !hasGeneric ? "mx" : "gt")}(\"{fullClassName}\", [{genericArgs}{(hasGeneric && usingInterfaceMixin ? ", " : "")}{(usingInterfaceMixin ? "Mixin" : "")}], () => " : "")}class {"" ?? className}{_base}";
                    //if (genericArgs?.Length > 0 || isInterface)
                    if (hasGenericArguments)
                    {
                        closingClassDeclaration = "));";
                    }
                    else
                    {
                        closingClassDeclaration = ");";
                    }
                }
                string openingClassDefinition = "{";
                if (isNested)
                {
                    var nestedClassName = className.Replace("<", "$").Replace(",", "$").Replace(">", "$");
                    var parent = _global.GetSymbol(node.Parent!, this/*, out _, out _*/);
                    var parentMetadata = _global.GetRequiredMetadata(parent);
                    //isInnerClass = true;
                    CurrentTypeWriter.WriteLine(node, $"static $_{nestedClassName};", true);
                    CurrentTypeWriter.WriteLine(node, $"static {(!hasGenericArguments ? "get " : "")}{nestedClassName}({genericArgs})", true);
                    CurrentTypeWriter.WriteLine(node, $"{{", true);
                    CurrentTypeWriter.WriteLine(node, $"let $cls = {parentMetadata.InvocationName};", true);
                    classDefinition = $"return {(hasGenericArguments ? "(" : "")}$cls.$_{nestedClassName} ??= {classDefinition}";
                    if (hasGenericArguments)
                    {
                        if (isBootClass)
                            closingClassDeclaration = $", $cls, ($v) => $cls.$_{nestedClassName} = $v))({genericArgs});";
                        else
                            closingClassDeclaration = $"), $cls, ($v) => $cls.$_{nestedClassName} = $v))({genericArgs});";
                    }
                    else
                    {
                        closingClassDeclaration = $", $cls, ($v) => $cls.$_{nestedClassName} = $v);";
                    }
                    //closingClassDeclaration = ");";
                    closingClassDeclaration2 = "}";
                }

                //if (node.Parent is TypeDeclarationSyntax)
                //{
                //    //inner class definition
                //    isInnerClass = true;
                //    Writer.WriteLine(node, $"static $_{className};", true);
                //    if ((node is TypeDeclarationSyntax t && t.Arity > 0))
                //    {
                //        genericArgs = string.Join(", ", (node as TypeDeclarationSyntax)?.TypeParameterList?.Parameters.Select(t => $"{(t.VarianceKeyword.ValueText?.Length > 0 ? $"/*{t.VarianceKeyword.ValueText}*/ " : "")}{t.Identifier.ValueText}") ?? Enumerable.Empty<string>());
                //        Writer.WriteLine(node, $"static get {className}()", true);
                //        Writer.WriteLine(node, $"{{", true);
                //        classDefinition = $"return $_{className} ??= ({genericArgs}) => {_global.GlobalName}.Assembly.GetGeneric($asm, [{genericArgs}], class {className}{_base}";
                //        closingClassDeclaration = ");";
                //        closingClassDeclaration2 = "}";
                //    }
                //    else
                //    {
                //        Writer.WriteLine(node, $"static get {className}()", true);
                //        Writer.WriteLine(node, $"{{", true);
                //        classDefinition = $"return $_{className} ??= {_global.GlobalName}.Assembly.Define($asm, class {className}{_base}";
                //        closingClassDeclaration = ");";
                //        closingClassDeclaration2 = "}";
                //    }
                //}
                //else if (node.IsKind(SyntaxKind.InterfaceDeclaration)/* is InterfaceDeclarationSyntax*/ || (node is TypeDeclarationSyntax t && t.Arity > 0))
                //{
                //    genericArgs = string.Join(", ", (node as TypeDeclarationSyntax)?.TypeParameterList?.Parameters.Select(t => $"{(t.VarianceKeyword.ValueText?.Length > 0 ? $"/*{t.VarianceKeyword.ValueText}*/ " : "")}{t.Identifier.ValueText}") ?? Enumerable.Empty<string>());
                //    if (node.IsKind(SyntaxKind.InterfaceDeclaration)/* is InterfaceDeclarationSyntax*/)
                //    {
                //        classDefinition = $"{(node.Parent is TypeDeclarationSyntax ? "static" : _global.OutputMode.HasFlag(OutputMode.Global) ? "" : "export const ")}$asm.$CLS(\"{fullClassName}\", ({genericArgs}{(genericArgs?.Length > 0 ? ", " : "")}Mixin) => {_global.GlobalName}.Assembly.Mixin($asm, [{"" ?? genericArgs}{(false && genericArgs?.Length > 0 ? ", " : "")}Mixin], class {className} extends Mixin";
                //        closingClassDeclaration = "));";
                //        hasMixin = true;
                //    }
                //    else
                //    {
                //        classDefinition = $"{(node.Parent is TypeDeclarationSyntax ? "static" : _global.OutputMode.HasFlag(OutputMode.Global) ? "" : "export const ")}$asm.$CLS(\"{fullClassName}\", ({genericArgs}) => {_global.GlobalName}.Assembly.GetGeneric($asm, [{genericArgs}], class {className}{_base}";
                //        closingClassDeclaration = "));";
                //    }
                //}
                //else
                //{
                //    classDefinition = $"{(_global.OutputMode.HasFlag(OutputMode.Global) ? "" : "export const ")}$asm.$CLS(\"{fullClassName}\", class {className}{_base}";
                //    closingClassDeclaration = ")";
                //}
                //if (!isInnerClass)
                //{
                //    Writer.WriteLine(node, $"$asm.$NS(\"{string.Join(".", classNameSegments.Take(classNameSegments.Length - 1))}\", function($ns)", true);
                //    Writer.WriteLine(node, $"{{", true);
                //}
                CurrentTypeWriter.WriteLine(node, classDefinition, true);
                CurrentTypeWriter.WriteLine(node, openingClassDefinition, true);

                CurrentTypeSymbols.Push(typeSymbol);
                if (node is BaseTypeDeclarationSyntax btd2)
                    CurrentTypes.Push(btd2);

                //define every member of this class in the class scope so they are available for type checking
                var membersSymbol = typeSymbol.GetMembers(null, _global)
                    .Where(m =>
                    {
                        if (m is IMethodSymbol mm && mm.IsPartialDefinition)
                        {
                            return false;
                        }
                        else if (m is IPropertySymbol pt && pt.IsPartialDefinition)
                        {
                            return false;
                        }
                        return true;
                    })
                //    .Where(m =>
                //{
                //    if (m is IMethodSymbol mm && mm.ExplicitInterfaceImplementations.Any())
                //    {
                //        return false;
                //    }
                //    else if (m is IPropertySymbol pt && pt.ExplicitInterfaceImplementations.Any())
                //    {
                //        return false;
                //    }
                //    return true;
                //})
                    .GroupBy(m =>
                {
                    //var metadata = _global.GetRequiredMetadata(m);
                    //return metadata.OriginalOverloadName;
                    var name = m.Name;
                    //if (!name.Contains(".") && m is IMethodSymbol mm && mm.ExplicitInterfaceImplementations.Any())
                    //{
                    //    var e = mm.ExplicitInterfaceImplementations.First();
                    //}
                    //else if (!name.Contains(".") && m is IPropertySymbol pt && pt.ExplicitInterfaceImplementations.Any())
                    //{
                    //    var e = pt.ExplicitInterfaceImplementations.First();
                    //}
                    if (m is IMethodSymbol mt && mt.Arity > 0)
                    {
                        name = name + "<" + string.Join(",", Enumerable.Range(1, mt.Arity).Select(c => "")) + ">";
                    }
                    if (m is INamedTypeSymbol tt && tt.Arity > 0)
                    {
                        name = name + "<" + string.Join(",", Enumerable.Range(1, tt.Arity).Select(c => "")) + ">";
                    }
                    return name;
                }).ToList();

                writePrologue?.Invoke();

                foreach (var m in membersSymbol)
                {
                    if (m.Count() == 1)
                        CurrentClosure.DefineIdentifierType(m.Key, CodeSymbol.From(m.Single()));
                    else
                    {
                        CurrentClosure.DefineIdentifierType(m.Key, CodeSymbol.From(new MemberSymbolOverload()
                        {
                            Overloads = m.ToList()!
                        }), throwOnDuplicate: false);
                    }
                }

                VisitChildren(members.Where(e =>
                {
                    if (!nestedClassAsNestedStaticObject)
                    {
                        if (IsInnerTypeDeclaration(e))
                        {
                            return false;
                        }
                    }
                    if (node.IsKind(SyntaxKind.InterfaceDeclaration)/* is InterfaceDeclarationSyntax*/)
                    {
                        if (e is PropertyDeclarationSyntax property)
                            return property.ExpressionBody != null || (property.AccessorList?.Accessors.Any() ?? false);
                        else if (e is MethodDeclarationSyntax method)
                            return method.ExpressionBody != null || method.Body != null;
                    }
                    return !e.IsKind(SyntaxKind.BaseList)/* is not BaseListSyntax*/ && !e.IsKind(SyntaxKind.TypeParameterConstraintClause)/* is not TypeParameterConstraintClauseSyntax*/;
                })/*.OrderBy(e =>
                {
                    //make sure we visit types first, then fields, then properties, constructor and finally methods, so we can have a reference to their TypeSyntax in method body
                    //if (e is BaseTypeDeclarationSyntax)
                        //return int.MaxValue - 3;
                    //if (e is IndexerDeclarationSyntax)
                    //    return int.MaxValue - 2;
                    //if (e is PropertyDeclarationSyntax)
                    //    return int.MaxValue - 2;
                    //if (e is ConstructorDeclarationSyntax)
                    //    return int.MaxValue - 1;
                    //if (e is MethodDeclarationSyntax)
                    //    return int.MaxValue;
                    return 0;
                })*/);

                writeEpilogue?.Invoke();

                WriteInterfaceImplementations(node, typeSymbol);

                if (!_static && !isDefinedTypeParameter)
                {
                    var constructors = members.Where(e => e.IsKind(SyntaxKind.ConstructorDeclaration)/* is ConstructorDeclarationSyntax*/).Cast<ConstructorDeclarationSyntax>()
                        .Where(c => !c.Modifiers.IsStatic());
                    if (node.IsKind(SyntaxKind.StructDeclaration))
                    {
                        //As struct must have a default constructor
                        if (!constructors.Any(e => e.ParameterList.Parameters.Count == 0)) //no default constructor defined, define it
                        {
                            var defaultCtor = typeSymbol.GetMembers(".ctor").Single(e => e is IMethodSymbol ms && ms.Parameters.Length == 0);
                            var ctorMetadata = _global.GetRequiredMetadata(defaultCtor);
                            CurrentTypeWriter.WriteLine(node, $"//default value type constructor", true);
                            CurrentTypeWriter.WriteLine(node, $"{ctorMetadata.OverloadName ?? Constants.DefaultConstructorName}()", true);
                            CurrentTypeWriter.WriteLine(node, "{", true);
                            CurrentTypeWriter.WriteLine(node, "return this;", true);
                            CurrentTypeWriter.WriteLine(node, "}", true);
                        }
                    }
                    else if (node.IsKind(SyntaxKind.ClassDeclaration))
                    {
                        //A class without any constructor has a a default one generated by the compiler
                        if (primaryConstructorParameters == null && !constructors.Any()) //no constructor defined, define default
                        {
                            var defaultCtor = typeSymbol.GetMembers(".ctor").Single(e => e is IMethodSymbol ms && ms.Parameters.Length == 0);
                            var ctorMetadata = _global.GetRequiredMetadata(defaultCtor);
                            CurrentTypeWriter.WriteLine(node, $"//default reference type constructor overload", true);
                            CurrentTypeWriter.WriteLine(node, $"{(constructorCallConvention == ConstructorCallConvention.StaticCall ? "static " : "")}{ctorMetadata.OverloadName ?? Constants.DefaultConstructorName}()", true);
                            CurrentTypeWriter.WriteLine(node, "{", true);
                            var baseMembers = typeSymbol.BaseType?.GetMembers(".ctor", _global, true);
                            //if there is a base constructor with all parameter having default, prioritize that
                            var defaulBaseConstructor = (IMethodSymbol?)baseMembers.FirstOrDefault(e => e is IMethodSymbol ms && ms.Parameters.Length > 0 && ms.Parameters.All(e => e.HasExplicitDefaultValue)) ??
                                (IMethodSymbol?)typeSymbol.BaseType?.GetMembers(".ctor", _global, true)
                                .SingleOrDefault(e => e is IMethodSymbol ms && ms.Parameters.Length == 0);
                            if (defaulBaseConstructor != null)
                            {
                                WriteCalledConstructor(node, typeSymbol, defaulBaseConstructor, null);
                            }
                            CurrentTypeWriter.WriteLine(node, "return this;", true);
                            CurrentTypeWriter.WriteLine(node, "}", true);
                        }
                    }
                }

                //Generate clone systems for all struct types, if not existing
                if ((typeSymbol.IsValueType || typeSymbol.IsRecord) &&
                    typeSymbol.EnumUnderlyingType == null/*dont generate for enum type*/ &&
                    !typeSymbol.IsJsPrimitive() &&
                    !typeSymbol.IsStatic &&
                    !typeSymbol.GetMembers().Any(a => a is IMethodSymbol m && m.Name == "Clone"))
                {
                    CurrentTypeWriter.Write(node, Constants.Clone, true);
                    CurrentTypeWriter.WriteLine(node, "($copy)");
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    CurrentTypeWriter.WriteLine(node, "let copy = $copy ?? new this.constructor();", true);
                    if (typeSymbol.BaseType?.IsRecord ?? false)
                    {
                        CurrentTypeWriter.Write(node, "super.", true);
                        CurrentTypeWriter.Write(node, Constants.Clone);
                        CurrentTypeWriter.WriteLine(node, "(copy);");
                    }
                    int arraySize = 0;
                    ITypeSymbol nfieldType;
                    bool misInlineArray = false;
                    if (misInlineArray = _global.IsInlineArray(typeSymbol, out arraySize, out _))
                    {
                        if (misInlineArray)
                            CurrentTypeWriter.WriteLine(node, $"//InlineArray({arraySize})", true);
                        CurrentTypeWriter.WriteLine(node, $"copy.{Constants.StructFieldsLayoutName} = [...this.{Constants.StructFieldsLayoutName}];", true);
                        CurrentTypeWriter.Write(node, "", true);
                        WriteMethodInvocation(node, "System.Array.CopyMetadata", arguments: [
                            new CodeNode(()=>
                            {
                                CurrentTypeWriter.Write(node, $"copy.{Constants.StructFieldsLayoutName}");
                            }),
                            new CodeNode(()=>
                            {
                                CurrentTypeWriter.Write(node, $"this.{Constants.StructFieldsLayoutName}");
                            })
                        ]);
                        CurrentTypeWriter.WriteLine(node, "");
                    }
                    else
                    {
                        //Writer.WriteLine(node, $"{classMetadata.InvocationName}.$ctor.call(copy);", true); //call default constructor generated
                        foreach (var member in typeSymbol.GetMembers())
                        {
                            if (!member.IsStatic && (member is IFieldSymbol || (member is IPropertySymbol p && p.IsAutoProperty())))
                            {
                                if (!member.Name.Contains("<") && !member.Name.Contains("[")) //skip those compiler generated members and indexer
                                {
                                    if (member is IFieldSymbol ff && _global.IsInlineArray(ff.Type, out arraySize, out nfieldType))
                                    {
                                        CurrentTypeWriter.WriteLine(node, $"//InlineArray({arraySize})", true);
                                        CurrentTypeWriter.WriteLine(node, $"copy.{Constants.StructFieldsLayoutName} = [...this.{Constants.StructFieldsLayoutName}];", true);
                                        CurrentTypeWriter.Write(node, "", true);
                                        WriteMethodInvocation(node, "System.Array.CopyMetadata", arguments: [
                                            new CodeNode(()=>
                                        {
                                            CurrentTypeWriter.Write(node, $"copy.{Constants.StructFieldsLayoutName}");
                                        }),
                                        new CodeNode(()=>
                                        {
                                            CurrentTypeWriter.Write(node, $"this.{Constants.StructFieldsLayoutName}");
                                        })
                                        ]);
                                        CurrentTypeWriter.WriteLine(node, "");
                                    }
                                    else if (_global.IsFixedSizeField(member, out arraySize, out nfieldType))
                                    {
                                        CurrentTypeWriter.WriteLine(node, $"//InlineArray({arraySize})", true);
                                        var memberName = _global.GetMetadata(member)?.OverloadName ?? member.Name;
                                        CurrentTypeWriter.WriteLine(node, $"copy.{memberName} = [...this.{memberName}];", true);
                                        CurrentTypeWriter.Write(node, "", true);
                                        WriteMethodInvocation(node, "System.Array.CopyMetadata", arguments: [
                                            new CodeNode(()=>
                                        {
                                            CurrentTypeWriter.Write(node, $"copy.{memberName}");
                                        }),
                                        new CodeNode(()=>
                                        {
                                            CurrentTypeWriter.Write(node, $"this.{memberName}");
                                        })
                                        ]);
                                        CurrentTypeWriter.WriteLine(node, "");
                                    }
                                    else
                                    {
                                        if (member is IFieldSymbol f && f.Type.IsValueType && !f.Type.IsJsPrimitive())
                                        {
                                            CurrentTypeWriter.WriteLine(node, $"copy.{member.Name} = this.{member.Name}.Clone();", true);
                                        }
                                        else if (member is IPropertySymbol pr && pr.Type.IsValueType && !pr.Type.IsJsPrimitive())
                                        {
                                            CurrentTypeWriter.WriteLine(node, $"copy.{member.Name} = this.{member.Name}.Clone();", true);
                                        }
                                        else
                                            CurrentTypeWriter.WriteLine(node, $"copy.{member.Name} = this.{member.Name};", true);
                                    }
                                }
                            }
                        }
                    }
                    CurrentTypeWriter.WriteLine(node, "return copy;", true);
                    CurrentTypeWriter.WriteLine(node, "}", true);
                }

                if (primaryConstructorParameters?.Any() ?? false)
                {
                    WritePrimaryConstructor((BaseTypeDeclarationSyntax)node, typeSymbol, primaryConstructorParameters);
                }

                bool hasDestuctor = members.Any(a => a.IsKind(SyntaxKind.DestructorDeclaration));
                //int minlineArraySize;
                //ITypeSymbol ifieldType;
                //bool isInlineArray = _global.IsInlineArray(typeSymbol, out minlineArraySize, out ifieldType);
                //bool isFixedArray = _global.IsFixedSizeField(typeSymbol, out minlineArraySize, out ifieldType);
                if (hasDestuctor /*|| isInlineArray*/ || CurrentClosure.TypeInitializers.Any(e => e.Location == TypeInitializerLocation.DefaultInstanceConstructor))
                {
                    CurrentTypeWriter.WriteLine(node, "//default member initializer", true);
                    CurrentTypeWriter.WriteLine(node, "constructor()", true);
                    CurrentTypeWriter.WriteLine(node, $"{{", true);
                    var baseIsBootClass = typeSymbol.BaseType != null ? _global.HasAttribute(typeSymbol.BaseType, typeof(BootAttribute).FullName, this, false, out _) : false;
                    if ((!isBootClass && typeSymbol.BaseType != null) || baseIsBootClass)
                        CurrentTypeWriter.WriteLine(node, $"super();", true);
                    //if (isInlineArray)
                    //{
                    //    var defaultValue = _global.GetDefaultValue(ifieldType, true);
                    //    CurrentTypeWriter.WriteLine(node, $"//InlineArray({ifieldType}, {minlineArraySize})", true);
                    //    CurrentTypeWriter.WriteLine(node, $"for (let $i = {minlineArraySize - 1}; $i >= 0; $i--)", true);
                    //    CurrentTypeWriter.WriteLine(node, "{", true);
                    //    CurrentTypeWriter.WriteLine(node, $"this.SetField($i, 1, {defaultValue});", true);
                    //    CurrentTypeWriter.WriteLine(node, "}", true);
                    //    CurrentTypeWriter.Write(node, "", true);
                    //    WriteMethodInvocation(node, "System.Array.AddMetadata", arguments: [
                    //        new CodeNode(()=>
                    //        {
                    //            CurrentTypeWriter.Write(node, $"this.{Constants.StructFieldsLayoutName}");
                    //        }),
                    //        new CodeNode(()=>
                    //        {
                    //            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.TypeOf}({ifieldType.ComputeOutputTypeName(_global)})");
                    //        })
                    //    ]);
                    //    CurrentTypeWriter.WriteLine(node, "");
                    //}
                    foreach (var init in CurrentClosure.TypeInitializers.Where((e => e.Location == TypeInitializerLocation.DefaultInstanceConstructor)))
                    {
                        init.Write();
                    }
                    if (hasDestuctor)
                    {
                        CurrentTypeWriter.WriteLine(node, $"$.{Constants.FinalizerRegister}(this);", true);
                    }
                    CurrentTypeWriter.WriteLine(node, $"}}", true);
                }

                if (CurrentClosure.TypeInitializers.Any(e => e.Location == TypeInitializerLocation.DefaultStaticConstructor))
                {
                    CurrentTypeWriter.WriteLine(node, "//Static Initializer", true);
                    //We cant use javascript default static initializer because we may need to call into other classes not intitialized yet
                    //Writer.WriteLine(node, $"static", true);
                    CurrentTypeWriter.WriteLine(node, $"static {Constants.StaticInitializerName}()", true);
                    CurrentTypeWriter.WriteLine(node, $"{{", true);
                    foreach (var init in CurrentClosure.TypeInitializers.Where(e => e.Location == TypeInitializerLocation.DefaultStaticConstructor))
                    {
                        //perform each initialization in its own closure to prevent local variable name clash
                        CurrentTypeWriter.WriteLine(node, $"{{", true);
                        OpenClosure(node);
                        init.Write();
                        CloseClosure(node);
                        CurrentTypeWriter.WriteLine(node, $"}}", true);
                    }
                    CurrentTypeWriter.WriteLine(node, $"}}", true);
                }
                var exports = typeSymbol.GetMembers().Where(m => _global.HasAttribute(m, "System.Runtime.InteropServices.JavaScript.JSExportAttribute", this, false, out _));
                if (exports.Any())
                {
                    CurrentTypeWriter.WriteLine(node, "//Method exports", true);
                    //We cant use javascript default static initializer because we may need to call into other classes not intitialized yet
                    //Writer.WriteLine(node, $"static", true);
                    CurrentTypeWriter.WriteLine(node, $"static {Constants.ExportMethodName}()", true);
                    CurrentTypeWriter.WriteLine(node, $"{{", true);
                    foreach (var e in exports)
                    {
                        if (e is IMethodSymbol ms && ms.IsStatic && ms.Arity == 0)
                        {
                            var typeName = typeSymbol.CreateSignature(_global, withGlobalNamespace: false, withAssemblySlugNamespace: false).Replace(".", "_");
                            var methodMetadata = _global.GetRequiredMetadata(ms);
                            var exportName = $"{typeName}_{methodMetadata.OverloadName}";
                            CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.$exports.{typeName}_{methodMetadata.OverloadName} = this.{methodMetadata.OverloadName};", true);
                        }
                    }
                    CurrentTypeWriter.WriteLine(node, $"}}", true);
                }
                if (typeSymbol.IsRecord)
                {
                    CurrentTypeWriter.WriteLine(node, $"//Begin generated record members", true);

                    CurrentTypeWriter.WriteLine(node, $"/*Type*/ get EqualityContract() {{ return $.{Constants.TypeOf}({typeSymbol.ComputeOutputTypeName(_global)}); }}", true);

                    CurrentTypeWriter.WriteLine(node, $"/*string*/ ToString()", true);
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    CurrentTypeWriter.Write(node, "let stringBuilder = ", true);
                    var builder = (INamedTypeSymbol)_global.GetSymbol("System.Text.StringBuilder", this);
                    var ctor = (IMethodSymbol)builder.GetMembers(".ctor").First(e => e is IMethodSymbol ms && ms.Parameters.Length == 0);
                    var appendString = (IMethodSymbol)builder.GetMembers("Append").First(e => e is IMethodSymbol ms && ms.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, _global.SystemString));
                    var toString = (IMethodSymbol)builder.GetMembers("ToString").First(e => e is IMethodSymbol ms && ms.Parameters.Length == 0);
                    WriteConstructorCall(node, builder, ctor);
                    CurrentTypeWriter.WriteLine(node, ";");
                    void StringBuilderAppend(string str)
                    {
                        CurrentTypeWriter.Write(node, "", true);
                        WriteMethodInvocation(node, appendString, null, [new CodeNode(() =>
                        {
                            CurrentTypeWriter.Write(node, $"\"{str}\"");
                        })], new CodeNode(() =>
                        {
                            CurrentTypeWriter.Write(node, "stringBuilder");
                        }), null);
                        CurrentTypeWriter.WriteLine(node, ";");
                    }
                    void StringBuilderAppendRHS(CodeNode str)
                    {
                        CurrentTypeWriter.Write(node, "", true);
                        WriteMethodInvocation(node, appendString, null, [new CodeNode(() =>
                        {
                            VisitNode(str);
                        })], new CodeNode(() =>
                        {
                            CurrentTypeWriter.Write(node, "stringBuilder");
                        }), null);
                        CurrentTypeWriter.WriteLine(node, ";");
                    }
                    StringBuilderAppend(typeSymbol.Name);
                    StringBuilderAppend("{");
                    CurrentTypeWriter.WriteLine(node, "if (this.PrintMembers(stringBuilder))", true);
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    StringBuilderAppend(" ");
                    CurrentTypeWriter.WriteLine(node, "}", true);
                    StringBuilderAppend("}");
                    CurrentTypeWriter.Write(node, "return ", true);
                    WriteMethodInvocation(node, toString, null, null, new CodeNode(() =>
                    {
                        CurrentTypeWriter.Write(node, "stringBuilder");
                    }), null);
                    CurrentTypeWriter.WriteLine(node, ";");
                    CurrentTypeWriter.WriteLine(node, "}", true);


                    CurrentTypeWriter.WriteLine(node, $"/*bool*/ PrintMembers(/*StringBuilder*/ stringBuilder)", true);
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    int ix = 0;
                    var properties = typeSymbol.GetMembers().Where(e => e.Kind == SymbolKind.Property && e.DeclaredAccessibility == Accessibility.Public &&
                        !_global.HasAttribute(e, typeof(CompilerGeneratedAttribute).FullName, this, false, out _) && e.Name != "EqualityContract").Cast<IPropertySymbol>()
                        .Where(e => !e.IsIndexer);
                    foreach (var property in properties)
                    {
                        if (ix > 0)
                            StringBuilderAppend($", ");
                        StringBuilderAppend($"{property.Name} = ");
                        if (SymbolEqualityComparer.Default.Equals(property.Type, _global.SystemString))
                        {
                            StringBuilderAppendRHS(new CodeNode(() =>
                            {
                                CurrentTypeWriter.Write(node, "this.");
                                CurrentTypeWriter.Write(node, property.Name);
                            }));
                        }
                        else
                        {
                            var mtoString = (IMethodSymbol?)(property.Type.GetMembers("ToString").FirstOrDefault(e => e is IMethodSymbol ms && ms.Parameters.Length == 0) ??
                                _global.SystemObject.GetMembers("ToString").FirstOrDefault(e => e is IMethodSymbol ms && ms.Parameters.Length == 0));
                            StringBuilderAppendRHS(new CodeNode(() =>
                            {
                                WriteMethodInvocation(node, mtoString, null, null, new CodeNode(() =>
                                {
                                    CurrentTypeWriter.Write(node, "this.");
                                    CurrentTypeWriter.Write(node, property.Name);
                                }), null);
                            }));
                        }
                        ix++;
                    }
                    CurrentTypeWriter.WriteLine(node, "return true;", true);
                    CurrentTypeWriter.WriteLine(node, "}", true);

                    //CurrentTypeWriter.WriteLine(node, $"/*bool*/ Equals(/*object*/ other)", true);
                    //CurrentTypeWriter.WriteLine(node, "{", true);
                    //var eqs = string.Join(" && ", properties.Select(p =>
                    //{
                    //    var eqComparer = ((INamedTypeSymbol)_global.GetSymbol("System.Collections.Generic.EqualityComparer<>", this)).Construct(p.Type);
                    //    var eqq = $"{eqComparer.ComputeOutputTypeName(_global)}.Default.Equals(this.{p.Name}, other.{p.Name})";
                    //    return eqq; ;
                    //}));
                    var equalsMethod = typeSymbol.GetMembers("Equals").Single(e => e is IMethodSymbol ms && SymbolEqualityComparer.Default.Equals(ms.ReturnType, _global.SystemBoolean) && ms.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, typeSymbol));
                    var equalsMetadata = _global.GetRequiredMetadata(equalsMethod);
                    CurrentTypeWriter.WriteLine(node, $"/*bool*/ {equalsMetadata.OverloadName}(/*{typeSymbol.Name}*/ other)", true);
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    //var eqs = string.Join(" && ", properties.Select(p =>
                    //{
                    //    var eqComparer = ((INamedTypeSymbol)_global.GetSymbol("System.Collections.Generic.EqualityComparer<>", this)).Construct(p.Type);
                    //    var equalsMethod = eqComparer
                    //    .GetMembers("Equals")
                    //    .Single(e => e is IMethodSymbol ms &&
                    //                ms.Parameters.Length == 2 &&
                    //                SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, p.Type) &&
                    //                SymbolEqualityComparer.Default.Equals(ms.Parameters[1].Type, p.Type));
                    //    var metadata = _global.GetRequiredMetadata(equalsMethod);
                    //    var eqq = $"{eqComparer.ComputeOutputTypeName(_global)}.Default.{(metadata.OverloadName ?? "Equals")}(this.{p.Name}, other.{p.Name})";
                    //    return eqq;
                    //}));
                    //CurrentTypeWriter.WriteLine(node, $"return this === other || (other !== null && this.EqualityContract == other.EqualityContract{(eqs.Length > 0 ? " && " : "")}{eqs});", true);
                    CurrentTypeWriter.Write(node, $"return this === other || (other !== null && this.EqualityContract == other.EqualityContract", true);
                    foreach (var pr in properties)
                    {
                        CurrentTypeWriter.Write(node, $" && ");
                        WriteCompareEquals(node, pr.Type, new CodeNode(() => CurrentTypeWriter.Write(node, $"this.{pr.Name}")), new CodeNode(() => CurrentTypeWriter.Write(node, $"other.{pr.Name}")));
                    }
                    CurrentTypeWriter.WriteLine(node, $");");
                    CurrentTypeWriter.WriteLine(node, "}", true);


                    CurrentTypeWriter.WriteLine(node, $"/*int*/ GetHashCode()", true);
                    CurrentTypeWriter.WriteLine(node, "{", true);

                    var eqComparer = ((INamedTypeSymbol)_global.GetSymbol("System.Collections.Generic.EqualityComparer<>", this)).Construct(_global.SystemType);
                    var getHashMethod = eqComparer
                    .GetMembers("GetHashCode")
                    .Single(e => e is IMethodSymbol ms &&
                                ms.Parameters.Length == 1 &&
                                SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, _global.SystemType));
                    var getHashCodeMetadata = _global.GetRequiredMetadata(getHashMethod);
                    var eqCHashCode = $"{eqComparer.ComputeOutputTypeName(_global)}.Default.{(getHashCodeMetadata.OverloadName ?? "GetHashCode")}(this.EqualityContract) * -1521134295";

                    var hashs = string.Join(" && ", properties.Select(p =>
                    {
                        var eqComparer = ((INamedTypeSymbol)_global.GetSymbol("System.Collections.Generic.EqualityComparer<>", this)).Construct(p.Type);
                        var getHashMethod = eqComparer
                        .GetMembers("GetHashCode")
                        .Single(e => e is IMethodSymbol ms &&
                                    ms.Parameters.Length == 1 &&
                                    SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, p.Type));
                        var metadata = _global.GetRequiredMetadata(getHashMethod);
                        var eqq = $"{eqComparer.ComputeOutputTypeName(_global)}.Default.{(metadata.OverloadName ?? "GetHashCode")}(this.{p.Name}) * -1521134295";
                        return eqq;
                    }));

                    CurrentTypeWriter.WriteLine(node, $"return {eqCHashCode}{(hashs.Length > 0 ? " && " : "")}{hashs};", true);
                    CurrentTypeWriter.WriteLine(node, "}", true);
                    CurrentTypeWriter.WriteLine(node, $"static /*bool*/ op_InEquality(/*{typeSymbol.Name}*/ left, /*{typeSymbol.Name}*/ right)", true);
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    CurrentTypeWriter.WriteLine(node, "return !(this.op_Equals(left, right));", true);
                    CurrentTypeWriter.WriteLine(node, "}", true);

                    CurrentTypeWriter.WriteLine(node, $"static /*bool*/ op_Equality(/*{typeSymbol.Name}*/ left, /*{typeSymbol.Name}*/ right)", true);
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    CurrentTypeWriter.WriteLine(node, $"return left === right || (left !== null && left.{(equalsMetadata.OverloadName ?? "Equals")}(right));", true);
                    CurrentTypeWriter.WriteLine(node, "}", true);


                    if (properties.Any())
                    {
                        CurrentTypeWriter.WriteLine(node, $"Deconstruct({string.Join(", ", properties.Select(p => $"/*out {p.Type}*/ ${p.Name}"))})", true);
                        CurrentTypeWriter.WriteLine(node, "{", true);
                        foreach (var p in properties)
                        {
                            CurrentTypeWriter.WriteLine(node, $"${p.Name}.{Constants.RefValueName} = this.{p.Name};", true);
                        }
                        CurrentTypeWriter.WriteLine(node, "}", true);
                    }

                    CurrentTypeWriter.WriteLine(node, $"//End generated record member3s", true);
                }

                //if (typeSymbol.IsAwaitable())
                //{
                //    CurrentTypeWriter.WriteLine(node, $"then(continuation, onRejected)", true);
                //    CurrentTypeWriter.WriteLine(node, $"{{", true);
                //    CurrentTypeWriter.WriteLine(node, $"let awaiter = this.GetAwaiter();", true);
                //    CurrentTypeWriter.WriteLine(node, $"awaiter.System$Runtime$CompilerServices$INotifyCompletion$OnCompleted(continuation);", true);
                //    CurrentTypeWriter.WriteLine(node, $"}}", true);
                //}

                WriteTypeMetadata(node, typeSymbol);

                CurrentTypeWriter.WriteLine(node, $"}}{(!closingClassDeclaration.StartsWith("}") ? closingClassDeclaration : "")}", true);
                if (closingClassDeclaration.StartsWith("}"))
                {
                    CurrentTypeWriter.WriteLine(node, closingClassDeclaration, true);
                }
                if (closingClassDeclaration2 != null)
                {
                    CurrentTypeWriter.WriteLine(node, closingClassDeclaration2, true);
                }

                CurrentTypeSymbols.Pop();
                if (node is BaseTypeDeclarationSyntax)
                    CurrentTypes.Pop();
            }
            CloseClosure(node);

            if (export && !nestedClassAsNestedStaticObject)
            {
                VisitChildren(members.Where(e =>
                {
                    if (IsInnerTypeDeclaration(e))
                    {
                        return true;
                    }
                    return false;
                }));
            }
        }

        public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            var field = (IFieldSymbol)_global.GetSymbol(node, this/*, out _, out _*/);
            var siblings = node.Parent!.ChildNodes().Where(e => e.IsKind(SyntaxKind.EnumMemberDeclaration)/* is EnumMemberDeclarationSyntax*/).ToArray();
            CurrentTypeWriter.Write(node, $"static {node.Identifier.ValueText}", true);
            if (field.ConstantValue != null)
            {
                CurrentTypeWriter.Write(node, $" = ", false);
                CurrentTypeWriter.Write(node, field.ConstantValue.ToString(), false);
            }
            else
            {
                Visit(node.EqualsValue);
                if (node.EqualsValue == null)
                {
                    //TODO estimate member value from last EqualsValue
                    CurrentTypeWriter.Write(node, $" = {(Array.IndexOf(siblings, node).ToString())}", false);
                }
            }
            if (((INamedTypeSymbol)field.Type).EnumUnderlyingType!.IsLongNumericType())
                CurrentTypeWriter.Write(node, $"n", false);
            CurrentTypeWriter.WriteLine(node, $";", false);
            //base.VisitEnumMemberDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            WriteTypeDeclaration(node, null, node.Members, writeEpilogue: () =>
            {
                CurrentTypeWriter.WriteLine(node, "static $map;", true);
                CurrentTypeWriter.WriteLine(node, "static get Map()", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                CurrentTypeWriter.WriteLine(node, "return this.$map ??= ", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                int ix = 0;
                int nextMemberValue = 0;
                foreach (var member in node.Members)
                {
                    if (ix > 0)
                        CurrentTypeWriter.WriteLine(node, ",");
                    CurrentTypeWriter.Write(node, "\"", true);
                    CurrentTypeWriter.Write(node, member.Identifier.ValueText);
                    CurrentTypeWriter.Write(node, "\": ");
                    var field = (IFieldSymbol)_global.GetSymbol(member, this/*, out _, out _*/);
                    if (field.ConstantValue != null)
                    {
                        CurrentTypeWriter.Write(node, field.ConstantValue.ToString(), false);
                        CurrentTypeWriter.Write(node, "n", false);
                    }
                    else
                    {
                        Visit(member.EqualsValue?.Value);
                        if (member.EqualsValue == null)
                        {
                            CurrentTypeWriter.Write(node, $"{nextMemberValue}n", false);
                            nextMemberValue++;
                        }
                        else
                        {
                            //TODO estimate lastmember value from EqualsValue
                        }
                    }
                    CurrentTypeWriter.Write(node, "");
                    ix++;
                }
                CurrentTypeWriter.WriteLine(node, "");
                CurrentTypeWriter.WriteLine(node, "};", true);
                CurrentTypeWriter.WriteLine(node, "}", true);
            });
            //base.VisitEnumDeclaration(node);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            WriteTypeDeclaration(node, node.ParameterList?.Parameters, node.Members);
            //base.VisitClassDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            WriteTypeDeclaration(node, node.ParameterList?.Parameters, node.Members);
            //base.VisitStructDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            WriteTypeDeclaration(node, node.ParameterList?.Parameters, node.Members);
            //base.VisitInterfaceDeclaration(node);
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            WriteTypeDeclaration(node, node.ParameterList?.Parameters, node.Members);
            //base.VisitRecordDeclaration(node);
        }

        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            WriteTypeDeclaration(node, null, [], writePrologue: () =>
            {
                //var baseClass = (INamedTypeSymbol)_global.GetTypeSymbol("System.MulticastDelegate", this);
                //var constructors = baseClass.GetMembers(".ctor").Cast<IMethodSymbol>();
                //var objectCtor = constructors.Single(c => c.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, _global.SystemObject));
                //var typeCtor = constructors.Single(c => c.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, _global.SystemType));
                //var objectCtorMetadata = _global.GetRequiredMetadata(objectCtor);
                //var typeCtorMetadata = _global.GetRequiredMetadata(typeCtor);

                //CurrentTypeWriter.WriteLine(node, $"let _nativeFunction;", true);
                //CurrentTypeWriter.WriteLine(node, $"let _this;", true);

                //CurrentTypeWriter.WriteLine(node, $"{objectCtorMetadata.OverloadName}(/*object*/ target, /*string*/ method)", true);
                //CurrentTypeWriter.WriteLine(node, "{", true);
                //CurrentTypeWriter.WriteLine(node, $"this.{objectCtorMetadata.OverloadName}(target, method);", true);
                //CurrentTypeWriter.WriteLine(node, "return this;", true);
                //CurrentTypeWriter.WriteLine(node, "}", true);

                //CurrentTypeWriter.WriteLine(node, $"{typeCtorMetadata.OverloadName}(/*Type*/ target, /*string*/ method)", true);
                //CurrentTypeWriter.WriteLine(node, "{", true);
                //CurrentTypeWriter.WriteLine(node, $"this.{typeCtorMetadata.OverloadName}(target, method);", true);
                //CurrentTypeWriter.WriteLine(node, "return this;", true);
                //CurrentTypeWriter.WriteLine(node, "}", true);

                CurrentTypeWriter.WriteLine(node, $"$ctor(/*object*/ target, fn)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                CurrentTypeWriter.WriteLine(node, $"this._target = target;", true);
                CurrentTypeWriter.WriteLine(node, $"this.{Constants.NativeDelagateFunction} = fn;", true);
                CurrentTypeWriter.WriteLine(node, "return this;", true);
                CurrentTypeWriter.WriteLine(node, "}", true);

                //var returnType = _global.GetTypeSymbol(node.ReturnType, null);
                //var returnTypeMetadata = _global.GetRequiredMetadata(returnType);

                CurrentTypeWriter.WriteLine(node, $"/*{node.ReturnType}*/ Invoke({(string.Join(", ", node.ParameterList.Parameters.Select(parameter =>
                {
                    var t = _global.TryGetSymbol(parameter.Type!, null);
                    return $"/*{(t != null ? _global.GetMetadata(t)?.InvocationName ?? t?.Name : t?.Name)}*/ ${parameter.Identifier.ValueText}";
                })))})", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                CurrentTypeWriter.WriteLine(node, $"return this.{Constants.NativeDelagateFunction}.apply(this._target, arguments);", true);
                CurrentTypeWriter.WriteLine(node, "}", true);

                //CurrentTypeWriter.WriteLine(node, $"BeginInvoke({(string.Join(", ", node.ParameterList.Parameters.Select(parameter =>
                //{
                //    var t = _global.TryGetTypeSymbol(parameter.Type!, null);
                //    return $"/*{(t != null ? _global.GetRequiredMetadata(t)?.InvocationName : null)}*/ {parameter.Identifier.ValueText}";
                //})))}, /*AsyncCallback*/callback, /*object*/ result)", true);
                //CurrentTypeWriter.WriteLine(node, "{", true);
                //CurrentTypeWriter.WriteLine(node, $"let $t = this._nativeFunction.apply(arguments);", true);
                //CurrentTypeWriter.WriteLine(node, "}", true);

                //CurrentTypeWriter.WriteLine(node, $"EndInvoke(/*IAsyncResult*/ result)", true);
                //CurrentTypeWriter.WriteLine(node, "{", true);
                //CurrentTypeWriter.WriteLine(node, "}", true);
            });
            //var typeSymbol = (INamedTypeSymbol)OpenClosure(node);
            //bool export = _global.ShouldExportType(typeSymbol, this);
            //if (export)
            //{
            //    var typeMetadata = _global.GetRequiredMetadata(typeSymbol);
            //    var fullClassName = /*typeMetadata.OverloadName ??*/ typeMetadata.FullName.RemoveGenericParameterNames(out _);// ?? _global.ResolveTypeName(node);
            //    if (_global.OutputMode.HasFlag(OutputMode.Global) && fullClassName.StartsWith(_global.GlobalName + "."))
            //    {
            //        fullClassName = fullClassName.Substring(_global.GlobalName.Length + 1);
            //    }
            //    var returnType = _global.GetTypeSymbol(node.ReturnType, null);
            //    var returnTypeMetadata = _global.GetRequiredMetadata(returnType);
            //    CurrentTypeWriter.WriteLine(node, $"$asm.$dlg(\"{fullClassName}\", {returnTypeMetadata.InvocationName}, [{(string.Join(", ", node.ParameterList.Parameters.Select(parameter => _global.GetRequiredMetadata(_global.GetTypeSymbol(parameter.Type!, null)).InvocationName)))}]);", true);
            //}
            //base.VisitDelegateDeclaration(node);
        }
    }
}
