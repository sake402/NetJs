using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    //Handle this = value syntax
    sealed class ThisAssignmentSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            //The only time when c# allows this to be assigned is if it is a struct type.
            //We clone the rhs into this
            if (node.Left.IsKind(SyntaxKind.ThisExpression)/* is ThisExpressionSyntax*/&& node.OperatorToken.IsKind(SyntaxKind.EqualsToken))
            {
                visitor.Visit(node.Right);
                visitor.CurrentTypeWriter.Write(node, ".");
                visitor.CurrentTypeWriter.Write(node, Constants.Clone);
                visitor.CurrentTypeWriter.Write(node, "(this)");
                return true;
            }
            return false;
        }
    }
}
