using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref
{
    sealed class RefTypeDereferenceOnAccessSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.ThisExpression))
                return false;
            List<CSharpSyntaxNode>? inProcess = null;
            if (visitor.States.TryGetValue(nameof(RefTypeDereferenceOnAccessSyntaxEmitter), out var states))
            {
                inProcess = (List<CSharpSyntaxNode>?)states;
            }
            if (inProcess != null && inProcess.Contains(node))
                return false;
            if (node.IsReadOnlyOperation())
            {
                var symbol = visitor.Global.TryGetSymbol(node, visitor);
                var refKind = symbol?.GetRefKind();
                if (refKind != null && refKind != RefKind.None)
                {
                    bool NeedsDereferenceAccess()
                    {
                        if (refKind != RefKind.Out && node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
                        {
                            var ass = (AssignmentExpressionSyntax)node.Parent;
                            var right = ass.Right;
                            RefKind? rightRefKind = null;
                            if (right.IsKind(SyntaxKind.RefExpression))
                            {
                                rightRefKind = RefKind.Ref;
                            }
                            else
                            {
                                var rsymbolType = visitor.Global.TryGetSymbol(right, visitor);
                                rightRefKind = rsymbolType?.GetRefKind();
                            }
                            if (rightRefKind != null && rightRefKind != RefKind.None)
                            {
                                return false;
                            }
                        }
                        if (node.Parent.IsKind(SyntaxKind.Argument))
                        {
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
