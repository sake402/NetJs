using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Numbers
{
    //~ulong wont give the extected result unless we mask it to the 64 bits required of long
    sealed class NotOfBigIntShiftSyntaxEmitter : SyntaxEmitter<PrefixUnaryExpressionSyntax>
    {
        public override bool TryEmit(PrefixUnaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.BitwiseNotExpression))
            {
                var type = visitor.Global.GetTypeSymbol(node.Operand, visitor);
                if (type.IsLongNumericType() && type.IsUnsignedNumericType())
                {
                    visitor.CurrentTypeWriter.Write(node, "(");
                    visitor.CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);
                    visitor.Visit(node.Operand);
                    visitor.CurrentTypeWriter.Write(node, " & 0xFFFFFFFFFFFFFFFFn)");
                    return true;
                }
            }
            return false;
        }
    }
}
