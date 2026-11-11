using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref;
using NUglify.JavaScript.Visitors;
using System.Linq.Expressions;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Indexer
{
    /// <summary>
    /// Optimize span access of span[i] = value to span._reference.SetAt(value, i)
    /// Removes the temp variable that is created on span.get_Item(i)
    /// span.get_Item(i).$v = value becomes span._reference.SetAt(value, i)
    /// </summary>
    sealed class SpanSetItemSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        public static bool TryWrite(CSharpSyntaxNode node, CSharpSyntaxNode left, CodeNode right, TranslatorSyntaxVisitor visitor, bool isForceWrite = false)
        {
            if (RefTypeDereferenceOnAccessSyntaxEmitter.IsRefTypeToDereference(left, visitor, out var isWrite) && (isForceWrite || isWrite == true))
            {
                var expression = (left as ElementAccessExpressionSyntax)?.Expression;
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
                    var arguments = (left as ElementAccessExpressionSyntax)?.ArgumentList.Arguments ?? (left as ElementBindingExpressionSyntax)!.ArgumentList.Arguments;
                    if (arguments.Count == 1)
                    {
                        //var argType = visitor.Global.GetTypeSymbol(arguments[0].Expression, visitor);
                        //if (argType.IsNumericType())
                        {
                            var lhsSymbol = visitor.Global.GetSymbol(expression, visitor);
                            var type = visitor.Global.GetTypeSymbol(lhsSymbol);
                            if (
                                //SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, visitor.Global.SystemReadOnlySpan) ||
                                SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, visitor.Global.SystemSpan))
                            {
                                CodeNode cExpression = expression;
                                if (conditionalExpression != null && visitor.ConditionalAccessUseIfNotNull(conditionalExpression, out _))
                                {
                                    cExpression = new CodeNode(() =>
                                    {
                                        visitor.CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                                    });
                                }
                                var refSetAt = (IMethodSymbol)visitor.Global.SystemRefOrPointer.GetMembers("SetAt").Single();
                                visitor.WriteMethodInvocation(node, refSetAt, null, [right, new CodeNode(() => {
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
                }
            }
            return false;
        }

        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
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
                return TryWrite(node, node.Left, node.Right, visitor);
            }
            return false;
        }
    }
}