using LivingThing.Core.Frameworks.Common.OneOf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    sealed class AsyncReturnSyntaxEmitter : SyntaxEmitter<ReturnStatementSyntax>
    {
        public override bool TryEmit(ReturnStatementSyntax node, TranslatorSyntaxVisitor visitor)
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
