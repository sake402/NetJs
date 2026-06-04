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
            var symbol = OpenClosure(node);
            if (accessor.ExpressionBody != null)
            {
                CurrentTypeWriter.WriteLine(node, "{", true);
                if (!accessor.ExpressionBody.Expression.IsKind(SyntaxKind.ThrowExpression)/* is not ThrowExpressionSyntax*/)
                    CurrentTypeWriter.Write(node, $"return ", true);
                else
                    CurrentTypeWriter.Write(node, $"", true);
                Visit(accessor.ExpressionBody.Expression);
                CurrentTypeWriter.WriteLine(node, $";");
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            else if (accessor.Body != null)
            {
                TryWrapInYieldingGetEnumerable(node, (node.Type as GenericNameSyntax)?.TypeArgumentList.Arguments, [accessor.Body]);
                //VisitChildren(accessor.Body.Statements);
            }
            else
            {
                CurrentTypeWriter.WriteLine(node, "{", true);
                var declaringMetadata = _global.GetRequiredMetadata(propertySymbol.ContainingType);
                var propertyMetadata = _global.GetRequiredMetadata(propertySymbol);
                CurrentTypeWriter.WriteLine(node, $"return {(propertySymbol.IsStatic ? "" : "this.")}{propertyMetadata.InvocationName ?? propertyName}$;", true);
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            CloseClosure();
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
            CurrentTypeWriter.WriteLine(node, "{", true);
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
            CurrentTypeWriter.WriteLine(node, "}", true);
            CloseClosure();
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
            var symbol = _global.GetSymbol(node, this/*, out _, out _*/);
            var metadata = _global.GetRequiredMetadata(symbol);
            if (node.AccessorList != null)
            {
                foreach (var accessor in node.AccessorList.Accessors)
                {
                    //if (accessor.ExpressionBody == null && accessor.Body == null)
                    //EnsureWriteBackingField();
                    if (accessor.IsKind(SyntaxKind.AddAccessorDeclaration))
                    {
                        CurrentTypeWriter.WriteLine(node, $"/*{node.Type.ToString().Trim()}*/{modifier} {metadata.OverloadName}$add(value)", true);
                        WritePropertyGetAccessor(node, node.Identifier.ValueText, accessor, symbol);
                    }
                    else if (accessor.IsKind(SyntaxKind.RemoveAccessorDeclaration))
                    {
                        CurrentTypeWriter.WriteLine(node, $"/*{node.Type.ToString().Trim()}*/{modifier} {metadata.OverloadName}$remove(value)", true);
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
                var defaultValue = node.Initializer == null ? _global.GetDefaultValue(node.Type, this) : null;
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
                        if (MemberWasInitializedByPrimaryConstructor(node, node.Initializer))
                        {
                        }
                        else
                        {
                            bool isStaticInit = node.Modifiers.IsStatic();
                            CurrentClosure.RegisterTypeInitializer(() =>
                            {
                                //If we are in a static initilizer, it is safe to use this as it reference the class prototype itself
                                CurrentTypeWriter.Write(node, $"{(isStaticInit ? "this" /*declaringMetadata.InvocationName + "."*/ : "this")}", true);
                                CurrentTypeWriter.Write(node, ".");
                                CurrentTypeWriter.Write(node, propertyName);
                                CurrentTypeWriter.Write(node, " = ");
                                //Visit(node.Initializer);
                                if (node.Initializer != null)
                                {
                                    if (!TryWriteConstant(node, propertySymbol.Type, node.Initializer!.Value))
                                        WriteVariableAssignment(node, null, propertySymbol, null, node.Initializer.Value, null);
                                }
                                else //handles value type
                                {
                                    if (defaultValue != null)
                                        CurrentTypeWriter.Write(node, defaultValue);
                                    else
                                        CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DefaultTypeName}({propertySymbol.Type.ComputeOutputTypeName(_global)})");
                                }
                                CurrentTypeWriter.WriteLine(node, ";");
                            }, isStaticInit);
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
                                if (node.Parent.IsKind(SyntaxKind.ExtensionBlockDeclaration))
                                {
                                    var extensionBlock = (ExtensionBlockDeclarationSyntax)node.Parent;
                                    var extensionParameter = extensionBlock.ParameterList!.Parameters.Single();
                                    CurrentTypeWriter.WriteLine(node, $"static {smodifier}{modifier} {propertyName}$get(/*this {extensionParameter.Type}*/{extensionParameter.Identifier.ValueText})", true);
                                }
                                else
                                {
                                    CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} {(!isStaticConvention ? "get " : "")}{propertyName}{(isStaticConvention ? "$get" : "")}()", true);
                                }
                                WritePropertyGetAccessor(node, node.Identifier.ValueText, accessor, propertySymbol);
                            }
                            else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                            {
                                if (node.Parent.IsKind(SyntaxKind.ExtensionBlockDeclaration))
                                {
                                    var extensionBlock = (ExtensionBlockDeclarationSyntax)node.Parent;
                                    var extensionParameter = extensionBlock.ParameterList!.Parameters.Single();
                                    CurrentTypeWriter.WriteLine(node, $"static {smodifier}{modifier} {propertyName}$set(/*this {extensionParameter.Type}*/{extensionParameter.Identifier.ValueText}, value)", true);
                                }
                                else
                                {
                                    CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} {(!isStaticConvention ? "set " : "")}{propertyName}{(isStaticConvention ? "$set" : "")}(value)", true);
                                }
                                WritePropertySetAccessor(node, node.Identifier.ValueText, accessor, propertySymbol);
                            }
                        }
                        //if only getter is defined, we need a private setter too
                        if (backingFieldWritten && !node.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)))
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
                    if (node.Parent.IsKind(SyntaxKind.ExtensionBlockDeclaration))
                    {
                        var extensionBlock = (ExtensionBlockDeclarationSyntax)node.Parent;
                        var extensionParameter = extensionBlock.ParameterList!.Parameters.Single();
                        CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} {propertyName}(/*this {extensionParameter.Type}*/{extensionParameter.Identifier.ValueText})", true);
                    }
                    else
                    {
                        CurrentTypeWriter.WriteLine(node, $"{smodifier}{modifier} {(!isStaticConvention ? "get " : "")}{propertyName}{(isStaticConvention ? "$get" : "")}()", true);
                    }
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    if (HasYield(node))
                        TryWrapInYieldingGetEnumerable(node, (node.Type as GenericNameSyntax)?.TypeArgumentList.Arguments, [node.ExpressionBody.Expression]);
                    else
                    {
                        if (!node.ExpressionBody.Expression.IsKind(SyntaxKind.ThrowExpression)/* is not ThrowExpressionSyntax*/)
                        {
                            WriteReturn(node, node.ExpressionBody.Expression);
                        }
                        else
                        {
                            CurrentTypeWriter.Write(node, "", true);
                            Visit(node.ExpressionBody.Expression);
                            CurrentTypeWriter.WriteLine(node, ";");
                        }
                    }
                    CurrentTypeWriter.WriteLine(node, "}", true);
                    CloseClosure();
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
                                CurrentTypeWriter.WriteLine(node, "{", true);
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
                                CurrentTypeWriter.WriteLine(node, "}", true);
                                CloseClosure();
                            }
                        }
                        else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
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
                                CurrentTypeWriter.WriteLine(node, "{", true);
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
                                CurrentTypeWriter.WriteLine(node, "}", true);
                                CloseClosure();
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
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    WriteReturn(node, node.ExpressionBody.Expression);
                    //Writer.Write(node, $"return ", true);
                    //Visit(node.ExpressionBody.Expression);
                    CurrentTypeWriter.WriteLine(node, $";");
                    CurrentTypeWriter.WriteLine(node, "}", true);
                    CloseClosure();
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
