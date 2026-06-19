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
        static string[] MathBinaryOperators = new string[]
        {
            "+", "-", "*", "/", "%", "==", "!="//, ">>", "<<"
        };

        bool TryWriteMathBinaryExpression(CSharpSyntaxNode node, string _operator, ExpressionSyntax left, ExpressionSyntax right)
        {
            return false;
            if (MathBinaryOperators.Contains(_operator))
            {
                var leftType = _global.TryGetTypeSymbol(left, this);
                var rightType = _global.TryGetTypeSymbol(right, this);
                if (leftType != null && rightType != null && leftType.IsNumericType() && rightType.IsNumericType())
                {
                    if (TryInvokeMethodOperator(node, _operator, leftType, left, [left, right]))
                        return true;
                    if ((leftType.Equals(rightType, SymbolEqualityComparer.Default)) || //if both math lhs and rhs are the same, nothing special to do
                        (leftType.IsNumberNumericType() && rightType.IsNumberNumericType()) // if we handle both types in native javascript, nothing special to do
                        )
                    {
                        Visit(left);
                        CurrentTypeWriter.Write(node, " ");
                        CurrentTypeWriter.Write(node, _operator);
                        CurrentTypeWriter.Write(node, " ");
                        Visit(right);
                        return true;
                    }
                    //lhs and rhs are not the same type, see if we need to cast before invoking the operator
                    //find the one that has highest precision
                    int lhsRank = leftType.GetNumericRangeRank();
                    int rhsRank = rightType.GetNumericRangeRank();
                    ITypeSymbol higherPrecision = rhsRank > lhsRank ? rightType : leftType;
                    ITypeSymbol lowerPrecision = rhsRank > lhsRank ? leftType : rightType;
                    //Generate a cast for the one from lower precision to high precision
                    CastExpressionSyntax Cast(ExpressionSyntax expression, ITypeSymbol type)
                    {
                        var typeSyntax = SyntaxFactory.ParseTypeName(type.Name);
                        var casted = SyntaxFactory.CastExpression(typeSyntax, expression);
                        return casted;
                    }
                    ExpressionSyntax newLeft = left, newRight = right;
                    bool leftCasted = false;
                    bool rightCasted = false;
                    if (lhsRank < rhsRank)
                    {
                        if (TryInvokeMethodOperator(node, ImplicitOperatorName, higherPrecision, null, [left]))
                        {
                            CurrentTypeWriter.Write(node, " ");
                            CurrentTypeWriter.Write(node, _operator);
                            CurrentTypeWriter.Write(node, " ");
                            Visit(right);
                            return true;
                        }
                        if (left is LiteralExpressionSyntax) //no point casting a literal expression at runtime
                            return false;
                        leftCasted = true;
                        newLeft = Cast(left, higherPrecision);
                    }
                    else
                    {
                        if (TryInvokeMethodOperator(node, ImplicitOperatorName, higherPrecision, null, [right], () =>
                        {
                            Visit(left);
                            CurrentTypeWriter.Write(node, " ");
                            CurrentTypeWriter.Write(node, _operator);
                            CurrentTypeWriter.Write(node, " ");
                        }))
                        {
                            return true;
                        }
                        if (right is LiteralExpressionSyntax) //no point casting a literal expression at runtime
                            return false;
                        rightCasted = true;
                        newRight = Cast(right, higherPrecision);
                    }
                    //Create a new binary expression from the left right
                    var kind = _operator switch
                    {
                        "+" => SyntaxKind.AddExpression,
                        "-" => SyntaxKind.SubtractExpression,
                        "*" => SyntaxKind.MultiplyExpression,
                        "/" => SyntaxKind.DivideExpression,
                        "%" => SyntaxKind.ModuloExpression,
                        "==" => SyntaxKind.EqualsExpression,
                        "!=" => SyntaxKind.NotEqualsExpression,
                        ">>" => SyntaxKind.RightShiftExpression,
                        "<<" => SyntaxKind.LeftShiftExpression,
                        _ => SyntaxKind.None
                    };
                    var newNode = SyntaxFactory.BinaryExpression(kind, newLeft, newRight);
                    IDisposable? leftDispose = null;
                    IDisposable? rightDispose = null;
                    if (leftCasted && newNode.Left is CastExpressionSyntax castL)
                        leftDispose = AssociateSyntaxFactoryNode(left, castL.Expression);
                    else
                        leftDispose = AssociateSyntaxFactoryNode(left, newNode.Left);
                    if (rightCasted && newNode.Right is CastExpressionSyntax castR)
                        rightDispose = AssociateSyntaxFactoryNode(right, castR.Expression);
                    else
                        rightDispose = AssociateSyntaxFactoryNode(right, newNode.Right);
                    Visit(newNode);
                    leftDispose?.Dispose();
                    rightDispose?.Dispose();
                    return true;
                }
            }
            return false;
        }
    }
}
