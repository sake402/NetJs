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
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.ThisExpression) ||
                node.IsKind(SyntaxKind.Argument) ||
                node.IsKind(SyntaxKind.RefExpression) ||
                node.IsKind(SyntaxKind.VariableDeclarator) ||
                node.IsKind(SyntaxKind.MethodDeclaration) ||
                node.IsKind(SyntaxKind.PropertyDeclaration) ||
                node.IsKind(SyntaxKind.IndexerDeclaration) ||
                node.IsKind(SyntaxKind.ConditionalExpression))
                return false;
            List<CSharpSyntaxNode>? inProcess = null;
            if (visitor.States.TryGetValue(nameof(RefTypeDereferenceOnAccessSyntaxEmitter), out var states))
            {
                inProcess = (List<CSharpSyntaxNode>?)states;
            }
            if (inProcess != null && inProcess.Contains(node))
                return false;
            //if (node.IsReadOnlyOperation() ||
            //(node.Parent is AssignmentExpressionSyntax ass && ass.Left == node && (visitor.Global.GetSymbol(node, visitor).GetRefKind() ?? RefKind.None) != RefKind.None))
            {
                var symbol = visitor.Global.TryGetSymbol(node, visitor);
                var refKind = symbol?.GetRefKind();
                if ((refKind == null || refKind == RefKind.None) && node is ExpressionSyntax expression)
                {
                    var _lhsRefKind = visitor.GetRefKind(expression);
                    if (_lhsRefKind != null && _lhsRefKind != RefKind.None)
                        refKind = _lhsRefKind;
                }

                if (refKind != null && refKind != RefKind.None)
                {
                    bool NeedsDereferenceAccess()
                    {
                        if (refKind != RefKind.Out && node.Parent is AssignmentExpressionSyntax ass)
                        {
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
                            //var right = ass.Right;
                            //RefKind? rightRefKind = null;
                            //if (right.IsKind(SyntaxKind.RefExpression))
                            //{
                            //    rightRefKind = RefKind.Ref;
                            //}
                            //else
                            //{
                            //    var rsymbolType = visitor.Global.TryGetSymbol(right, visitor);
                            //    rightRefKind = rsymbolType?.GetRefKind();
                            //}
                            //if (rightRefKind != null && rightRefKind != RefKind.None)
                            //{
                            //    return false;
                            //}
                        }
                        if (node.IsKind(SyntaxKind.Argument))
                        {
                            if (((ArgumentSyntax)node).RefKindKeyword.ValueText.Length > 0)
                            {
                                return false;
                            }
                        }
                        if (node.Parent.IsKind(SyntaxKind.Argument))
                        {
                            IArgumentOperation? operation = null;
                            foreach(var sm in visitor.SemanticModels)
                            {
                                if (node.SyntaxTree == sm.SyntaxTree)
                                {
                                    operation = sm.GetOperation(node.Parent) as IArgumentOperation;
                                    break;
                                }
                            }
                            if (operation?.Parameter!= null)
                            {
                                var parameterRefKind = operation?.Parameter.GetRefKind();
                                if (parameterRefKind != null && parameterRefKind != RefKind.None) //Passing a ref to a ref argument, no dereference
                                    return false;
                            }
                            if (((ArgumentSyntax)node.Parent).RefKindKeyword.ValueText.Length > 0)
                            {
                                return false;
                            }
                        }
                        if (node.Parent.IsKind(SyntaxKind.AddressOfExpression))
                        {
                            return false;
                        }
                        if (node.Parent.IsKind(SyntaxKind.RefExpression))
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
                        return true;
                        //return node.Parent is BinaryExpressionSyntax ||
                        //    node.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression)/* is MemberAccessExpressionSyntax*/ ||
                        //    node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression);
                    }
                    if (NeedsDereferenceAccess())
                    {
                        inProcess ??= new List<CSharpSyntaxNode>();
                        visitor.States[nameof(RefTypeDereferenceOnAccessSyntaxEmitter)] = inProcess;
                        inProcess.Add(node);
                        visitor.Visit(node);
                        visitor.TryDereference(node);
                        inProcess.Remove(node);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
