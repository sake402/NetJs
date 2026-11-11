using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref
{
    sealed class RefTypeDereferenceOnAccessSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public static bool IsRefTypeToDereference(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor, out bool? isWrite)
        {
            isWrite = null;
            if (node.IsKind(SyntaxKind.ThisExpression) ||
                node.IsKind(SyntaxKind.BaseExpression) ||
                node.IsKind(SyntaxKind.Argument) ||
                node.IsKind(SyntaxKind.RefExpression) ||
                node.IsKind(SyntaxKind.VariableDeclarator) ||
                node.IsKind(SyntaxKind.MethodDeclaration) ||
                node.IsKind(SyntaxKind.LocalFunctionStatement) ||
                node.IsKind(SyntaxKind.PropertyDeclaration) ||
                node.IsKind(SyntaxKind.IndexerDeclaration) ||
                node.IsKind(SyntaxKind.ForEachStatement) ||
                node.IsKind(SyntaxKind.ConditionalExpression) ||
                node.IsKind(SyntaxKind.UsingDirective) ||
                node.IsKind(SyntaxKind.ClassDeclaration) ||
                node.IsKind(SyntaxKind.StructDeclaration) ||
                node.IsKind(SyntaxKind.InterfaceDeclaration) ||
                node.IsKind(SyntaxKind.RecordDeclaration) ||
                node.IsKind(SyntaxKind.SimpleAssignmentExpression) ||
                (node.IsKind(SyntaxKind.IdentifierName) && node.Parent.IsKind(SyntaxKind.InvocationExpression))
                //||
                //node.Parent.IsKind(SyntaxKind.InvocationExpression)
                )
                return false;
            var symbol = visitor.Global.TryGetSymbol(node, visitor);
            if (!node.IsKind(SyntaxKind.InvocationExpression) && symbol?.Kind == SymbolKind.Method)
                return false;
            var refKind = symbol?.GetRefKind();
            if ((refKind == null || refKind == RefKind.None) && node is ExpressionSyntax expression1)
            {
                var _lhsRefKind = visitor.GetRefKind(expression1);
                if (_lhsRefKind != null && _lhsRefKind != RefKind.None)
                    refKind = _lhsRefKind;
            }

            if (refKind != null && refKind != RefKind.None)
            {
                var parent = node.Parent;
                if (parent.IsKind(SyntaxKind.SuppressNullableWarningExpression))
                {
                    parent = parent.Parent;
                }
                if (refKind != RefKind.Out && parent is AssignmentExpressionSyntax ass)
                {
                    if (ass.Left == node)
                        isWrite = true;
                    else if (ass.Right == node)
                        isWrite = false;
                    var lhs = visitor.Global.TryGetSymbol(ass.Left, visitor);
                    var lhsRefKind = lhs?.GetRefKind();
                    if ((lhsRefKind == null || lhsRefKind == RefKind.None) && ass.Left is ExpressionSyntax expression)
                    {
                        var _lhsRefKind = visitor.GetRefKind(expression);
                        if (_lhsRefKind != null && _lhsRefKind != RefKind.None)
                            lhsRefKind = _lhsRefKind;
                    }

                    var rhs = visitor.Global.TryGetSymbol(ass.Right, visitor);
                    var rhsRefKind = rhs?.GetRefKind();
                    if (rhsRefKind == null || rhsRefKind == RefKind.None)
                    {
                        var _rhsRefKind = visitor.GetRefKind(ass.Right);
                        if (_rhsRefKind != null && _rhsRefKind != RefKind.None)
                            rhsRefKind = _rhsRefKind;
                    }
                    if (rhsRefKind == null)
                    {
                        if (ass.Right.IsKind(SyntaxKind.ArrayCreationExpression) ||
                            ass.Right.IsKind(SyntaxKind.ObjectCreationExpression) ||
                            ass.Right.IsKind(SyntaxKind.ImplicitObjectCreationExpression))
                        {
                            rhsRefKind = RefKind.None;
                        }
                    }

                    bool explicitRhsRef = false;
                    if (ass.Right.IsKind(SyntaxKind.RefExpression))
                    {
                        explicitRhsRef = true;
                        rhsRefKind = RefKind.Ref;
                    }
                    if (lhsRefKind != null && rhsRefKind != null)
                    {
                        if (ass.Left == node)
                        {
                            if (lhsRefKind != RefKind.None && rhsRefKind == RefKind.None)
                            {
                                return true;
                            }
                            else if (lhsRefKind != RefKind.None && rhsRefKind != RefKind.None)
                            {
                                if (!explicitRhsRef)
                                    return true;
                                return false;
                            }
                        }
                        if (ass.Right == node && !(rhs is IParameterSymbol pr && pr.IsThis))
                        {
                            if (lhsRefKind == RefKind.None && rhsRefKind != RefKind.None)
                            {
                                return true;
                            }
                            else if (lhsRefKind != RefKind.None && rhsRefKind != RefKind.None && !explicitRhsRef)
                            {
                                return true;
                            }
                        }
                    }
                }
                if (node.IsKind(SyntaxKind.Argument))
                {
                    isWrite = false;
                    if (((ArgumentSyntax)node).RefKindKeyword.ValueText.Length > 0)
                    {
                        return false;
                    }
                }
                ArgumentSyntax? arg = null;
                parent?.VisitParentHierachy((node, depth) =>
                {
                    if (node.IsKind(SyntaxKind.Argument))
                    {
                        arg = (ArgumentSyntax)node;
                        return false;
                    }
                    if (node.IsKind(SyntaxKind.SuppressNullableWarningExpression))
                        return true;
                    return false;
                });
                if (arg != null)
                {
                    isWrite = false;
                    IArgumentOperation? operation = null;
                    foreach (var sm in visitor.SemanticModels)
                    {
                        if (node.SyntaxTree == sm.SyntaxTree)
                        {
                            operation = sm.GetOperation(arg) as IArgumentOperation;
                            break;
                        }
                    }
                    if (operation?.Parameter != null)
                    {
                        var parameterRefKind = operation?.Parameter.GetRefKind();
                        if (parameterRefKind != null && parameterRefKind != RefKind.None) //Passing a ref to a ref argument, no dereference
                            return false;
                    }
                    if (arg.RefKindKeyword.ValueText.Length > 0)
                    {
                        return false;
                    }
                }
                if (parent.IsKind(SyntaxKind.AddressOfExpression))
                {
                    return false;
                }
                if (parent.IsKind(SyntaxKind.RefExpression))
                {
                    return false;
                }
                if (node.IsKind(SyntaxKind.RefExpression))
                {
                    return false;
                }
                if (node.IsKind(SyntaxKind.CastExpression))
                {
                    return false;
                }
                //mainly to handle ref identifier in ref.ExtensionMethod() where first parameter is a ref
                if (parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    isWrite = false;
                    var parentSymbol = visitor.Global.TryGetSymbol(parent, visitor);
                    if (parentSymbol?.Kind == SymbolKind.Method && parentSymbol is IMethodSymbol ms)
                    {
                        if (ms.IsExtensionMethod)
                        {
                            IMethodSymbol mms = ms;
                            if (ms.ReducedFrom != null)
                                mms = ms.ReducedFrom;
                            if (mms.Parameters.Length > 0 && mms.Parameters[0].RefKind != RefKind.None)
                            {
                                return false;
                            }
                        }
                    }
                }
                if (node.IsReadOnlyOperation())
                    isWrite = false;
                return true;
            }
            return false;
        }
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node is LiteralExpressionSyntax ||
                node.IsKind(SyntaxKind.ThisExpression) ||
                node.IsKind(SyntaxKind.Argument) ||
                node.IsKind(SyntaxKind.RefExpression) ||
                node.IsKind(SyntaxKind.VariableDeclarator) ||
                node.IsKind(SyntaxKind.MethodDeclaration) ||
                node.IsKind(SyntaxKind.LocalFunctionStatement) ||
                node.IsKind(SyntaxKind.PropertyDeclaration) ||
                node.IsKind(SyntaxKind.IndexerDeclaration) ||
                node.IsKind(SyntaxKind.ForEachStatement) ||
                node.IsKind(SyntaxKind.ConditionalExpression) ||
                node.IsKind(SyntaxKind.UsingDirective) ||
                node.IsKind(SyntaxKind.ClassDeclaration) ||
                node.IsKind(SyntaxKind.StructDeclaration) ||
                node.IsKind(SyntaxKind.InterfaceDeclaration) ||
                node.IsKind(SyntaxKind.RecordDeclaration) ||
                node.IsKind(SyntaxKind.SimpleAssignmentExpression) ||
                (node.IsKind(SyntaxKind.IdentifierName) && node.Parent.IsKind(SyntaxKind.InvocationExpression))
                //||
                //node.Parent.IsKind(SyntaxKind.InvocationExpression)
                )
                return false;
            if (UnneccessaryUnsafeAddSyntaxEmitter.IsUnsafeAddGetRewriteCandidate(node, visitor, out _, out _))
                return false;
            if (UnneccessaryUnsafeAddSyntaxEmitter.IsUnsafeAddSetRewriteCandidate(node, visitor, out _, out _))
                return false;
            List<CSharpSyntaxNode>? inProcess = null;
            if (visitor.States.TryGetValue(nameof(RefTypeDereferenceOnAccessSyntaxEmitter), out var states))
            {
                inProcess = (List<CSharpSyntaxNode>?)states;
            }
            if (inProcess != null && inProcess.Contains(node))
                return false;
            if (IsRefTypeToDereference(node, visitor, out var isWrite))
            {
                if (UnneccessaryUnsafeAddSyntaxEmitter.IsUnsafeAdd(node, visitor, out var invocation)) //trying to dereference an Unsafe.Add(), let UnneccessaryUnsafeAddSyntaxEmitter handle it
                {
                    if (isWrite == false)
                    {
                        UnneccessaryUnsafeAddSyntaxEmitter.EmitUnsafeAddGet(node, visitor, invocation);
                        return true;
                    }
                }
                inProcess ??= new List<CSharpSyntaxNode>();
                visitor.States[nameof(RefTypeDereferenceOnAccessSyntaxEmitter)] = inProcess;
                inProcess.Add(node);
                visitor.Visit(node);
                visitor.TryDereference(node);
                inProcess.Remove(node);
                return true;
            }
            return false;
        }
    }
}
