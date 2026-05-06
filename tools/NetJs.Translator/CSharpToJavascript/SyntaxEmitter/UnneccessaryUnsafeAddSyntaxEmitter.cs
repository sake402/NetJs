using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    /// <summary>
    /// And expression like int a = Unsafe.Add(ref first, index); will typically produce let a = $.$spc.System.Runtime.CompilerServices.Unsafe.Add$1(T)(first, index).$v;
    /// Rewrite as let a = first.Get(index), this will be way faster as it doesnt create the temp reference object returned by Unsafe.Add
    /// </summary>
    internal class UnneccessaryUnsafeAddSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.SimpleAssignmentExpression) || node.IsKind(SyntaxKind.EqualsValueClause))
            {
                var left = (node as AssignmentExpressionSyntax)?.Left ?? (node as EqualsValueClauseSyntax)?.Parent;
                var right = (node as AssignmentExpressionSyntax)?.Right ?? (node as EqualsValueClauseSyntax)?.Value;
                if (left != null && right != null && right.IsKind(SyntaxKind.InvocationExpression) && right.ToString().StartsWith("Unsafe.Add("))
                {
                    var lhsType = visitor.Global.GetTypeSymbol(left, visitor);
                    var leftRefKind = lhsType.GetRefKind() ?? RefKind.None;
                    if (leftRefKind == RefKind.None &&
                        right is InvocationExpressionSyntax inv &&
                        inv.ArgumentList.Arguments.Count == 2)
                    {
                        if (node.IsKind(SyntaxKind.SimpleAssignmentExpression))
                        {
                            visitor.Visit(left);
                        }
                        visitor.CurrentTypeWriter.Write(node, " = ");
                        visitor.Visit(inv.ArgumentList.Arguments[0]);
                        visitor.CurrentTypeWriter.Write(node, ".GetAt(");
                        visitor.Visit(inv.ArgumentList.Arguments[1]);
                        visitor.CurrentTypeWriter.Write(node, ")");
                        return true;
                    }
                }
                else if (left != null && right != null && left.IsKind(SyntaxKind.InvocationExpression) && left.ToString().StartsWith("Unsafe.Add("))
                {
                    var rhsType = visitor.Global.GetTypeSymbol(right, visitor);
                    var rightRefKind = rhsType.GetRefKind() ?? RefKind.None;
                    if (rightRefKind == RefKind.None &&
                        left is InvocationExpressionSyntax inv &&
                        inv.ArgumentList.Arguments.Count == 2) //left.Set(right, int)
                    {
                        visitor.Visit(inv.ArgumentList.Arguments[0]);
                        visitor.CurrentTypeWriter.Write(node, ".SetAt(");
                        visitor.Visit(right);
                        visitor.CurrentTypeWriter.Write(node, ", ");
                        visitor.Visit(inv.ArgumentList.Arguments[1]);
                        visitor.CurrentTypeWriter.Write(node, ")");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}