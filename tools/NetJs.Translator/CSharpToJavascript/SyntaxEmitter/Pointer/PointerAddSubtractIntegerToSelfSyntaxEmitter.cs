using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like p+=2, p-=2 where p is a pointer
    sealed class PointerAddSubtractIntegerToSelfSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddAssignmentExpression) || node.IsKind(SyntaxKind.SubtractAssignmentExpression))
            {
                var leftOperandType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rightOperandType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                //var operandType = visitor.Global.ResolveSymbol(visitor.GetExpressionReturnSymbol(node.Left), visitor)!.GetTypeSymbol();
                if ((leftOperandType?.IsPointer(out var pointedType) ?? false) && (rightOperandType?.IsNumericType() ?? false))
                {
                    visitor.WritePointerSelfAdvance(node, node.Left, node.Right, subtract: node.IsKind(SyntaxKind.SubtractAssignmentExpression));
                    return true;
                }
            }
            return false;
        }
    }
}
