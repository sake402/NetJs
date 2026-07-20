using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Indexer
{
    //Handle indexer get_Item eg foo.Item[1]
    sealed class IndexerGetItemSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
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
                    CodeNode cExpression = expression;
                    if (conditionalExpression != null && visitor.ConditionalAccessUseIfNotNull(conditionalExpression, out _))
                    {
                        cExpression = new CodeNode(() =>
                        {
                            visitor.CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                        });
                    }
                    var bestGetIndexer = visitor.GetGetIndexer(node is ElementAccessExpressionSyntax ? (ElementAccessExpressionSyntax)node : (ElementBindingExpressionSyntax)node);
                    if (bestGetIndexer != null)
                    {
                        bool isExtern = bestGetIndexer.IsExtern || visitor.Global.HasAttribute(bestGetIndexer, typeof(ExternalAttribute).FullName!, visitor, false, out _) ||
                             (bestGetIndexer.AssociatedSymbol?.IsExtern ?? false) || (bestGetIndexer.AssociatedSymbol != null && visitor.Global.HasAttribute(bestGetIndexer.AssociatedSymbol, typeof(ExternalAttribute).FullName!, visitor, false, out _));
                        bool hasTemplate = bestGetIndexer.GetTemplateAttribute(visitor.Global, visitor) != null;
                        if (!isExtern || hasTemplate)
                        {
                            visitor.WriteMethodInvocation(node, bestGetIndexer, null, arguments.Select(a => new CodeNode(a)), cExpression, lhsSymbol, null, false);
                            if (bestGetIndexer.RefKind != RefKind.None)
                            {
                                if (node.Parent.IsKind(SyntaxKind.EqualsEqualsToken) ||
                                    node.Parent.IsKind(SyntaxKind.NotEqualsExpression) ||
                                    node.Parent.IsKind(SyntaxKind.PostIncrementExpression) ||
                                    node.Parent.IsKind(SyntaxKind.PostDecrementExpression) ||
                                    node.Parent.IsKind(SyntaxKind.SubtractExpression) ||
                                    node.Parent.IsKind(SyntaxKind.AddExpression) ||
                                    node.Parent.IsKind(SyntaxKind.MultiplyExpression) ||
                                    node.Parent.IsKind(SyntaxKind.DivideExpression) ||
                                    node.Parent.IsKind(SyntaxKind.IfStatement) ||
                                    node.Parent.IsKind(SyntaxKind.WhileStatement))
                                {
                                    visitor.TryDereference(node);
                                }
                            }
                            return true;
                        }
                    }
                    //var targetType = target?.GetTypeSymbol();
                    //if (targetType != null)
                    //{
                    //    var propertyIndexers = targetType.GetMembers("get_Item", visitor.Global).Where(e => e is IMethodSymbol p && p.Parameters.Count() == arguments.Count).Cast<IMethodSymbol>().ToList();
                    //    //var propertyIndexers = nt.GetMembers("get_Item", _global).Where(e => e is IPropertySymbol p && p.IsIndexer && p.Parameters.Count() == node.ArgumentList.Arguments.Count && p.GetMethod != null).Cast<IPropertySymbol>().ToList();
                    //    var bestIndexer = visitor.GetBestOverloadMethod(targetType, propertyIndexers, null, arguments, null, out _);
                    //    if (bestIndexer != null)
                    //    {
                    //        bool isExtern = bestIndexer.IsExtern || visitor.Global.HasAttribute(bestIndexer, typeof(ExternalAttribute).FullName!, visitor, false, out _) ||
                    //             (bestIndexer.AssociatedSymbol?.IsExtern ?? false) || (bestIndexer.AssociatedSymbol != null && visitor.Global.HasAttribute(bestIndexer.AssociatedSymbol, typeof(ExternalAttribute).FullName!, visitor, false, out _));
                    //        bool hasTemplate = bestIndexer.GetTemplateAttribute(visitor.Global) != null;
                    //        if (!isExtern || hasTemplate)
                    //        {
                    //            visitor.WriteMethodInvocation(node, bestIndexer, null, arguments.Select(a => new CodeNode(a)), cExpression, target, null, false);
                    //            if (bestIndexer.RefKind != RefKind.None)
                    //            {
                    //                if (node.Parent.IsKind(SyntaxKind.EqualsEqualsToken) ||
                    //                    node.Parent.IsKind(SyntaxKind.NotEqualsExpression) ||
                    //                    node.Parent.IsKind(SyntaxKind.PostIncrementExpression) ||
                    //                    node.Parent.IsKind(SyntaxKind.PostDecrementExpression) ||
                    //                    node.Parent.IsKind(SyntaxKind.SubtractExpression) ||
                    //                    node.Parent.IsKind(SyntaxKind.AddExpression) ||
                    //                    node.Parent.IsKind(SyntaxKind.MultiplyExpression) ||
                    //                    node.Parent.IsKind(SyntaxKind.DivideExpression) ||
                    //                    node.Parent.IsKind(SyntaxKind.IfStatement) ||
                    //                    node.Parent.IsKind(SyntaxKind.WhileStatement))
                    //                {
                    //                    visitor.TryDereference(node);
                    //                }
                    //            }
                    //            return true;
                    //        }
                    //    }
                    //}
                }
            }
            return false;
        }
    }
}
