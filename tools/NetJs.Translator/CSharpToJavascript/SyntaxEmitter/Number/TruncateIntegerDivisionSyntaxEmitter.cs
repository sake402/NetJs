using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Number
{
    /// <summary>
    /// When we divide integer 1 by integer 2, we want to make sure the result is zero, not 0.5
    /// </summary>
    sealed class TruncateIntegerDivisionSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.DivideExpression))
            {
                var lhsType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rhsType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if (lhsType != null &&
                    rhsType != null &&
                    lhsType.IsIntegerNumericType() &&
                    rhsType.IsIntegerNumericType())
                {
                    visitor.CurrentTypeWriter.Write(node, visitor.Global.GlobalName);
                    visitor.CurrentTypeWriter.Write(node, ".trunc(");
                    visitor.Visit(node.Left);
                    visitor.CurrentTypeWriter.Write(node, " / ");
                    visitor.Visit(node.Right);
                    visitor.CurrentTypeWriter.Write(node, ")");
                    return true;
                }
            }
            return false;
        }
    }

}
