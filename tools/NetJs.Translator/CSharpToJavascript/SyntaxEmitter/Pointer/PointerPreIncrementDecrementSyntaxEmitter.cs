using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like ++p, --p where p is a pointer
    sealed class PointerPreIncrementDecrementSyntaxEmitter : SyntaxEmitter<PrefixUnaryExpressionSyntax>
    {
        public override bool TryEmit(PrefixUnaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.PreIncrementExpression) || node.IsKind(SyntaxKind.PreDecrementExpression))
            {
                var operandType = visitor.Global.GetTypeSymbol(node.Operand, visitor);
                if (operandType.IsPointer(out var pointerType))
                {
                    //if the result of the expression is passed to other variables
                    if (node.Parent is AssignmentExpressionSyntax || 
                        node.Parent.IsKind(SyntaxKind.EqualsExpression) ||
                        node.Parent.IsKind(SyntaxKind.Argument) ||
                        node.Parent.IsKind(SyntaxKind.PointerIndirectionExpression))
                    {
                        visitor.WrapStatementsInExpression(node, () =>
                        {
                            visitor.WritePointerSelfAdvance(node, node.Operand, new CodeNode(() =>
                            {
                                visitor.CurrentTypeWriter.Write(node, node.IsKind(SyntaxKind.PreIncrementExpression) ? "1" : "-1");
                            }));
                            visitor.CurrentTypeWriter.WriteLine(node, ";");
                            visitor.CurrentTypeWriter.Write(node, "return ", true);
                            visitor.Visit(node.Operand);
                            visitor.CurrentTypeWriter.WriteLine(node, ";");
                        });
                    }
                    else
                    {
                        visitor.WritePointerSelfAdvance(node, node.Operand, new CodeNode(() =>
                        {
                            visitor.CurrentTypeWriter.Write(node, node.IsKind(SyntaxKind.PreIncrementExpression) ? "1" : "-1");
                        }));
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
