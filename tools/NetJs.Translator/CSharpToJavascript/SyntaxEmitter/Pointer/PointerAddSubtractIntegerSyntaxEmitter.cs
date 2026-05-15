using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like p+2, p-2 where p is a pointer
    sealed class PointerAddSubtractIntegerSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddExpression) || node.IsKind(SyntaxKind.SubtractExpression))
            {
                var leftOperandType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rightOperandType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if ((leftOperandType?.IsPointer(out var pointedType) ?? false) && (rightOperandType?.IsNumericType() ?? false))
                {
                    visitor.WritePointerAdvance(node, node.Left, node.Right, subtract: node.IsKind(SyntaxKind.SubtractExpression));
                    return true;
                }
                //operandType = visitor.Global.GetTypeSymbol(node.Right, visitor).GetTypeSymbol();
                //if (operandType.IsPointer(out pointerType))
                //{
                //    visitor.WritePointerAdvance(node, node.Right, node.Left);
                //    return true;
                //}
            }
            return false;
        }
    }
}
