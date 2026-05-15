using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    sealed class PointerMemberAccessSyntaxEmitter : SyntaxEmitter<MemberAccessExpressionSyntax>
    {
        public override bool TryEmit(MemberAccessExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.PointerMemberAccessExpression))
            {
                visitor.Visit(node.Expression);
                visitor.TryDereference(node);
                visitor.CurrentTypeWriter.Write(node, ".");
                visitor.Visit(node.Name);
                return true;
            }
            return false;
        }
    }
}
