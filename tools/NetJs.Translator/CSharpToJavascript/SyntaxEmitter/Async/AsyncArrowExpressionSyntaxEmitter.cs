using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    sealed class AsyncArrowExpressionSyntaxEmitter : SyntaxEmitter<ArrowExpressionClauseSyntax>
    {
        public override bool TryEmit(ArrowExpressionClauseSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var method = node.FindClosestParent<CSharpSyntaxNode>(e =>
                (e.IsKind(SyntaxKind.MethodDeclaration) && ((MethodDeclarationSyntax)e).Modifiers.IsAsync()) ||
                (e.IsKind(SyntaxKind.LocalFunctionStatement) && ((LocalFunctionStatementSyntax)e).Modifiers.IsAsync()) ||
                (e.IsKind(SyntaxKind.ConstructorDeclaration) && ((ConstructorDeclarationSyntax)e).Modifiers.IsAsync()) ||
                (e.IsKind(SyntaxKind.IndexerDeclaration) && ((IndexerDeclarationSyntax)e).Modifiers.IsAsync()) ||
                (e.IsKind(SyntaxKind.ConversionOperatorDeclaration) && ((ConversionOperatorDeclarationSyntax)e).Modifiers.IsAsync()) ||
                (e.IsKind(SyntaxKind.SimpleLambdaExpression) && ((SimpleLambdaExpressionSyntax)e).Modifiers.IsAsync()) ||
                (e.IsKind(SyntaxKind.ParenthesizedLambdaExpression) && ((ParenthesizedLambdaExpressionSyntax)e).Modifiers.IsAsync()) ||
                (e.IsKind(SyntaxKind.AnonymousMethodExpression) && ((AnonymousMethodExpressionSyntax)e).Modifiers.IsAsync()) ||
                (e.IsKind(SyntaxKind.PropertyDeclaration) && ((PropertyDeclarationSyntax)e).Modifiers.IsAsync()), continueParent: (e) =>
                {
                    if (e.IsKind(SyntaxKind.Argument))
                        return false;
                    return true;
                });
            if (method != null)
            {
                visitor.CurrentTypeWriter.Write(node, "return ", true);
                visitor.WriteMethodInvocation(node, "NetJs.AsyncResolver.Result", arguments: [new CodeNode(() => {
                    visitor.CurrentTypeWriter.Write(node, "$asnycContext");
                }), node.Expression??new CodeNode(() => {

                })]);
                visitor.CurrentTypeWriter.WriteLine(node, ";");
                return true;
            }
            return false;
        }
    }
}
