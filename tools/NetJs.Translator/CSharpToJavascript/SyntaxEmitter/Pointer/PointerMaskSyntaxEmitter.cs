using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    sealed class PointerMaskSyntaxEmitter : SyntaxEmitter<CastExpressionSyntax>
    {
        public override bool TryEmit(CastExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Type.IsKind(SyntaxKind.PointerType))
            {
                var exp = node.Expression;
                while (exp.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    exp = ((ParenthesizedExpressionSyntax)exp).Expression;
                }
                if (exp.IsKind(SyntaxKind.BitwiseAndExpression) || exp.IsKind(SyntaxKind.BitwiseOrExpression) || exp.IsKind(SyntaxKind.ExclusiveOrExpression))
                {
                    var bin = (BinaryExpressionSyntax)exp;
                    if (bin.Left.IsKind(SyntaxKind.CastExpression) && bin.Left is CastExpressionSyntax cast)
                    {
                        var castType = visitor.Global.GetTypeSymbol(cast.Type, visitor);
                        if (castType.IsNumericType())
                        {
                            var cExp = cast.Expression;
                            while (cExp.IsKind(SyntaxKind.ParenthesizedExpression))
                            {
                                cExp = ((ParenthesizedExpressionSyntax)cExp).Expression;
                            }
                            visitor.Visit(cExp);
                            visitor.CurrentTypeWriter.Write(node, ".");
                            visitor.WriteMemberName(node, visitor.Global.SystemPointer, exp.IsKind(SyntaxKind.BitwiseAndExpression) ? "And" : exp.IsKind(SyntaxKind.ExclusiveOrExpression) ? "Xor" : "Or");
                            visitor.CurrentTypeWriter.Write(node, "(");
                            visitor.Visit(bin.Right);
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
