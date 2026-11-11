using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like p-q where p and q are pointers
    //Also handles (int)p - (int)q where p and q are pointers
    sealed class PointerSubtractPointerToIntegerSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.SubtractExpression))
            {
                var left = node.Left;
                var right = node.Right;
                var leftOperandType = visitor.Global.TryGetTypeSymbol(left, visitor);
                var rightOperandType = visitor.Global.TryGetTypeSymbol(right, visitor);
                ITypeSymbol? castNumericType = null;
                if (left.IsKind(SyntaxKind.CastExpression) && left is CastExpressionSyntax leftCast)
                {
                    castNumericType = visitor.Global.GetTypeSymbol(leftCast.Type, visitor);
                    if (castNumericType.IsNumericType())
                    {
                        leftOperandType = visitor.Global.TryGetTypeSymbol(leftCast.Expression, visitor);
                        left = leftCast.Expression;
                    }
                    else castNumericType = null;
                }
                if (right.IsKind(SyntaxKind.CastExpression) && right is CastExpressionSyntax rightCast)
                {
                    castNumericType = visitor.Global.GetTypeSymbol(rightCast.Type, visitor);
                    if (castNumericType.IsNumericType())
                    {
                        rightOperandType = visitor.Global.TryGetTypeSymbol(rightCast.Expression, visitor);
                        right = rightCast.Expression;
                    }
                    else castNumericType = null;
                }
                if ((leftOperandType?.IsPointer(out var leftPointedType) ?? false) && (rightOperandType?.IsPointer(out var rightPointedType) ?? false))
                {
                    if (castNumericType != null)
                    {
                        Debug.Assert(castNumericType.IsNumericType());
                        visitor.CurrentTypeWriter.Write(node, "Number(");
                    }
                    visitor.WritePointerSubtration(node, left, right, byteOffset: castNumericType != null);
                    if (castNumericType != null)
                    {
                        visitor.CurrentTypeWriter.Write(node, ")");
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
