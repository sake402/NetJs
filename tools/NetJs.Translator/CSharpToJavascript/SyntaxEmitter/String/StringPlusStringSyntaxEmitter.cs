using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    /// <summary>
    /// Handles "string" + "string". This checks if any of the string is null and replaces it with empty string. This is to avoid null + "string" which will result in "nullstring" in JS.
    /// </summary>
    sealed class StringPlusStringSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddExpression))
            {
                var lhsType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rhsType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if (lhsType != null &&
                    rhsType != null &&
                    SymbolEqualityComparer.Default.Equals(lhsType, visitor.Global.SystemString) &&
                    SymbolEqualityComparer.Default.Equals(rhsType, visitor.Global.SystemString))
                {
                    bool IsNullableString(CSharpSyntaxNode node)
                    {
                        if (node.IsKind(SyntaxKind.StringLiteralExpression))
                            return false;
                        if (node.IsKind(SyntaxKind.AddExpression) && node is BinaryExpressionSyntax bn && (!IsNullableString(bn.Left) || !IsNullableString(bn.Right)))
                            return false;
                        if (node.IsKind(SyntaxKind.ConditionalExpression) && node is ConditionalExpressionSyntax ce && !IsNullableString(ce.WhenTrue) && !IsNullableString(ce.WhenFalse))
                            return false;
                        return true;
                    }

                    if (!IsNullableString(node.Left))
                    {
                        visitor.Visit(node.Left);
                    }
                    else
                    {
                        visitor.CurrentTypeWriter.Write(node, visitor.Global.GlobalName);
                        visitor.CurrentTypeWriter.Write(node, ".");
                        visitor.CurrentTypeWriter.Write(node, Constants.StringNotNull);
                        visitor.CurrentTypeWriter.Write(node, "(");
                        visitor.Visit(node.Left);
                        visitor.CurrentTypeWriter.Write(node, ")");
                    }

                    visitor.CurrentTypeWriter.Write(node, " + ");

                    if (!IsNullableString(node.Right))
                    {
                        visitor.Visit(node.Right);
                    }
                    else
                    {
                        visitor.CurrentTypeWriter.Write(node, visitor.Global.GlobalName);
                        visitor.CurrentTypeWriter.Write(node, ".");
                        visitor.CurrentTypeWriter.Write(node, Constants.StringNotNull);
                        visitor.CurrentTypeWriter.Write(node, "(");
                        visitor.Visit(node.Right);
                        visitor.CurrentTypeWriter.Write(node, ")");
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
