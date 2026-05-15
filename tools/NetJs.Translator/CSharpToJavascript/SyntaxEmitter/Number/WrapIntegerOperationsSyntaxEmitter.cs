using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Numbers
{
    /// <summary>
    /// All int32 or uint32 operations must produce int32 or uint32, clipped to the range of int32 or uint32.
    /// </summary>
    sealed class WrapIntegerOperationsSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddExpression) || node.IsKind(SyntaxKind.SubtractExpression) || node.IsKind(SyntaxKind.MultiplyExpression))
            {
                var lhsType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rhsType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if (lhsType != null && rhsType != null && lhsType.IsIntegerNumericType() && rhsType.IsIntegerNumericType())
                {
                    bool leftSigned = lhsType.IsSignedNumericType();
                    bool rightSigned = rhsType.IsSignedNumericType();
                    //if one of the operand is actually unsigned literal, it isnt signed
                    if (leftSigned && node.Left.IsKind(SyntaxKind.NumericLiteralExpression) && node.Left is LiteralExpressionSyntax ltl)
                    {
                        if (!ltl.Token.ValueText.StartsWith("-"))
                        {
                            leftSigned = false;
                        }
                    }
                    if (rightSigned && node.Right.IsKind(SyntaxKind.NumericLiteralExpression) && node.Right is LiteralExpressionSyntax ltr)
                    {
                        if (!ltr.Token.ValueText.StartsWith("-"))
                        {
                            rightSigned = false;
                        }
                    }
                    var isSigned = leftSigned || rightSigned;
                    visitor.CurrentTypeWriter.Write(node, "((");
                    visitor.Visit(node.Left);
                    visitor.CurrentTypeWriter.Write(node, " ");
                    visitor.CurrentTypeWriter.Write(node, node.OperatorToken.Text);
                    visitor.CurrentTypeWriter.Write(node, " ");
                    visitor.Visit(node.Right);
                    visitor.CurrentTypeWriter.Write(node, ")");
                    if (isSigned)
                    {
                        visitor.CurrentTypeWriter.Write(node, " | 0)");
                    }
                    else
                    {
                        visitor.CurrentTypeWriter.Write(node, " >>> 0)");
                    }
                    //visitor.CurrentTypeWriter.Write(node, visitor.Global.GlobalName);
                    //visitor.CurrentTypeWriter.Write(node, ".$wrap(");
                    //visitor.Visit(node.Left);
                    //visitor.CurrentTypeWriter.Write(node, " * ");
                    //visitor.Visit(node.Right);
                    //visitor.CurrentTypeWriter.Write(node, ", ");
                    //visitor.CurrentTypeWriter.Write(node, isSigned ? "1" : "0");
                    //visitor.CurrentTypeWriter.Write(node, ")");
                    return true;
                }
            }
            return false;
        }
    }
}
