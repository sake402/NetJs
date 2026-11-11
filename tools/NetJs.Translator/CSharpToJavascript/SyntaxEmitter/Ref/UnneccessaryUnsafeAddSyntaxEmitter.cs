using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref
{
    /// <summary>
    /// An expression like "int a = Unsafe.Add(ref first, index)" 
    /// will typically produce "let a = $.$spc.System.Runtime.CompilerServices.Unsafe.Add$1(T)(first, index).$v"
    /// Rewrite as let a = first.Get(index) or first.SetAt(...), this will be way faster as it doesnt create the temp js reference object returned by Unsafe.Add
    /// </summary>
    internal class UnneccessaryUnsafeAddSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public static bool IsUnsafeAdd(SyntaxNode node, TranslatorSyntaxVisitor visitor, out InvocationExpressionSyntax inv)
        {
            var mt = visitor.Global.GetSymbol(node, visitor);
            if (mt.Kind == SymbolKind.Method && ((IMethodSymbol)mt).ContainingType.Name == "Unsafe" && ((IMethodSymbol)mt).Name == "Add")
            {
                inv = (InvocationExpressionSyntax)node;
                return true;
            }
            inv = null!;
            return false;
        }
        public static bool IsUnsafeAddGetRewriteCandidate(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor, out SyntaxNode? lhs, out InvocationExpressionSyntax inv)
        {
            bool isReturn = false;
            if (node.IsKind(SyntaxKind.SimpleAssignmentExpression) ||
                node.IsKind(SyntaxKind.EqualsValueClause) ||
                (node.IsKind(SyntaxKind.Argument) && !((ArgumentSyntax)node).RefKindKeyword.IsKind(SyntaxKind.RefKeyword)) ||
                (isReturn = node.Parent.IsKind(SyntaxKind.ReturnStatement) && !node.IsKind(SyntaxKind.RefExpression)))
            {
                var left = (node as AssignmentExpressionSyntax)?.Left ?? (node as EqualsValueClauseSyntax)?.Parent ?? (node as ArgumentSyntax) ?? node;
                var right = (node as AssignmentExpressionSyntax)?.Right ?? (node as EqualsValueClauseSyntax)?.Value ?? (node as ArgumentSyntax)?.Expression ?? node;
                if (left != null &&
                    right != null &&
                    right.IsKind(SyntaxKind.InvocationExpression) &&
                    IsUnsafeAdd(right, visitor, out var inv2))
                {
                    var lhsType = node.IsKind(SyntaxKind.Argument) ? visitor.GetParameterSymbol((ArgumentSyntax)node) : visitor.Global.GetSymbol(left, visitor);
                    if (lhsType != null)
                    {
                        var leftRefKind = lhsType.GetRefKind() ?? RefKind.None;
                        if ((leftRefKind == RefKind.None || isReturn) &&
                            inv2.ArgumentList.Arguments.Count == 2)
                        {
                            lhs = left;
                            inv = inv2;
                            return true;
                        }
                    }
                }
            }
            lhs = null;
            inv = null!;
            return false;
        }

        public static bool IsUnsafeAddSetRewriteCandidate(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor, out InvocationExpressionSyntax inv, out CSharpSyntaxNode? rhs)
        {
            if (node.IsKind(SyntaxKind.SimpleAssignmentExpression) ||
                node.IsKind(SyntaxKind.EqualsValueClause))
            {
                var left = (node as AssignmentExpressionSyntax)?.Left ?? (node as EqualsValueClauseSyntax)?.Parent;
                var right = (node as AssignmentExpressionSyntax)?.Right ?? (node as EqualsValueClauseSyntax)?.Value;
                if (left != null && right != null && left.IsKind(SyntaxKind.InvocationExpression) && IsUnsafeAdd(left, visitor, out var inv2))
                {
                    var rhsType = visitor.Global.GetSymbol(right, visitor);
                    var rightRefKind = rhsType.GetRefKind() ?? RefKind.None;
                    if (rightRefKind == RefKind.None &&
                        inv2.ArgumentList.Arguments.Count == 2) //left.Set(right, int)
                    {
                        inv = inv2;
                        rhs = right;
                        return true;
                    }
                }
            }
            inv = null!;
            rhs = null!;
            return false;
        }

        public static void EmitUnsafeAddGet(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor, InvocationExpressionSyntax inv)
        {
            visitor.Visit(inv.ArgumentList.Arguments[0]);
            visitor.CurrentTypeWriter.Write(node, ".");
            visitor.WriteMemberName(node, visitor.Global.SystemPointer, "GetAt");
            visitor.CurrentTypeWriter.Write(node, "(");
            visitor.Visit(inv.ArgumentList.Arguments[1]);
            visitor.CurrentTypeWriter.Write(node, ")");
        }

        public static void EmitUnsafeAddSet(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor, InvocationExpressionSyntax inv, CodeNode value)
        {
            visitor.Visit(inv.ArgumentList.Arguments[0]);
            visitor.CurrentTypeWriter.Write(node, ".");
            visitor.WriteMemberName(node, visitor.Global.SystemPointer, "SetAt");
            visitor.CurrentTypeWriter.Write(node, "(");
            visitor.VisitNode(value);
            visitor.CurrentTypeWriter.Write(node, ", ");
            visitor.Visit(inv.ArgumentList.Arguments[1]);
            visitor.CurrentTypeWriter.Write(node, ")");
        }

        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            return false;
            if (IsUnsafeAddGetRewriteCandidate(node, visitor, out var left, out var inv))
            {
                if (node.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    visitor.Visit(left);
                }
                if (node.IsKind(SyntaxKind.SimpleAssignmentExpression) || node.IsKind(SyntaxKind.EqualsValueClause))
                {
                    visitor.CurrentTypeWriter.Write(node, " = ");
                }
                EmitUnsafeAddGet(node, visitor, inv);
                return true;
            }
            if (IsUnsafeAddSetRewriteCandidate(node, visitor, out var inv2, out var right))
            {
                EmitUnsafeAddSet(node, visitor, inv2, right);
                return true;
            }
            return false;
        }
    }
}