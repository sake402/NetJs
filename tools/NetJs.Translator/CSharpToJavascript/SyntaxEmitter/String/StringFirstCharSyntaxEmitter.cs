using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    /// <summary>
    /// Reading string._firstChar produces string.charCodeAt(0)
    /// </summary>
    sealed class StringFirstCharSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression) || node.IsKind(SyntaxKind.IdentifierName))
            {
                var field = visitor.Global.TryGetSymbol(node, visitor) as IFieldSymbol;
                if (field != null && field.Name == "_firstChar" && SymbolEqualityComparer.Default.Equals(field.ContainingType, visitor.Global.SystemString))
                {
                    if (node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression) && node.Parent is AssignmentExpressionSyntax assign && assign.Left == node)
                    {
                        //writing to string._firstChar, emit as normal field access and let the proxy handle it
                        return false;
                    }
                    if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        visitor.Visit(((MemberAccessExpressionSyntax)node).Expression);
                    }
                    else
                    {
                        visitor.CurrentTypeWriter.Write(node, "this");
                    }
                    visitor.CurrentTypeWriter.Write(node, ".charCodeAt(0)");
                    return true;
                }
            }
            return false;
        }
    }
}
