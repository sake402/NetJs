using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Delegate
{
    internal class MethodDelegateCreateSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node.ToString() == "(BindFormatter<T>)FormatEnumValueCore<T>")
            {

            }
            if (_processing.Value.TryPeek(out var top) && top == node)
                return false;
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression) ||
                node.IsKind(SyntaxKind.IdentifierName) ||
                node.IsKind(SyntaxKind.SimpleLambdaExpression) ||
                node.IsKind(SyntaxKind.AnonymousMethodExpression) ||
                node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) ||
                node.IsKind(SyntaxKind.CastExpression))
            {
                foreach (var sm in visitor.SemanticModels)
                {
                    if (node.SyntaxTree == sm.SyntaxTree)
                    {
                        var type = visitor.Global.TryGetSymbol(node, visitor);
                        if (type == null)
                            continue;
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
                            var delegateCreate = operation?.Parent as IDelegateCreationOperation ?? operation as IDelegateCreationOperation;
                            if (conversion.Exists &&
                                conversion.IsImplicit &&
                                (conversion.IsMethodGroup || conversion.IsAnonymousFunction || node.IsKind(SyntaxKind.CastExpression)) &&
                                (
                                (operation is IDelegateCreationOperation && delegateCreate != null) ||
                                (operation is IMethodReferenceOperation mconversion && delegateCreate != null) ||
                                (operation is IAnonymousFunctionOperation afconversion && delegateCreate != null)
                                ))
                            {
                                if (!visitor.Global.IsNativeFunction(delegateCreate.Type!))
                                {
                                    CodeNode _this = (node as MemberAccessExpressionSyntax)?.Expression;
                                    var methodGroup = node;// as MemberAccessExpressionSyntax ?? node as IdentifierNameSyntax; 
                                    if (methodGroup.IsKind(SyntaxKind.CastExpression) && methodGroup is CastExpressionSyntax cast)
                                        methodGroup = cast.Expression;
                                    var methodSymbol = (IMethodSymbol)visitor.Global.GetSymbol(methodGroup, visitor);
                                    if (_this == null)
                                    {
                                        if (!methodSymbol.IsStatic)
                                        {
                                            _this = new CodeNode(() => visitor.CurrentTypeWriter.Write(node, "this"));
                                        }
                                        else
                                        {
                                            //TODO: Instead of null, pass the static class prototype as target
                                            _this = new CodeNode(() => visitor.CurrentTypeWriter.Write(node, methodSymbol.ContainingType.ComputeOutputTypeName(visitor.Global)));
                                            //_this = new CodeNode(() => visitor.CurrentTypeWriter.Write(node, "null"));
                                        }
                                    }
                                    int popCount = 1;
                                    _processing.Value.Push(node);
                                    if (node.IsKind(SyntaxKind.CastExpression) && node is CastExpressionSyntax cast2) //a lambda casted to delegate? We are already creating the delegate based on the cast type, skip another creation for the lamda
                                    {
                                        _processing.Value.Push(cast2.Expression);
                                        popCount++;
                                        if (cast2.Expression.IsKind(SyntaxKind.ParenthesizedExpression) && cast2.Expression is ParenthesizedExpressionSyntax pl)
                                        {
                                            _processing.Value.Push(pl.Expression);
                                            popCount++;
                                        }
                                    }
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
                                        visitor.VisitNode(_this);
                                        visitor.CurrentTypeWriter.Write(node, ", ");
                                        if (methodGroup.IsKind(SyntaxKind.GenericName)) //a method like FormatEnumValueCore<T>
                                        {
                                            visitor.CurrentTypeWriter.Write(node, $"(...args) => ");
                                        }
                                        visitor.Visit(methodGroup);
                                        if (methodGroup.IsKind(SyntaxKind.GenericName)) //a method like FormatEnumValueCore<T>
                                        {
                                            visitor.CurrentTypeWriter.Write(node, "(");
                                            int ix = 0;
                                            foreach (var arg in ((GenericNameSyntax)methodGroup).TypeArgumentList.Arguments)
                                            {
                                                if (ix > 0)
                                                    visitor.CurrentTypeWriter.Write(node, ", ");
                                                visitor.Visit(arg);
                                                ix++;
                                            }
                                            if (ix > 0)
                                                visitor.CurrentTypeWriter.Write(node, ", ");
                                            visitor.CurrentTypeWriter.Write(node, " ...args)");
                                        }
                                        //A parameter that is a lamda block could have written newLine
                                        visitor.CurrentTypeWriter.TrimEnd();
                                        //An anonumous function should have its method metadata passed to the delegate it is converted to
                                        if (node.IsKind(SyntaxKind.SimpleLambdaExpression) ||
                                            node.IsKind(SyntaxKind.AnonymousMethodExpression) ||
                                            node.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
                                        {
                                            var methodModel = visitor.Global.Reflection.FromMethodSymbolAsJson(methodSymbol, visitor);
                                            visitor.CurrentTypeWriter.Write(node, ", ");
                                            visitor.CurrentTypeWriter.Write(node, methodModel);
                                        }
                                        visitor.CurrentTypeWriter.Write(node, ")");
                                        return true;
                                    }
                                    finally
                                    {
                                        while (popCount-- > 0)
                                            _processing.Value.Pop();
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
