using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Indexer
{
    /// <summary>
    /// Optimize span access of span[i] to span._reference.GetAt(i)
    /// Removes the temp variable that is created on span.get_Item(i)
    /// span.get_Item(i).$v becomes span._reference.GetAt(i)
    /// </summary>
    sealed class SpanGetItemSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.ElementAccessExpression) || node.IsKind(SyntaxKind.ElementBindingExpression))
            {
                var expression = (node as ElementAccessExpressionSyntax)?.Expression;
                ConditionalAccessExpressionSyntax? conditionalExpression = null;
                if (expression == null)
                {
                    conditionalExpression = node.FindClosestParent<ConditionalAccessExpressionSyntax>(isCandidate: e => e.WhenNotNull == node);
                    //conditionalExpression = node.FindClosestParent<ConditionalAccessExpressionSyntax>(isCandidate: e => e.WhenNotNull == node || (e.WhenNotNull is AssignmentExpressionSyntax ass && ass.Left == node));
                    if (conditionalExpression != null)
                        expression = conditionalExpression.Expression;
                }
                if (expression != null)
                {
                    var arguments = (node as ElementAccessExpressionSyntax)?.ArgumentList.Arguments ?? (node as ElementBindingExpressionSyntax)!.ArgumentList.Arguments;
                    var lhsSymbol = visitor.Global.GetSymbol(expression, visitor);
                    var type = visitor.Global.GetTypeSymbol(lhsSymbol);
                    if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, visitor.Global.SystemReadOnlySpan) || SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, visitor.Global.SystemSpan))
                    {
                        CodeNode cExpression = expression;
                        if (conditionalExpression != null && visitor.ConditionalAccessUseIfNotNull(conditionalExpression, out _))
                        {
                            cExpression = new CodeNode(() =>
                            {
                                visitor.CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                            });
                        }
                        var refGetAt = (IMethodSymbol)visitor.Global.SystemBaseRefOrPointer.GetMembers("GetAt").Single();
                        visitor.WriteMethodInvocation(node, refGetAt, null, [new CodeNode(() => {
                            visitor.Visit(arguments[0]);
                        })], new CodeNode(() =>
                        {
                            visitor.VisitNode(cExpression);
                            visitor.CurrentTypeWriter.Write(node, "._reference");
                        }), lhsSymbol, null);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
