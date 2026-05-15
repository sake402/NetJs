using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Indexer
{
    //Handle likes of CustomObject[key] = value, CustomObject?[key] = value, Dictionary[key] = value, Span[index] = value, 
    sealed class IndexerSetItemSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            //if (node.IsKind(SyntaxKind.SimpleAssignmentExpression))
            //{
            //AssignmentExpressionSyntax node = (AssignmentExpressionSyntax)node;
            bool IsEqualToken()
            {
                return node.OperatorToken.IsKind(SyntaxKind.EqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.MinusEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.PercentEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.AmpersandEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.AsteriskEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.SlashEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.BarEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.CaretEqualsToken);
            }
            if ((node.Left.IsKind(SyntaxKind.ElementAccessExpression) || node.Left.IsKind(SyntaxKind.ElementBindingExpression)) && IsEqualToken())
            {
                var expression = (node.Left as ElementAccessExpressionSyntax)?.Expression;
                ConditionalAccessExpressionSyntax? conditionalExpression = null;
                if (expression == null)
                {
                    conditionalExpression = node.FindClosestParent(isCandidate: (ConditionalAccessExpressionSyntax e) => e.WhenNotNull == node);
                    //conditionalExpression = node.FindClosestParent<ConditionalAccessExpressionSyntax>(isCandidate: e => e.WhenNotNull == node || (e.WhenNotNull is AssignmentExpressionSyntax ass && ass.Left == node));
                    if (conditionalExpression != null)
                        expression = conditionalExpression.Expression;
                }
                if (expression != null)
                {
                    var arguments = (node.Left as ElementAccessExpressionSyntax)?.ArgumentList.Arguments ?? (node.Left as ElementBindingExpressionSyntax)!.ArgumentList.Arguments;
                    var lhsSymbol = visitor.Global.GetSymbol(expression, visitor);

                    var rhsType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);// GetExpressionBoundTarget(node.Right).TypeSyntaxOrSymbol as ISymbol;
                    //if (rhsType == null)
                    //{
                    //    rhsType = visitor.Global.ResolveSymbol(visitor.GetExpressionReturnSymbol(node.Right), visitor/*, out _, out _*/);
                    //}
                    var lhsType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);// GetExpressionBoundTarget(node.Left).TypeSyntaxOrSymbol as ISymbol;
                    //if (lhsType == null)
                    //{
                    //    lhsType = visitor.Global.ResolveSymbol(visitor.GetExpressionReturnSymbol(node.Left), visitor/*, out _, out _*/);
                    //}
                    var assignmentType = rhsType ?? lhsType;

                    //var sourceDeclaration = visitor.GetExpressionReturnSymbol(expression);
                    //var sourceType = visitor.Global.ResolveSymbol(sourceDeclaration, visitor/*, out _, out _*/)?.GetTypeSymbol();
                    //if (sourceType != null)
                    {
                        CodeNode cExpression = expression;
                        if (conditionalExpression != null && visitor.ConditionalAccessUseIfNotNull(conditionalExpression, out _))
                        {
                            cExpression = new CodeNode(() =>
                            {
                                visitor.CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                            });
                        }
                        var bestIndexer = visitor.GetSetIndexer(node.Left is ElementAccessExpressionSyntax ? (ElementAccessExpressionSyntax)node.Left : (ElementBindingExpressionSyntax)node.Left, node.Right);
                        if (bestIndexer != null && bestIndexer.IsInvokable(visitor.Global))
                        {
                            var valueParameter = bestIndexer.Parameters.Last();
                            var box = true;
                            if (valueParameter != null && visitor.Global.HasAttribute(valueParameter, typeof(BoxAttribute).FullName, visitor, false, out var arg))
                            {
                                box = (bool)arg[0];
                            }
                            visitor.WriteMethodInvocation(node, bestIndexer, null, arguments.Select(a => new CodeNode(a)), cExpression, lhsSymbol, null, false, suffixArguments: (Action)(() =>
                            {
                                if (!node.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                                {
                                    visitor.Visit(node.Left);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.CurrentTypeWriter.Write(node, node.OperatorToken.ValueText.Substring(0, node.OperatorToken.ValueText.Length - 1));
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                }
                                visitor.WriteVariableAssignment(node, null, lhsType, null, new CodeNode(node.Right), rhsType, enableBoxing: box);
                                //visitor.Visit(node.Right);
                            }));
                            return true;
                        }

                        //var propertyIndexers = sourceType.GetMembers("set_Item", visitor.Global).Where(e => e is IMethodSymbol p && p.Parameters.Count() == arguments.Count + 1).Cast<IMethodSymbol>().ToList();
                        ////var propertyIndexers = nt.GetMembers("set_Item", _global).Where(e => e is IPropertySymbol p && p.IsIndexer && p.Parameters.Count() == elementAccess.ArgumentList.Arguments.Count && p.SetMethod != null).Cast<IPropertySymbol>().ToList();
                        //var bestIndexer = visitor.GetBestOverloadMethod(sourceType, propertyIndexers, null, arguments, assignment.Right, out _);
                        //if (bestIndexer != null && bestIndexer.IsInvokable(visitor.Global))
                        //{
                        //    var valueParameter = bestIndexer.Parameters.Last();
                        //    var box = true;
                        //    if (valueParameter != null && visitor.Global.HasAttribute(valueParameter, typeof(BoxAttribute).FullName, visitor, false, out var arg))
                        //    {
                        //        box = (bool)arg[0];
                        //    }
                        //    visitor.WriteMethodInvocation(node, bestIndexer, null, arguments.Select(a => new CodeNode(a)), cExpression, assignmentType, null, false, suffixArguments: (Action)(() =>
                        //    {
                        //        if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                        //        {
                        //            visitor.Visit(assignment.Left);
                        //            visitor.CurrentTypeWriter.Write(node, " ");
                        //            visitor.CurrentTypeWriter.Write(node, assignment.OperatorToken.ValueText.Substring(0, assignment.OperatorToken.ValueText.Length - 1));
                        //            visitor.CurrentTypeWriter.Write(node, " ");
                        //        }
                        //        visitor.WriteVariableAssignment(node, null, lhsType, null, new CodeNode(assignment.Right), rhsType, enableBoxing: box);
                        //        //visitor.Visit(node.Right);
                        //    }));
                        //    return true;
                        //}
                    }
                    //check if we have a get_Item that return a ref type in the source
                    //propertyIndexers = sourceType.GetMembers("get_Item", _global).Where(e => e is IMethodSymbol p && p.Parameters.Count() == elementAccess.ArgumentList.Arguments.Count).Cast<IMethodSymbol>().ToList();
                    //bestIndexer = GetBestOverloadMethod(sourceType, propertyIndexers, null, elementAccess.ArgumentList.Arguments, node.Right, out _);
                    //if (bestIndexer != null && bestIndexer.CanInvoke(_global))
                    //{
                    //    if (bestIndexer.ReturnsByRef)
                    //    {
                    //        Visit(node.Left);
                    //        Writer.Write(node, ".");
                    //        Writer.Write(node, Constants.RefValueName);
                    //        if (node.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                    //            Writer.Write(node, $" {node.OperatorToken.ValueText} ");
                    //        else
                    //        {
                    //            //a+=b becomes  a.$v = a.$v+b;
                    //            Writer.Write(node, $" = ");
                    //            Visit(node.Left);
                    //            Writer.Write(node, ".");
                    //            Writer.Write(node, Constants.RefValueName);
                    //            Writer.Write(node, " ");
                    //            Writer.Write(node, node.OperatorToken.ValueText.Substring(0, node.OperatorToken.ValueText.Length - 1));
                    //            Writer.Write(node, " ");
                    //        }
                    //        Visit(node.Right);
                    //        return;
                    //    }
                    //}
                }
            }
            //}
            return false;
        }
    }
}
