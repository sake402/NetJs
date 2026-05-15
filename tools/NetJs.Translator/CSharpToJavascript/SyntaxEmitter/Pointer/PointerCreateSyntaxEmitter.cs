using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like &pointed or &pointed[2] to create a pointer
    sealed class PointerCreateSyntaxEmitter : SyntaxEmitter<PrefixUnaryExpressionSyntax>
    {
        public override bool TryEmit(PrefixUnaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddressOfExpression))
            {
                if (node.Operand.IsKind(SyntaxKind.ElementAccessExpression)) //&pointer[n] is a pointer addition
                {
                    var element = (ElementAccessExpressionSyntax)node.Operand;
                    var pointer = element.Expression;
                    var pointerType = visitor.Global.GetTypeSymbol(pointer, visitor);
                    if (pointerType.IsPointer(out _))
                    {
                        var increment = element.ArgumentList.Arguments[0];
                        visitor.WritePointerAdvance(node, pointer, increment);
                        return true;
                    }
                }
                var operandSymbol = visitor.Global.GetSymbol(node.Operand, visitor);// ResolveSymbol(visitor.GetExpressionReturnSymbol(node.Operand), visitor);
                var operandRefKind = operandSymbol.GetRefKind();
                //Ref and Pointer uses the same object for abstraction
                //If it is already a ref, no need to create another pointer
                if (operandRefKind != null && operandRefKind != RefKind.None)
                {
                    visitor.Visit(node.Operand);
                }
                else
                {
                    visitor.WriteCreateRef(node, node.Operand, visitor.Global.GetTypeSymbol(operandSymbol));
                }
                return true;
            }
            return false;
        }
    }
}
