using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Nullable
{
    internal class NullableCompareNullSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.EqualsExpression) || node.IsKind(SyntaxKind.NotEqualsExpression))
            {
                var leftType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                var rightType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                if (leftType != null && leftType.IsNullable(out _) && node.Right.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    if (node.IsKind(SyntaxKind.EqualsExpression))
                    {
                        visitor.WriteMemberName(node, leftType, "!");
                    }
                    visitor.Visit(node.Left);
                    visitor.CurrentTypeWriter.Write(node, ".");
                    visitor.WriteMemberName(node, leftType, "HasValue");
                    return true;
                }
                else if (rightType != null && rightType.IsNullable(out _) && node.Left.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    if (node.IsKind(SyntaxKind.EqualsExpression))
                    {
                        visitor.WriteMemberName(node, rightType, "!");
                    }
                    visitor.Visit(node.Right);
                    visitor.CurrentTypeWriter.Write(node, ".");
                    visitor.WriteMemberName(node, rightType, "HasValue");
                    return true;
                }
            }
            return false;
        }
    }
}
