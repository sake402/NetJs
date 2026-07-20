using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        bool SymbolIsTypeMember(ISymbol? symbol, out bool isPrimaryConstructorParameter)
        {
            isPrimaryConstructorParameter = false;
            if (symbol == null)
                return false;
            if (symbol.ContainingSymbol?.Kind == SymbolKind.NamedType)
            {
                if (symbol.Kind == SymbolKind.Method)
                    return true;
                if (symbol.Kind == SymbolKind.Property)
                    return true;
                if (symbol.Kind == SymbolKind.Field)
                    return true;
                if (symbol.Kind == SymbolKind.Event)
                    return true;
            }
            //primary constructor parameter are rewritten as field members
            if (symbol.Kind == SymbolKind.Parameter && symbol.ContainingSymbol is IMethodSymbol method && method.IsPrimaryConstructor(_global))
            {
                isPrimaryConstructorParameter = true;
                return true;
            }
            return false;
        }

        bool SymbolIsThisTypeMember(ISymbol? symbol, out bool isPrimaryConstructorParameter)
        {
            isPrimaryConstructorParameter = false;
            if (symbol == null)
                return false;
            var currentType = _global.GetTypeSymbol(CurrentType, this);
            IEnumerable<INamedTypeSymbol> GetAllBaseTypes(ITypeSymbol type)
            {
                if (type.BaseType != null)
                {
                    yield return type.BaseType;
                    foreach (var t in GetAllBaseTypes(type.BaseType))
                        yield return t;
                }
                foreach (var t in type.Interfaces)
                    yield return t;
            }
            bool IsOnBaseType(ITypeSymbol? type)
            {
                while (type?.BaseType != null)
                {
                    if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType.OriginalDefinition, type.BaseType.OriginalDefinition))
                    {
                        return true;
                    }
                    type = type.BaseType;
                }
                return false;
            }
            if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType.OriginalDefinition, currentType) ||/* IsOnBaseType(currentType)*/ GetAllBaseTypes(currentType).Contains(symbol.ContainingType, SymbolEqualityComparer.Default))
            {
                if (symbol.ContainingSymbol?.Kind == SymbolKind.NamedType)
                {
                    if (symbol.Kind == SymbolKind.Method)
                        return true;
                    if (symbol.Kind == SymbolKind.Property)
                        return true;
                    if (symbol.Kind == SymbolKind.Field)
                        return true;
                    if (symbol.Kind == SymbolKind.Event)
                        return true;
                }
                //primary constructor parameter are rewritten as field members
                if (symbol.Kind == SymbolKind.Parameter && symbol.ContainingSymbol is IMethodSymbol method && method.IsPrimaryConstructor(_global))
                {
                    isPrimaryConstructorParameter = true;
                    return true;
                }
            }
            return false;
        }

        string? currentExpressionNamespace;
        void WriteIdentifier(CSharpSyntaxNode node, SyntaxToken identifier)
        {
            ISymbol? identifierSymbol;
            //ISymbol? declaringSymbol = null;
            //SymbolKind declaringKind = SymbolKind.ErrorType;
            if (currentExpressionNamespace?.Length > 0)
            {
                identifierSymbol = _global.TryGetSymbol(currentExpressionNamespace + "." + identifier.ValueText, this/*, out declaringSymbol, out declaringKind*/);
            }
            else
            {
                identifierSymbol = _global.TryGetSymbol(node, this);
            }
            if (identifierSymbol is INamespaceSymbol ns)
            {
                if (currentExpressionNamespace?.Length > 0)
                    currentExpressionNamespace += ".";
                currentExpressionNamespace += ns.Name;
            }
            else if (identifierSymbol is IFieldSymbol field && field.IsConst && (_global.OutputMode.HasFlag(OutputMode.InlineConstants) || _global.HasAttribute(field, typeof(InlineConstAttribute).FullName, this, false, out _)))
            {
                if (!TryWriteConstant(node, field.Type, node)) //this shouldn't fail, but if it does we need to know
                {
                    throw new ApplicationException($"Cannot write constant expression for {field}");
                }
            }
            else
            {
                currentExpressionNamespace = null;
                if (SymbolIsTypeMember(identifierSymbol, out var isPrimaryConstructorParameter))
                {
                    //AttributeData? attribute = symbolType!.GetTemplateAttribute(_global);
                    //IMethodSymbol? method = null;
                    //if (attribute == null)
                    //{
                    //    if (symbolType is IPropertySymbol property)
                    //    {
                    //        if (isAssignment)
                    //        {
                    //            method = property.SetMethod;
                    //            attribute = property.SetMethod?.GetTemplateAttribute(_global);
                    //        }
                    //        else
                    //        {
                    //            method = property.GetMethod;
                    //            attribute = property.GetMethod?.GetTemplateAttribute(_global);
                    //        }
                    //    }
                    //}
                    //if (attribute != null)
                    //{
                    //    WriteMethodTemplate(node, null, null, false, method, null, attribute, default, assignmentRhs);
                    //    return;
                    //}
                    //bool isStaticConvention = symbolType?.IsStaticCallConvention(_global) ?? false;
                    //if (node.Parent is AssignmentExpressionSyntax assign && assign.Left == node && node.Parent?.Parent is InitializerExpressionSyntax)
                    //{
                    //    //do not write this inside initializer expression
                    //}
                    ////TODO: While in dotnet, there is no "this" instance in a static method/property.
                    ////For Js however if we call a static method like System.AppDomain.method(), the object System.AppDomain becomes "this" in the context of the method
                    ////Perhaps in the future we can explore using the available this within the method to access other static members like this.staticField instead of rewriting to $.System.AppDomain.staticField
                    ////We can shrink the code further with this approach
                    //else if (!(symbolType!.IsStatic || isStaticConvention))
                    //    Writer.Write(node, "this.");
                    //var symbolMetadata = isPrimaryConstructorParameter ? null : _global.GetRequiredMetadata(symbolType!);
                    //Writer.Write(node, symbolMetadata?.InvocationName ?? symbolType!.Name);
                    //if (isStaticConvention)
                    //{
                    //    Writer.Write(node, ".call(this)");
                    //}
                    //else
                    //{
                    WriteMemberAccess(node, null, identifierSymbol!.ContainingType, identifierSymbol.Name, identifierSymbol);
                    //}
                }
                else if (identifierSymbol != null && identifierSymbol.Kind != SymbolKind.TypeParameter && (identifierSymbol is INamedTypeSymbol || identifierSymbol is INamespaceSymbol))
                {
                    var symbolMetadata = _global.GetRequiredMetadata(identifierSymbol);
                    var ivName = symbolMetadata.InvocationName;
                    //if (!_global.ShouldExportType(identifierSymbol, this))
                    //{
                    //    identifierSymbol = _global.DeletedObject;
                    //    var msymbolMetadata = _global.GetRequiredMetadata(_global.DeletedObject);
                    //    ivName = $"/*{ivName}*/{msymbolMetadata.InvocationName}";
                    //}
                    //var symbolMetadata = _global.GetRequiredMetadata(symbolType);
                    CurrentTypeWriter.Write(node, ivName ?? identifierSymbol.Name);
                }
                else
                {
                    CurrentTypeWriter.Write(node, identifier.ResolveIdentifierName());
                }
                //var refKind = identifierSymbol?.GetRefKind();
                //if (refKind != null && refKind != RefKind.None)
                //{
                //    bool NeedsDereferenceAccess()
                //    {
                //        //if (symbolType?.Kind == SymbolKind.Field)
                //        //{
                //        //    var constructor = node.FindClosestParent<ConstructorDeclarationSyntax>();
                //        //    //if a field is assigned in a constructor, no dereference
                //        //    var method = node.FindClosestParent<MethodDeclarationSyntax>();
                //        //}
                //        //if (identifier.ValueText == "_reference")
                //        //{

                //        //}
                //        if (refKind != RefKind.Out && node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
                //        {
                //            var ass = (AssignmentExpressionSyntax)node.Parent;
                //            var right = ass.Right;
                //            RefKind? rightRefKind = null;
                //            if (right.IsKind(SyntaxKind.RefExpression))
                //            {
                //                rightRefKind = RefKind.Ref;
                //            }
                //            else
                //            {
                //                var rsymbolType = _global.TryGetSymbol(right, this);
                //                rightRefKind = rsymbolType?.GetRefKind();
                //            }
                //            if (rightRefKind != null && rightRefKind != RefKind.None)
                //            {
                //                return false;
                //            }
                //        }
                //        return node.Parent is BinaryExpressionSyntax ||
                //            node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression)/* is MemberAccessExpressionSyntax*/ ||
                //            node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression);
                //    }
                //    if (NeedsDereferenceAccess())
                //    {
                //        TryDereference(node);
                //    }
                //}
                //dereference ref or out keyword ulness we are passing it as parameter
                //if (node.Parent is not ArgumentSyntax)
                //{
                //    var cd = GetIdentifierTypeInScope(node.Identifier.ValueText);
                //    if (cd.TypeSyntaxOrSymbol is IParameterSymbol parameter && (parameter.RefKind == RefKind.Ref || parameter.RefKind == RefKind.Out))
                //    {
                //        Writer.Write(node, ".");
                //        Writer.Write(node, Constants.RefValueName);
                //    }
                //}
                //base.VisitIdentifierName(node);
            }
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            WriteIdentifier(node, node.Identifier);
        }

        public override void VisitGenericName(GenericNameSyntax node)
        {
            WriteIdentifier(node, node.Identifier);
            ////var genericType = _global.TryGetTypeSymbol(node, this/*, out _, out _*/) ?? _global.GetTypeSymbol($"{node.Identifier.ValueText}<{string.Join(",", Enumerable.Range(1, node.Arity).Select(c => ""))}>", this/*, out _, out _*/);
            ////var metadata = _global.GetRequiredMetadata(genericType);
            ////Writer.Write(node, metadata?.OverloadName ?? node.Identifier.ValueText);
            ////Writer.Write(node, metadata?.InvocationName ?? node.Identifier.ValueText);
            ////if (metadata?.InvocationName == null)
            ////{
            //Writer.Write(node, "(");
            //int i = 0;
            //foreach (var n in node.TypeArgumentList.Arguments)
            //{
            //    if (i > 0)
            //        Writer.Write(node, ", ");
            //    Visit(n);
            //    i++;
            //}
            //Writer.Write(node, ")");
            ////}
            ////Writer.Write($"{ResolveFullTypeName(node.Identifier)}");
            ////foreach (var n in node.TypeArgumentList.Arguments)
            ////{
            ////    Visit(n);
            ////}
            ////Writer.Write($">");
            ////base.VisitGenericName(node);
        }

        public override void VisitQualifiedName(QualifiedNameSyntax node)
        {
            var type = Global.TryGetSymbol(node, this);
            if (type?.Kind == SymbolKind.Field)
            {
                string? memberName = null;
                //if (node.Right is GenericNameSyntax gn)
                //{
                //    memberName = gn.Identifier.Text;
                //}
                //else
                //{
                memberName = node.Right.Identifier.ValueText;
                //}
                WriteMemberAccess(node, node.Left, null, memberName, null);
            }
            else
            {
                string? initialNs = currentExpressionNamespace;
                Visit(node.Left);
                //dont write dot if the identifierName is collected/not written yet
                if (initialNs == currentExpressionNamespace)
                    CurrentTypeWriter.Write(node, node.DotToken.ValueText);
                Visit(node.Right);
            }
            //base.VisitQualifiedName(node);
        }

    }
}
