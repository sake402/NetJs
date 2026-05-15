using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Numbers
{
    /// <summary>
    /// When we do uint - uint >= uint and there is an overflow/underflow, make sure both are casted to proper uint before comparison
    /// </summary>
    sealed class UnsignedNumberComparisonSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.GreaterThanExpression) ||
                node.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
                node.IsKind(SyntaxKind.LessThanExpression) ||
                node.IsKind(SyntaxKind.LessThanOrEqualExpression) ||
                node.IsKind(SyntaxKind.EqualsExpression))
            {
                var lhsType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rhsType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if (lhsType != null &&
                    rhsType != null &&
                    lhsType.SpecialType == SpecialType.System_UInt32 &&
                    rhsType.SpecialType == SpecialType.System_UInt32)
                {
                    bool leftNeedConvert = false;
                    bool rightNeedConvert = false;
                    void Check(CSharpSyntaxNode node, ref bool needConvert)
                    {
                        bool _need = false;
                        node.VisitHierachy((node, depth) =>
                        {
                            if (node.IsKind(SyntaxKind.AddExpression) || node.IsKind(SyntaxKind.SubtractExpression) || node.IsKind(SyntaxKind.ExclusiveOrExpression))
                            {
                                _need = true;
                                return false;
                            }
                            else if (node.IsKind(SyntaxKind.CastExpression))
                            {
                                return false;
                            }
                            else if (node.IsKind(SyntaxKind.InvocationExpression))
                            {
                                return false;
                            }
                            else if (node is LiteralExpressionSyntax && depth == 0)
                            {
                                return false;
                            }
                            return true;
                        });
                        needConvert = _need;
                    }
                    Check(node.Left, ref leftNeedConvert);
                    Check(node.Right, ref rightNeedConvert);
                    if (leftNeedConvert || rightNeedConvert)
                    {
                        if (leftNeedConvert)
                            visitor.CurrentTypeWriter.Write(node, "(");
                        visitor.Visit(node.Left);
                        if (leftNeedConvert)
                            visitor.CurrentTypeWriter.Write(node, " >>> 0)");
                        visitor.CurrentTypeWriter.Write(node, " ");
                        visitor.CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);
                        visitor.CurrentTypeWriter.Write(node, " ");
                        if (rightNeedConvert)
                            visitor.CurrentTypeWriter.Write(node, "(");
                        visitor.Visit(node.Right);
                        if (rightNeedConvert)
                            visitor.CurrentTypeWriter.Write(node, " >>> 0)");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
