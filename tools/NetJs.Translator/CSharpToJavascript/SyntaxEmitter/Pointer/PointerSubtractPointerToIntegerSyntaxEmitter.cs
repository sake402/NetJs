using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like p-q where p and q are pointers
    sealed class PointerSubtractPointerToIntegerSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.SubtractExpression))
            {
                var leftOperandType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rightOperandType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if ((leftOperandType?.IsPointer(out var leftPointedType) ?? false) && (rightOperandType?.IsPointer(out var rightPointedType) ?? false))
                {
                    visitor.WritePointerSubtration(node, node.Left, node.Right);
                    return true;
                }
            }
            return false;
        }
    }
}
