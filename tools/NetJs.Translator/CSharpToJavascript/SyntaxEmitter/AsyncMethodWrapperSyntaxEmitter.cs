using LivingThing.Core.Frameworks.Common.OneOf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    sealed class AsyncMethodWrapperSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public static bool IsAsyncWrapperCandidate(TranslatorSyntaxVisitor visitor, SyntaxNode node, out OneOf<ITypeSymbol, TypeSyntax> taskType)
        {
            taskType = default;
            if (visitor.HasYield(node))
            {
                return false;
            }
            var isAsync = false;
            //TypeSyntax? returnType = null;
            if (node.IsKind(SyntaxKind.MethodDeclaration))
            {
                isAsync = ((MethodDeclarationSyntax)node).Modifiers.IsAsync();
                taskType = ((MethodDeclarationSyntax)node).ReturnType;
            }
            else if (node.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                isAsync = ((LocalFunctionStatementSyntax)node).Modifiers.IsAsync();
                taskType = ((LocalFunctionStatementSyntax)node).ReturnType;
            }
            else if (node.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                isAsync = ((AnonymousFunctionExpressionSyntax)node).Modifiers.IsAsync();
                var t = visitor.Global.GetTypeSymbol(node, visitor);
                taskType = OneOf<ITypeSymbol, TypeSyntax>.FromT0(t);
            }
            else if (node.IsKind(SyntaxKind.SimpleLambdaExpression))
            {
                isAsync = ((LambdaExpressionSyntax)node).Modifiers.IsAsync();
                var t = visitor.Global.GetTypeSymbol(node, visitor);
                taskType = OneOf<ITypeSymbol, TypeSyntax>.FromT0(t);
            }
            else if (node.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                isAsync = ((ParenthesizedLambdaExpressionSyntax)node).Modifiers.IsAsync();
                var t = visitor.Global.GetTypeSymbol(node, visitor);
                taskType = OneOf<ITypeSymbol, TypeSyntax>.FromT0(t);
            }
            //taskType = returnType;
            return isAsync;
        }
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.Block) || node.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                if (visitor.HasYield(node))
                    return false;
                if (_processing.Value.Contains(node))
                {
                    return false;
                }
                if (node.Parent != null && IsAsyncWrapperCandidate(visitor, node.Parent, out var returnType) && (returnType.IsT0 || returnType.IsT1))
                {
                    _processing.Value.Push(node);
                    try
                    {
                        var taskSymbol = returnType.IsT0 ? returnType.AsT0 : visitor.Global.GetTypeSymbol(returnType.AsT1, visitor);
                        var returnSymbol = taskSymbol is INamedTypeSymbol nt && nt.Arity == 1 ? nt.TypeArguments[0] : visitor.Global.SystemVoid;
                        if (node.IsKind(SyntaxKind.Block)) //for arrow clause, the brace is already written by method visitor
                        {
                            visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                        }
                        visitor.CurrentTypeWriter.Write(node, "return ", true);
                        visitor.WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.Async", methodGenericTypes: [taskSymbol, returnSymbol], arguments: [new CodeNode(() => {
                            visitor.CurrentTypeWriter.WriteLine(node, "async () => ");
                            if (!node.IsKind(SyntaxKind.Block)) //arrow clause has no brace, write it
                            {
                                visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                            }
                            visitor.Visit(node);
                            if (!node.IsKind(SyntaxKind.Block))
                            {
                                visitor.CurrentTypeWriter.WriteLine(node, "}", true);
                            }
                            visitor.CurrentTypeWriter.TrimEnd();
                        })]);
                        visitor.CurrentTypeWriter.WriteLine(node, ";");
                        if (node.IsKind(SyntaxKind.Block))
                        {
                            visitor.CurrentTypeWriter.WriteLine(node, "}", true);
                        }
                        return true;
                    }
                    finally
                    {
                        _processing.Value.Pop();
                    }
                }
            }
            return false;
        }
    }
}
