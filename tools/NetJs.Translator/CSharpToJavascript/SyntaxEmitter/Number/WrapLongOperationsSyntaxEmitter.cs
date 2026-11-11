using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Numbers
{
    /// <summary>
    /// All long or ulong operations must produce long or ulong, clipped to the range of long or ulong(64 bits).
    /// </summary>
    sealed class WrapLongOperationsSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddExpression) || node.IsKind(SyntaxKind.SubtractExpression) || node.IsKind(SyntaxKind.MultiplyExpression))
            {
                var lhsType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rhsType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if (lhsType != null &&
                    rhsType != null &&
                    (lhsType.IsLongNumericType() || rhsType.IsLongNumericType()) &&
                    !lhsType.IsFloatingNumericType() &&
                    !rhsType.IsFloatingNumericType())
                {
                    foreach (var sm in visitor.SemanticModels)
                    {
                        if (node.SyntaxTree == sm.SyntaxTree)
                        {
                            bool isSignedResult = lhsType.IsSignedNumericType() ||
                                rhsType.IsSignedNumericType() ||
                                lhsType.SpecialType == SpecialType.System_Int64 ||
                                rhsType.SpecialType == SpecialType.System_Int64;

                            visitor.CurrentTypeWriter.Write(node, isSignedResult ? "BigInt.asIntN(64, " : "BigInt.asUintN(64, ");
                            visitor.Visit(node.Left);
                            visitor.CurrentTypeWriter.Write(node, " ");
                            visitor.CurrentTypeWriter.Write(node, node.OperatorToken.Text);
                            visitor.CurrentTypeWriter.Write(node, " ");
                            visitor.Visit(node.Right);
                            visitor.CurrentTypeWriter.Write(node, ")");
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
