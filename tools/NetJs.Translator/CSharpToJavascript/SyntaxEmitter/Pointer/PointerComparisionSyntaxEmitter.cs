using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like p1>p2 where p1 and p2 are pointers
    sealed class PointerComparisionSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.LessThanExpression) ||
                node.IsKind(SyntaxKind.LessThanOrEqualExpression) ||
                node.IsKind(SyntaxKind.GreaterThanExpression) ||
                node.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
                node.IsKind(SyntaxKind.EqualsExpression) ||
                node.IsKind(SyntaxKind.NotEqualsExpression)
                )
            {
                if (!node.Left.IsKind(SyntaxKind.NullLiteralExpression) && !node.Right.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    var leftOperandType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                    var rightOperandType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                    if ((leftOperandType?.IsPointer(out _) ?? false) && (rightOperandType?.IsPointer(out _) ?? false))
                    {
                        visitor.WriteMethodInvocation(node, "NetJs.RefOrPointer.Compare",/* classGenericTypes: [visitor.Global.SystemObject],*/ arguments: [node.Left, node.Right]);
                        //visitor.Visit(node.Left);
                        //visitor.CurrentTypeWriter.Write(node, ".Compare(");
                        //visitor.Visit(node.Right);
                        //visitor.CurrentTypeWriter.Write(node, ") ");
                        visitor.CurrentTypeWriter.Write(node, " ");
                        visitor.CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);
                        visitor.CurrentTypeWriter.Write(node, " 0");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
