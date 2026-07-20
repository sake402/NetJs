using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    /// <summary>
    /// Handles "string" + 23 or "string" + customObject
    /// </summary>
    sealed class StringPlusNumberSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddExpression))
            {
                var lhsType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rhsType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if (lhsType != null &&
                    rhsType != null &&
                    !SymbolEqualityComparer.Default.Equals(lhsType, rhsType) &&
                    (SymbolEqualityComparer.Default.Equals(lhsType, visitor.Global.SystemString) || SymbolEqualityComparer.Default.Equals(rhsType, visitor.Global.SystemString)))
                {
                    using (BoxPrimitiveAssignmentSyntaxEmitter.Disable(node.Left))
                    {
                        using (BoxPrimitiveAssignmentSyntaxEmitter.Disable(node.Right))
                        {
                            if (!SymbolEqualityComparer.Default.Equals(lhsType, visitor.Global.SystemString) && !lhsType.IsNumericType())
                            {
                                var toString = (IMethodSymbol)lhsType.GetMembers("ToString", visitor.Global).Single(e => e is IMethodSymbol ms && ms.Parameters.Length == 0 && SymbolEqualityComparer.Default.Equals(ms.ReturnType, visitor.Global.SystemString));
                                visitor.WriteMethodInvocation(node, toString, null, null, node.Left, null);
                            }
                            else
                            {
                                visitor.Visit(node.Left);
                            }
                            visitor.CurrentTypeWriter.Write(node, " + ");
                            if (!SymbolEqualityComparer.Default.Equals(rhsType, visitor.Global.SystemString) && !rhsType.IsNumericType())
                            {
                                var toString = (IMethodSymbol)rhsType.GetMembers("ToString", visitor.Global).Single(e => e is IMethodSymbol ms && ms.Parameters.Length == 0 && SymbolEqualityComparer.Default.Equals(ms.ReturnType, visitor.Global.SystemString));
                                visitor.WriteMethodInvocation(node, toString, null, null, node.Right, null);
                            }
                            else
                            {
                                visitor.Visit(node.Right);
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
