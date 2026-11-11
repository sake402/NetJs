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
            var currentType = CurrentTypeSymbol;// _global.GetTypeSymbol(CurrentType, this);
            IEnumerable<INamedTypeSymbol> GetAllBaseTypes(ITypeSymbol type)
            {
                if (type.BaseType != null)
                {
                    yield return type.BaseType;
                    foreach (var t in GetAllBaseTypes(type.BaseType))
                        yield return t;
                }
                else if (type.TypeKind == TypeKind.Interface)
                {
                    yield return _global.SystemObject;
                }
                foreach (var t in type.AllInterfaces)
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
            if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType.OriginalDefinition, currentType) ||
                /* IsOnBaseType(currentType)*/ GetAllBaseTypes(currentType).Contains(symbol.ContainingType, SymbolEqualityComparer.Default))
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
                identifierSymbol = _global.TryGetSymbol(currentExpressionNamespace + "." + identifier.ValueText, this/*, out declaringSymbol, out declaringKind*/) ?? _global.TryGetSymbol(node, this);
            }
            else
            {
                identifierSymbol = _global.TryGetSymbol(node, this);
            }
            if (identifierSymbol != null)
            {
                MarkCurrentClosureDependsOn(identifierSymbol);
            }
            if (identifierSymbol is INamespaceSymbol ns)
            {
                if (currentExpressionNamespace?.Length > 0)
                    currentExpressionNamespace += ".";
                currentExpressionNamespace += ns.Name;
            }
            else if (identifierSymbol is IFieldSymbol field && field.IsConst && (_global.BuildFlags.HasFlag(NetJsBuildFlags.InlineConstants) || _global.HasAttribute(field, typeof(InlineConstAttribute).FullName!, this, false, out _)))
            {
                currentExpressionNamespace = null;
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
                    WriteMemberAccess(node, null, identifierSymbol!.ContainingType, identifierSymbol.Name, identifierSymbol);
                }
                else if (identifierSymbol != null && identifierSymbol.Kind != SymbolKind.TypeParameter && (identifierSymbol is INamedTypeSymbol || identifierSymbol is INamespaceSymbol))
                {
                    var symbolMetadata = _global.GetRequiredMetadata(identifierSymbol);
                    var ivName = symbolMetadata.InvocationName;
                    CurrentTypeWriter.Write(node, ivName ?? identifierSymbol.Name);
                }
                else
                {
                    CurrentTypeWriter.Write(node, identifier.ResolveIdentifierName());
                }
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
