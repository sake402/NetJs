using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    //Handles a+b defined in an operator that would be recursively calling itself
    sealed class RecursiveOperatorSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.OperatorToken.ValueText == "+" ||
                node.OperatorToken.ValueText == "-" ||
                node.OperatorToken.ValueText == "/" ||
                node.OperatorToken.ValueText == "*" ||
                node.OperatorToken.ValueText == "==" ||
                node.OperatorToken.ValueText == "!=")
            {
                var moperator = node.FindClosestParent<OperatorDeclarationSyntax>();
                if (moperator != null && node.OperatorToken.ValueText == moperator.OperatorToken.ValueText)
                {
                    var operatorSymbol = visitor.Global.TryGetSymbol(moperator, visitor);
                    var leftType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                    var rightType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                    if (operatorSymbol != null && 
                        leftType != null && 
                        rightType != null && 
                        (SymbolEqualityComparer.Default.Equals(operatorSymbol.ContainingType, leftType) ||
                        SymbolEqualityComparer.Default.Equals(operatorSymbol.ContainingType, rightType)))
                    {
                        visitor.Visit(node.Left);
                        visitor.CurrentTypeWriter.Write(node, " ");
                        visitor.CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);
                        visitor.CurrentTypeWriter.Write(node, " ");
                        visitor.Visit(node.Right);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
