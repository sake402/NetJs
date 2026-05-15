using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Delegate
{
    internal class MethodDelegateCreateSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        Stack<CSharpSyntaxNode> _processing = new Stack<CSharpSyntaxNode>();
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (_processing.TryPeek(out var top) && top == node)
                return false;
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression) ||
                node.IsKind(SyntaxKind.IdentifierName) ||
                node.IsKind(SyntaxKind.SimpleLambdaExpression) ||
                node.IsKind(SyntaxKind.AnonymousMethodExpression) ||
                node.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                foreach (var sm in visitor.SemanticModels)
                {
                    if (node.SyntaxTree == sm.SyntaxTree)
                    {
                        var type = visitor.Global.GetSymbol(node, visitor);
                        if (!visitor.Global.IsNativeFunction(type))
                        {
                            if (node.Parent.IsKind(SyntaxKind.Argument))
                            {
                                var argOperation = sm.GetOperation(node.Parent) as IArgumentOperation;
                                if (argOperation?.Parameter != null)
                                {
                                    if (visitor.Global.IsNativeFunction(argOperation.Parameter))
                                    {
                                        return false;
                                    }
                                }
                            }
                            var conversion = sm.GetConversion(node);
                            var operation = sm.GetOperation(node);
                            var delegateCreate = operation?.Parent as IDelegateCreationOperation;
                            if (conversion.Exists &&
                                conversion.IsImplicit &&
                                (conversion.IsMethodGroup || conversion.IsAnonymousFunction) &&
                                (
                                (operation is IMethodReferenceOperation mconversion && delegateCreate != null) ||
                                (operation is IAnonymousFunctionOperation afconversion && delegateCreate != null)
                                ))
                            {
                                if (!visitor.Global.IsNativeFunction(delegateCreate.Type!))
                                {
                                    var _this = (node as MemberAccessExpressionSyntax)?.Expression;
                                    var methodGroup = node;// as MemberAccessExpressionSyntax ?? node as IdentifierNameSyntax; 
                                    if (_this == null)
                                    {
                                        var methodSymbol = (IMethodSymbol)visitor.Global.GetSymbol(methodGroup, visitor);
                                        if (!methodSymbol.IsStatic)
                                        {
                                            _this = SyntaxFactory.ThisExpression();
                                        }
                                        else
                                        {
                                            _this = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                                        }
                                    }
                                    _processing.Push(node);
                                    try
                                    {
                                        var metadata = visitor.Global.GetRequiredMetadata(delegateCreate.Type!);
                                        visitor.CurrentTypeWriter.Write(node, "new ");
                                        bool isGeneric = false;
                                        if (delegateCreate.Type is INamedTypeSymbol nt && nt.Arity > 0)
                                        {
                                            visitor.CurrentTypeWriter.Write(node, "(");
                                            isGeneric = true;
                                        }
                                        visitor.CurrentTypeWriter.Write(node, metadata.InvocationName!);
                                        if (isGeneric)
                                        {
                                            visitor.CurrentTypeWriter.Write(node, ")");
                                        }
                                        visitor.CurrentTypeWriter.Write(node, "().$ctor(");
                                        visitor.Visit(_this);
                                        visitor.CurrentTypeWriter.Write(node, ", ");
                                        visitor.Visit(methodGroup);
                                        visitor.CurrentTypeWriter.Write(node, ")");
                                        return true;
                                    }
                                    finally
                                    {
                                        _processing.Pop();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
