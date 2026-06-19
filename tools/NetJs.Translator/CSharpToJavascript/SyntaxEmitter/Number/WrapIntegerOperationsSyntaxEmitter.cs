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
                if (lhsType != null &&
                    rhsType != null &&
                    lhsType.IsIntegerNumericType() &&
                    rhsType.IsIntegerNumericType())
                {
                    foreach (var sm in visitor.SemanticModels)
                    {
                        if (node.SyntaxTree == sm.SyntaxTree)
                        {
                            if (!ImplicitConversionSyntaxEmitter.NumberImplicitlyConvertsToLong(node, sm, visitor, null) &&
                                !ImplicitConversionSyntaxEmitter.NumberImplicitlyConvertsToLong(node.Left, sm, visitor, null) &&
                                !ImplicitConversionSyntaxEmitter.NumberImplicitlyConvertsToLong(node.Right, sm, visitor, null))
                            {
                                bool isChecked = visitor.Global.Evaluate("checked") != null;
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
                                var isSignedResult = leftSigned || rightSigned || (!leftSigned && !rightSigned && node.IsKind(SyntaxKind.SubtractExpression));
                                var leftRank = lhsType.GetNumericRangeRank();
                                var rightRank = rhsType.GetNumericRangeRank();
                                var int32Rank = visitor.Global.SystemInt32.GetNumericRangeRank();
                                //if both precision are less than int, no math operation on them can exceed int, no need to | 0
                                if (isSignedResult && leftRank < int32Rank && rightRank < int32Rank)
                                    return false;
                                if (!isChecked)
                                {
                                    visitor.CurrentTypeWriter.Write(node, "((");
                                    visitor.Visit(node.Left);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.CurrentTypeWriter.Write(node, node.OperatorToken.Text);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.Visit(node.Right);
                                    visitor.CurrentTypeWriter.Write(node, ")");
                                    if (isSignedResult)
                                    {
                                        visitor.CurrentTypeWriter.Write(node, " | 0)");
                                    }
                                    else
                                    {
                                        visitor.CurrentTypeWriter.Write(node, " >>> 0)");
                                    }
                                }
                                else
                                {
                                    visitor.CurrentTypeWriter.Write(node, visitor.Global.GlobalName);
                                    visitor.CurrentTypeWriter.Write(node, ".");
                                    visitor.CurrentTypeWriter.Write(node, Constants.IntegerChecked);
                                    visitor.CurrentTypeWriter.Write(node, "(");
                                    visitor.Visit(node.Left);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.CurrentTypeWriter.Write(node, node.OperatorToken.Text);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.Visit(node.Right);
                                    visitor.CurrentTypeWriter.Write(node, ", ");
                                    visitor.CurrentTypeWriter.Write(node, isSignedResult ? "1" : "0");
                                    visitor.CurrentTypeWriter.Write(node, ")");
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
