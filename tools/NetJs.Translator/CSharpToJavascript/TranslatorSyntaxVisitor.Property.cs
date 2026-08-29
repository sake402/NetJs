using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        void WritePropertyGetAccessor(BasePropertyDeclarationSyntax node, string propertyName, AccessorDeclarationSyntax accessor, ISymbol propertySymbol)
        {
            var signature = propertySymbol.CreateSignature(_global, withGlobalNamespace: false);
            var matchingMember = _global.GetLinkerMemeberSubstitution(signature);
            if (matchingMember != null && matchingMember.Body == "stub")
            {
                WriteBlock(node, new CodeNode(() =>
                {
                    CurrentTypeWriter.WriteLine(node, $"return {matchingMember.Value}; //Linker substituted!", true);
                }));
                return;
            }

            OpenClosure(node);
            if (accessor.ExpressionBody != null)
            {
                WriteBlock(accessor.ExpressionBody, new CodeNode(() =>
                {
                    if (!accessor.ExpressionBody.Expression.IsKind(SyntaxKind.ThrowExpression)/* is not ThrowExpressionSyntax*/)
                        CurrentTypeWriter.Write(node, $"return ", true);
                    else
                        CurrentTypeWriter.Write(node, $"", true);
                    Visit(accessor.ExpressionBody.Expression);
                    CurrentTypeWriter.WriteLine(node, $";");
                }));
            }
            else if (accessor.Body != null)
            {
                TryWrapInYieldingGetEnumerable(node, (node.Type as GenericNameSyntax)?.TypeArgumentList.Arguments, [accessor.Body]);
                //VisitChildren(accessor.Body.Statements);
            }
            else
            {
                WriteBlock(node, new CodeNode(() =>
                {
                    var declaringMetadata = _global.GetRequiredMetadata(propertySymbol.ContainingType);
                    var propertyMetadata = _global.GetRequiredMetadata(propertySymbol);
                    CurrentTypeWriter.WriteLine(node, $"return {(propertySymbol.IsStatic ? "" : "this.")}{propertyMetadata.InvocationName ?? propertyName}$;", true);
                }));
            }
            CloseClosure(node);
        }

        //void TryWriteImplementedPropertyGetter(BasePropertyDeclarationSyntax node, IPropertySymbol? propertySymbol, string propertyName)
        //{
        //    if (node.ExplicitInterfaceSpecifier == null && propertySymbol != null && propertySymbol.ContainingType.Interfaces.Any())
        //    {
        //        if (!propertySymbol.IsExtern && !_global.HasAttribute(propertySymbol, typeof(ExternalAttribute).FullName, this, false, out _) && !_global.HasAttribute(propertySymbol.ContainingSymbol, typeof(ExternalAttribute).FullName, this, false, out _))
        //        {
        //            var declaringMetadata = _global.GetRequiredMetadata(propertySymbol.ContainingType);
        //            //find the interfaces that this property implements
        //            var implementedProperties = propertySymbol.ContainingType.AllInterfaces
        //                .SelectMany(i => i.GetMembers().OfType<IPropertySymbol>())
        //                .Where(im => propertySymbol.Equals(propertySymbol.ContainingType.FindImplementationForInterfaceMember(im), SymbolEqualityComparer.Default));
        //            foreach (var imp in implementedProperties)
        //            {
        //                if (!imp.IsExtern && !_global.HasAttribute(imp, typeof(ExternalAttribute).FullName, this, false, out _) && !_global.HasAttribute(imp.ContainingSymbol, typeof(ExternalAttribute).FullName, this, false, out _))
        //                {
        //                    //var interfaceMetadata = global.ReversedSymbols[imp];
        //                    if (imp.GetMethod != null)
        //                    {
        //                        var implementationSymbol = _global.GetRequiredMetadata(imp);
        //                        if (propertySymbol.IsIndexer)
        //                        {
        //                            implementationSymbol = _global.GetRequiredMetadata(imp.GetMethod);
        //                        }
        //                        CurrentTypeWriter.WriteLine(node, $"//Generated explicit interface get implemetation for {imp}", true);
        //                        CurrentTypeWriter.WriteLine(node, $"{(imp.GetMethod.IsStatic || propertySymbol.IsStaticCallConvention(_global) ? "static " : "")}{(propertySymbol.IsStaticCallConvention(_global) ? "/*conventional*/ " : "")}{(propertySymbol.IsIndexer ? "" : "get ")}{implementationSymbol.OverloadName}()", true);
        //                        CurrentTypeWriter.WriteLine(node, $"{{", true);
        //                        CurrentTypeWriter.WriteLine(node, $"return {(imp.GetMethod.IsStatic ? declaringMetadata.InvocationName : "this")}.{propertyName};", true);
        //                        CurrentTypeWriter.WriteLine(node, $"}}", true);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //void TryWriteImplementedPropertySetter(BasePropertyDeclarationSyntax node, IPropertySymbol? propertySymbol, string propertyName)
        //{
        //    if (node.ExplicitInterfaceSpecifier == null && propertySymbol != null && propertySymbol.ContainingType.Interfaces.Any())
        //    {
        //        if (!propertySymbol.IsExtern && !_global.HasAttribute(propertySymbol, typeof(ExternalAttribute).FullName, this, false, out _) && !_global.HasAttribute(propertySymbol.ContainingSymbol, typeof(ExternalAttribute).FullName, this, false, out _))
        //        {
        //            var declaringMetadata = _global.GetRequiredMetadata(propertySymbol.ContainingType);
        //            //find the interfaces that this property implements
        //            var implementedProperties = propertySymbol.ContainingType.AllInterfaces
        //                .SelectMany(i => i.GetMembers().OfType<IPropertySymbol>())
        //                .Where(im => propertySymbol.Equals(propertySymbol.ContainingType.FindImplementationForInterfaceMember(im), SymbolEqualityComparer.Default));
        //            foreach (var imp in implementedProperties)
        //            {
        //                if (!imp.IsExtern && !_global.HasAttribute(imp, typeof(ExternalAttribute).FullName, this, false, out _) && !_global.HasAttribute(imp.ContainingSymbol, typeof(ExternalAttribute).FullName, this, false, out _))
        //                {
        //                    if (imp.SetMethod != null)
        //                    {
        //                        var symbol = _global.GetRequiredMetadata(imp);
        //                        if (propertySymbol.IsIndexer)
        //                        {
        //                            symbol = _global.GetRequiredMetadata(imp.SetMethod);
        //                        }
        //                        CurrentTypeWriter.WriteLine(node, $"//Generated explicit interface set implemetation for {imp}", true);
        //                        CurrentTypeWriter.WriteLine(node, $"{(imp.SetMethod.IsStatic || propertySymbol.IsStaticCallConvention(_global) ? "static " : "")}{(propertySymbol.IsStaticCallConvention(_global) ? "/*conventional*/ " : "")}{(propertySymbol.IsIndexer ? "" : "set ")}{symbol.OverloadName}({(propertySymbol.IsIndexer ? "" : "value")})", true);
        //                        CurrentTypeWriter.WriteLine(node, $"{{", true);
        //                        CurrentTypeWriter.WriteLine(node, $"{(imp.SetMethod.IsStatic ? declaringMetadata.InvocationName : "this")}.{propertyName};", true);
        //                        CurrentTypeWriter.WriteLine(node, $"}}", true);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        void WritePropertySetAccessor(BasePropertyDeclarationSyntax node, string propertyName, AccessorDeclarationSyntax? accessor, ISymbol propertySymbol)
        {
            var symbol = OpenClosure(node);
            if (symbol is IPropertySymbol property && property.SetMethod != null)
            {
                CurrentClosure.DefineIdentifierType("value", CodeSymbol.From(property.SetMethod.Parameters.Last()));
            }
            else if (symbol is IEventSymbol @event && @event.AddMethod != null)
            {
                CurrentClosure.DefineIdentifierType("value", CodeSymbol.From(@event.AddMethod.Parameters.Last()));
            }
            WriteBlock(accessor?.ExpressionBody ?? accessor?.Body ?? (CSharpSyntaxNode)node, new CodeNode(() =>
            {
                if (accessor?.ExpressionBody != null)
                {
                    CurrentTypeWriter.Write(node, "", true);
                    Visit(accessor.ExpressionBody.Expression);
                    CurrentTypeWriter.WriteLine(node, ";");
                }
                else if (accessor?.Body != null)
                {
                    VisitChildren(accessor.Body.Statements);
                }
                else
                {
                    var declaringMetadata = _global.GetRequiredMetadata(propertySymbol.ContainingType);
                    var propertyMetadata = _global.GetRequiredMetadata(propertySymbol);
                    CurrentTypeWriter.WriteLine(node, $"{(propertySymbol.IsStatic ? "" : "this.")}{propertyMetadata.InvocationName ?? propertyName}$ = value;", true);
                }
            }));
            CloseClosure(node);
        }

        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            if (node.Modifiers.IsPartial() && node.AccessorList == null)
                return;
            EnsureImported(node.Type);
            string? modifier = null;
            if (node.Modifiers.IsConst() || node.Modifiers.IsStatic())
            {
                modifier += "static ";
            }
            //bool backingFieldWritten = false;
            //void EnsureWriteBackingField()
            //{
            //    if (!backingFieldWritten)
            //    {
            //    }
            //    backingFieldWritten = true;
            //}
            var symbol = (IEventSymbol)_global.GetSymbol(node, this);
            var metadata = _global.GetRequiredMetadata(symbol);
            if (node.AccessorList != null)
            {
                foreach (var accessor in node.AccessorList.Accessors)
                {
                    if (accessor.IsKind(SyntaxKind.AddAccessorDeclaration))
                    {
                        var addMethodMetadata = _global.GetRequiredMetadata(symbol.AddMethod!);
                        CurrentTypeWriter.WriteLine(node, $"/*{node.Type.ToString().Trim()}*/{modifier} {(addMethodMetadata?.OverloadName ?? $"add_{metadata.OverloadName}")}(value)", true);
                        WritePropertyGetAccessor(node, node.Identifier.ValueText, accessor, symbol);
                    }
                    else if (accessor.IsKind(SyntaxKind.RemoveAccessorDeclaration))
                    {
                        var removeMethodMetadata = _global.GetRequiredMetadata(symbol.RemoveMethod!);
                        CurrentTypeWriter.WriteLine(node, $"/*{node.Type.ToString().Trim()}*/{modifier} {(removeMethodMetadata?.OverloadName ?? $"remove_{metadata.OverloadName}")}(value)", true);
                        WritePropertySetAccessor(node, node.Identifier.ValueText, accessor, symbol);
                    }
                }
            }
            else
            {
                throw new NotImplementedException("this should be an event field declaration?");
                //WriteEventAddRemove(node);
            }
            //base.VisitEventDeclaration(node);
        }

        public override void VisitFieldExpression(FieldExpressionSyntax node)
        {
            var containigType = node.FindClosestParent<BaseTypeDeclarationSyntax>() ?? throw new InvalidOperationException("field must be inside a property");
            var typeSymbol = _global.GetSymbol(containigType, this);
            var typeMetadata = _global.GetRequiredMetadata(typeSymbol);
            var containigProperty = node.FindClosestParent<PropertyDeclarationSyntax>() ?? throw new InvalidOperationException("field must be inside a property");
            var propertyName = containigProperty.Identifier.ValueText;
            bool isStatic = containigProperty.Modifiers.IsStatic();
            CurrentTypeWriter.Write(node, $"{(!isStatic ? "this" : typeMetadata.InvocationName ?? typeSymbol.Name)}.{propertyName}$");
            //base.VisitFieldExpression(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Modifiers.IsExtern())
                return;
            if (node.Modifiers.IsPartial() && (node.AccessorList == null || node.AccessorList.Accessors.All(a => a.ExpressionBody == null && a.Body == null)))
                return;
            if (node.Parent.IsKind(SyntaxKind.InterfaceDeclaration) &&
                (node.AccessorList == null || node.AccessorList.Accessors.All(a => a.ExpressionBody == null && a.Body == null)) &&
                (node.ExpressionBody == null))
                return;
            var propertySymbol = (IPropertySymbol)_global.GetSymbol(node, this/*, out _, out _*/);
            var propertyMetadata = _global.GetRequiredMetadata(propertySymbol);
            bool external = _global.HasAttribute(propertySymbol, typeof(TemplateAttribute).FullName!, this, false, out _);
            var propertyName = propertyMetadata.OverloadName ?? node.Identifier.ResolveIdentifierName();
            if (external)
                return;
            if (!node.Modifiers.IsAbstract())
            {
                //CurrentClosure.DefineIdentifierType(propertySymbol.Name, CodeType.From(propertySymbol));
                EnsureImported(node.Type);
                //register the symbol of this identifier in the closere
                CurrentClosure.DefineIdentifierType(propertySymbol.Name, CodeSymbol.From(propertySymbol));
                string? modifier = GetMethodModifier(node, node.Modifiers, node.Type);

                bool isStaticConvention = false;
                string? smodifier = null;
                if (!propertySymbol.IsStatic && propertySymbol.IsStaticCallConvention(_global))
                {
                    isStaticConvention = true;
                    smodifier = "static/*conventional*/ ";
                }

                var declaringMetadata = _global.GetRequiredMetadata(propertySymbol.ContainingType);
                //closures.Push(new CodeBlockClosure(global, semanticModel, node, methodSymbol));
                //var methodName = metadata?.OverloadedName ?? Utilities.ResolveMethodName(node);
                var defaultValue = _global.GetDefaultValue(node.Type, this);
                bool isLiteralInit = MemberIsLiteralInitialization(node.Initializer, propertySymbol.Type);
                bool isFieldLayout = _global.HasAttribute(propertySymbol.ContainingType, typeof(StructLayoutAttribute).FullName!, this, false, out _);
                //node.Initializer != null &&
                //(_global.EvaluateConstant(node.Initializer.Value, this).HasValue || (node.Initializer.Value is LiteralExpressionSyntax || (node.Initializer.Value is PrefixUnaryExpressionSyntax pu && pu.Operand is LiteralExpressionSyntax))) &&
                //propertySymbol.Type.IsJsPrimitive();
                void WriteInitializer()
                {
                    if (node.Initializer != null ||
                        ((propertySymbol.Type.IsValueType && !propertySymbol.Type.IsNumericType()) &&//.SpecialType == SpecialType.System_ValueType &&
                        node.ExpressionBody == null &&
                        node.AccessorList!.Accessors.All(a => a.ExpressionBody == null && a.Body == null)))
                    {
                        //If we initialize a property from a primary constructor parameter, this is already handled in the primary constructor generator (WritePrimaryConstructor)
                        //We should skip it here
                        if (MemberWasMarkedInitializedByPrimaryConstructor(node, node.Initializer))
                        {
                        }
                        else
                        {
                            bool isStaticInit = node.Modifiers.IsStatic();
                            var initLocation = isStaticInit ? TypeInitializerLocation.DefaultStaticConstructor : TypeInitializerLocation.DefaultInstanceConstructor;

                            // if class has primary constructor and the field being initialized needs a parameter from the constructor, we initialize that field in the primary constructor
                            if (node.Initializer?.Value != null && ((CurrentType is ClassDeclarationSyntax cls && cls.ParameterList != null) || (CurrentType is StructDeclarationSyntax str && str.ParameterList != null)))
                            {
                                var parameters = (CurrentType as ClassDeclarationSyntax)?.ParameterList ?? (CurrentType as StructDeclarationSyntax)!.ParameterList;
                                if (MemberReferencesPrimaryConstructorParameter(node.Initializer.Value, parameters!.Parameters))
                                {
                                    initLocation = TypeInitializerLocation.PrimaryConstructor;
                                }
                            }

                            CurrentClosure.RegisterTypeInitializer(() =>
                            {
                                if (initLocation == TypeInitializerLocation.PrimaryConstructor)
                                {
                                    CurrentTypeWriter.WriteLine(node, $"//depends on primary constructor parameter", true);
                                }
                                //If we are in a static initilizer, it is safe to use this as it reference the class prototype itself
                                CurrentTypeWriter.Write(node, $"{(isStaticInit ? "this" /*declaringMetadata.InvocationName + "."*/ : "this")}", true);
                                CurrentTypeWriter.Write(node, ".");
                                CurrentTypeWriter.Write(node, propertyName);
                                CurrentTypeWriter.Write(node, " = ");
                                //Visit(node.Initializer);
                                if (node.Initializer != null)
                                {
                                    if (!TryWriteConstant(node, propertySymbol.Type, node.Initializer!.Value))
                                    {
                                        ////We are inside js default constructor, if the node.Initializer.Value is a primary constructor parameter, we should not write it as that variable is undefined here
                                        //var symbol = _global.TryGetSymbol(node.Initializer.Value, this);
                                        //if (initLocation != TypeInitializerLocation.PrimaryConstructor && symbol != null && symbol.Kind == SymbolKind.Parameter)
                                        //{
                                        //    if (defaultValue != null)
                                        //        CurrentTypeWriter.Write(node, defaultValue);
                                        //    else
                                        //        CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}({propertySymbol.Type.ComputeOutputTypeName(_global)})");
                                        //}
                                        //else
                                        WriteVariableAssignment(node, null, propertySymbol, null, node.Initializer.Value, null);
                                    }
                                }
                                else //handles value type
                                {
                                    if (defaultValue != null)
                                        CurrentTypeWriter.Write(node, defaultValue);
                                    else
                                        CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}({propertySymbol.Type.ComputeOutputTypeName(_global)})");
                                }
                                CurrentTypeWriter.WriteLine(node, ";");
                            }, initLocation);
                        }
                    }
                }
                bool backingFieldWritten = false;
                void EnsureWriteBackingField()
                {
                    if (!backingFieldWritten)
                    {
                        if (isFieldLayout && TryWriteFieldLayout(node, propertySymbol, propertySymbol.Type, propertyName, $"{(node.Modifiers.IsStatic() ? "static " : "")}", node.Type.ToString().Trim()))
                        {
                        }
                        else
                        {
                            CurrentTypeWriter.WriteLine(node, $"/*{node.Type.ToString().Trim()}*/ {(node.Modifiers.IsStatic() ? "static " : "")}{propertyName}${(defaultValue != null ? $" = {defaultValue}" : "")};", true);
                        }
                        WriteInitializer();
                    }
                    backingFieldWritten = true;
                }
                if (node.AccessorList != null)
                {
                    if (node.AccessorList.Accessors.All(a => a.ExpressionBody == null && a.Body == null)) //is an auto property, simply write as a field to save space
                    {
                        if (isFieldLayout && TryWriteFieldLayout(node, propertySymbol, propertySymbol.Type, propertyName, $"{(node.Modifiers.IsStatic() ? "static " : "")}", node.Type.ToString().Trim()))
                        {
                        }
                        else
                        {
                            CurrentTypeWriter.WriteLine(node, $"/*{node.Type.ToString().Trim()}*/ {(node.Modifiers.IsStatic() ? "static " : "")}{propertyName}{(defaultValue != null ? $" = {defaultValue}" : "")};", true);
                        }
                        WriteInitializer();
                    }
                    else
                    {
                        foreach (var accessor in node.AccessorList.Accessors)
                        {
                            if (propertySymbol.ContainingType.TypeKind == TypeKind.Interface && accessor.ExpressionBody == null && accessor.Body == null)
                                continue;
                            bool usesFieldKeyword = node.DescendantNodes().Any(e => e.IsKind(SyntaxKind.FieldExpression));
                            if (usesFieldKeyword || (accessor.ExpressionBody == null && accessor.Body == null))
                                EnsureWriteBackingField();
                            if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                            {
                                if (node.Parent.IsKind(SyntaxKind.ExtensionBlockDeclaration) && !propertySymbol.IsStatic)
                                {
                                    var extensionBlock = (ExtensionBlockDeclarationSyntax)node.Parent;
                                    var extensionParameter = extensionBlock.ParameterList!.Parameters.Single();
                                    CurrentTypeWriter.WriteLine(node, $"static {smodifier}{modifier} {(Constants.CompatibleExtensionPropertyGetSetMethod ? "get_" : "")}{propertyName}{(!Constants.CompatibleExtensionPropertyGetSetMethod ? "$get" : "")}(/*this {extensionParameter.Type}*/{extensionParameter.Identifier.ValueText})", true);
                                }
                                else
                                {
                                    CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} {(!isStaticConvention ? "get " : "")}{propertyName}{(isStaticConvention ? "$get" : "")}()", true);
                                }
                                WritePropertyGetAccessor(node, node.Identifier.ValueText, accessor, propertySymbol);
                            }
                            else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
                            {
                                if (node.Parent.IsKind(SyntaxKind.ExtensionBlockDeclaration) && !propertySymbol.IsStatic)
                                {
                                    var extensionBlock = (ExtensionBlockDeclarationSyntax)node.Parent;
                                    var extensionParameter = extensionBlock.ParameterList!.Parameters.Single();
                                    CurrentTypeWriter.WriteLine(node, $"static {smodifier}{modifier} {(Constants.CompatibleExtensionPropertyGetSetMethod ? "set_" : "")}{propertyName}{(!Constants.CompatibleExtensionPropertyGetSetMethod ? "$set" : "")}(/*this {extensionParameter.Type}*/{extensionParameter.Identifier.ValueText}, value)", true);
                                }
                                else
                                {
                                    CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} {(!isStaticConvention ? "set " : "")}{propertyName}{(isStaticConvention ? "$set" : "")}(value)", true);
                                }
                                WritePropertySetAccessor(node, node.Identifier.ValueText, accessor, propertySymbol);
                            }
                        }
                        //if only getter is defined, we need a private setter too
                        if (backingFieldWritten && !node.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration)))
                        {
                            CurrentTypeWriter.WriteLine(node, $"{smodifier} {modifier} {(!isStaticConvention ? "set " : "")}{propertyName}{(isStaticConvention ? "$set" : "")}(value)", true);
                            WritePropertySetAccessor(node, node.Identifier.ValueText, null, propertySymbol);
                        }
                    }
                }
                else if (node.ExpressionBody != null)
                {
                    OpenClosure(node);
                    bool usesFieldKeyword = node.DescendantNodes().Any(e => e.IsKind(SyntaxKind.FieldExpression));
                    if (usesFieldKeyword)
                        EnsureWriteBackingField();
                    if (node.Parent.IsKind(SyntaxKind.ExtensionBlockDeclaration) && !propertySymbol.IsStatic)
                    {
                        var extensionBlock = (ExtensionBlockDeclarationSyntax)node.Parent;
                        var extensionParameter = extensionBlock.ParameterList!.Parameters.Single();
                        CurrentTypeWriter.WriteLine(node, $"static {smodifier}{modifier} {(Constants.CompatibleExtensionPropertyGetSetMethod ? "get_" : "")}{propertyName}{(!Constants.CompatibleExtensionPropertyGetSetMethod ? "$get" : "")}(/*this {extensionParameter.Type}*/{extensionParameter.Identifier.ValueText})", true);
                    }
                    else
                    {
                        CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} {(!isStaticConvention ? "get " : "")}{propertyName}{(isStaticConvention ? "$get" : "")}()", true);
                    }
                    WriteBlock(node.ExpressionBody, new CodeNode(() =>
                    {
                        if (HasYield(node))
                            TryWrapInYieldingGetEnumerable(node, (node.Type as GenericNameSyntax)?.TypeArgumentList.Arguments, [node.ExpressionBody.Expression]);
                        else
                        {
                            if (!node.ExpressionBody.Expression.IsKind(SyntaxKind.ThrowExpression)/* is not ThrowExpressionSyntax*/)
                            {
                                string? cacheKey = null;
                                if (propertySymbol.IsStatic && SymbolEqualityComparer.Default.Equals(propertySymbol.Type.OriginalDefinition, _global.SystemReadOnlySpan))
                                {
                                    //Optimize ReadOnlySpan properties by caching the result, instead of making a new one everytime the property is read
                                    cacheKey = $"this.$cache{propertySymbol.Name}";
                                }
                                WriteReturn(node, node.ExpressionBody.Expression, cacheKey);
                            }
                            else
                            {
                                CurrentTypeWriter.Write(node, "", true);
                                Visit(node.ExpressionBody.Expression);
                                CurrentTypeWriter.WriteLine(node, ";");
                            }
                        }
                    }));
                    CloseClosure(node);
                }
                else
                {
                    CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} $_{propertyName}{(defaultValue != null ? $" = {defaultValue}" : "")};");
                    CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} get_{propertyName}()");
                    CurrentTypeWriter.WriteLine(node, $"{{");
                    CurrentTypeWriter.WriteLine(node, $"return {(propertySymbol.IsStatic ? declaringMetadata.InvocationName : "this")}.{propertyName}$;");
                    CurrentTypeWriter.WriteLine(node, $"}}");
                    CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} set_{propertyName}(value)");
                    CurrentTypeWriter.WriteLine(node, $"{{");
                    CurrentTypeWriter.WriteLine(node, $"return {(propertySymbol.IsStatic ? declaringMetadata.InvocationName : "this")}.{propertyName}$ = value;");
                    CurrentTypeWriter.WriteLine(node, $"}}");
                }
                if (isStaticConvention)
                {
                    if (propertySymbol.GetMethod != null)
                    {
                        CurrentTypeWriter.WriteLine(node, $"//Static convention instance get redirect", true);
                        CurrentTypeWriter.WriteLine(node, $"{modifier} get {propertyName}() {{ return {propertyMetadata.InvocationName}$get.apply(this); }}", true);
                    }
                    if (propertySymbol.SetMethod != null)
                    {
                        CurrentTypeWriter.WriteLine(node, $"//Static convention instance set redirect", true);
                        CurrentTypeWriter.WriteLine(node, $"{modifier} set {propertyName}(value) {{ return {propertyMetadata.InvocationName}$set.apply(this, [value]); }}", true);
                    }
                }
            }
            //TryWriteImplementedPropertyGetter(node, propertySymbol, propertyName);
            //TryWriteImplementedPropertySetter(node, propertySymbol, propertyName);
        }

        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            if (node.Modifiers.IsExtern())
            {
                return;
            }
            if (!node.Modifiers.IsAbstract())
            {
                EnsureImported(node.Type);
                string? modifier = null;
                var symbol = _global.GetSymbol(node, this/*, out _, out _*/);
                if (node.Modifiers.IsStatic())
                {
                    modifier += "static ";
                }
                else if (symbol.IsStaticCallConvention(_global))
                {
                    modifier = "static/*conventional*/ ";
                }
                if (node.AccessorList != null)
                {
                    foreach (var accessor in node.AccessorList.Accessors)
                    {
                        if (symbol.ContainingType.TypeKind == TypeKind.Interface && accessor.ExpressionBody == null && accessor.Body == null)
                            continue;
                        if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                        {
                            if (!node.Modifiers.IsExtern())
                            {
                                var propertySymbol = (IPropertySymbol)OpenClosure(node);
                                var propertyMetadata = _global.GetRequiredMetadata(propertySymbol.GetMethod!);
                                CurrentTypeWriter.Write(node, $"/*{node.Type.ToString().Trim()}*/ {modifier}{propertyMetadata?.OverloadName ?? "get_Item"}(", true);
                                WriteMethodDeclarationParameters(node, node.ParameterList.Parameters);
                                CurrentTypeWriter.WriteLine(node, $")");
                                WriteBlock(accessor.Body ?? accessor.ExpressionBody ?? (CSharpSyntaxNode)node, new CodeNode(() =>
                                {
                                    if (accessor.ExpressionBody != null)
                                    {
                                        CurrentTypeWriter.Write(node, $"return ", true);
                                        Visit(accessor.ExpressionBody.Expression);
                                        CurrentTypeWriter.WriteLine(node, $";");
                                    }
                                    else if (accessor.Body != null)
                                    {
                                        VisitChildren(accessor.Body.Statements);
                                    }
                                }));
                                CloseClosure(node);
                            }
                        }
                        else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
                        {
                            if (!node.Modifiers.IsExtern())
                            {
                                var propertySymbol = (IPropertySymbol)OpenClosure(node);
                                var propertyMetadata = _global.GetRequiredMetadata(propertySymbol.SetMethod!);
                                CurrentTypeWriter.Write(node, $"/*void*/ {modifier}{propertyMetadata?.OverloadName ?? "set_Item"}(", true);
                                CurrentClosure.DefineIdentifierType("value", CodeSymbol.From(propertySymbol.SetMethod!.Parameters.Last()));
                                WriteMethodDeclarationParameters(node, node.ParameterList.Parameters);
                                if (node.ParameterList.Parameters.Any())
                                    CurrentTypeWriter.Write(node, ", ");
                                CurrentTypeWriter.Write(node, $"/*{node.Type.ToString().Trim()}*/ value");
                                CurrentTypeWriter.WriteLine(node, $")");
                                WriteBlock(accessor.Body ?? accessor.ExpressionBody ?? (CSharpSyntaxNode)node, new CodeNode(() =>
                                {
                                    if (accessor.ExpressionBody != null)
                                    {
                                        CurrentTypeWriter.Write(node, "", true);
                                        Visit(accessor.ExpressionBody.Expression);
                                        CurrentTypeWriter.WriteLine(node, ";");
                                    }
                                    else if (accessor.Body != null)
                                    {
                                        VisitChildren(accessor.Body.Statements);
                                    }
                                }));
                                CloseClosure(node);
                            }
                        }
                    }
                }
                else if (node.ExpressionBody != null)
                {
                    var propertySymbol = (IPropertySymbol)OpenClosure(node);
                    var propertyMetadata = _global.GetRequiredMetadata(propertySymbol.GetMethod!);
                    CurrentTypeWriter.Write(node, $"/*{node.Type.ToString().Trim()}*/ {modifier}{(propertyMetadata.OverloadName ?? "get_Item")}(", true);
                    WriteMethodDeclarationParameters(node, node.ParameterList.Parameters);
                    CurrentTypeWriter.WriteLine(node, $")");
                    WriteBlock(node.ExpressionBody, new CodeNode(() =>
                    {
                        WriteReturn(node, node.ExpressionBody.Expression);
                        //Writer.Write(node, $"return ", true);
                        //Visit(node.ExpressionBody.Expression);
                        CurrentTypeWriter.WriteLine(node, $";");
                    }));
                    CloseClosure(node);
                }
                else
                {

                }
            }
            //var mpropertySymbol = (IPropertySymbol)_global.GetSymbol(node, this/*, out _, out _*/);
            //var mpropertyMetadata = _global.GetRequiredMetadata(mpropertySymbol.GetMethod!);
            //TryWriteImplementedPropertyGetter(node, mpropertySymbol, $"{mpropertyMetadata?.OverloadName ?? "get_Item"}(...arguments)");
            //TryWriteImplementedPropertySetter(node, mpropertySymbol, $"{mpropertyMetadata?.OverloadName ?? "set_Item"}(...arguments)");
            //base.VisitIndexerDeclaration(node);
        }

    }
}
