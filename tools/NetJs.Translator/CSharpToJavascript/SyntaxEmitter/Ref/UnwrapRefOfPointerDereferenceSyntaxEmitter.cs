using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref
{
    /// <summary>
    /// Since we use the same type to abstract ref and pointer
    /// A syntax like ref *ptr or ref ptr[i] can shortcircuit to just the inner operand of the pointer
    /// </summary>
    internal class UnwrapRefOfPointerDereferenceSyntaxEmitter : SyntaxEmitter<RefExpressionSyntax>
    {
        public override bool TryEmit(RefExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Expression.IsKind(SyntaxKind.PointerIndirectionExpression) && node.Expression is PrefixUnaryExpressionSyntax pointerDeref)
            {
                visitor.Visit(pointerDeref.Operand);
                return true;
            }
            if (node.Expression.IsKind(SyntaxKind.ElementAccessExpression) && node.Expression is ElementAccessExpressionSyntax elementAccess)
            {
                var type = visitor.Global.TryGetTypeSymbol(elementAccess.Expression, visitor);
                if (type?.IsPointer(out _) ?? false)
                {
                    visitor.WritePointerAdvance(node, elementAccess.Expression, elementAccess.ArgumentList.Arguments[0]);
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Since we use the same type to abstract ref and pointer
    /// A syntax like ref *ptr or ref pointer[0] can shortcircuit to just the inner operand of the pointer
    /// </summary>
    internal class UnwrapRefOfPointerDerefereceFromArgumentSyntaxEmitter : SyntaxEmitter<ArgumentSyntax>
    {
        public override bool TryEmit(ArgumentSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.RefKindKeyword.ValueText.Length > 0 && node.Expression.IsKind(SyntaxKind.PointerIndirectionExpression) && node.Expression is PrefixUnaryExpressionSyntax pointerDeref)
            {
                visitor.Visit(pointerDeref.Operand);
                return true;
            }
            if (node.RefKindKeyword.ValueText.Length > 0 && node.Expression.IsKind(SyntaxKind.ElementAccessExpression) && node.Expression is ElementAccessExpressionSyntax elementAccess)
            {
                var symbol = visitor.Global.TryGetSymbol(elementAccess.Expression, visitor);
                if (symbol != null && !visitor.Global.IsFixedSizeField(symbol, out _, out _))
                {
                    var type = visitor.Global.GetTypeSymbol(symbol);
                    if (type?.IsPointer(out _) ?? false)
                    {
                        visitor.WritePointerAdvance(node, elementAccess.Expression, elementAccess.ArgumentList.Arguments[0]);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}