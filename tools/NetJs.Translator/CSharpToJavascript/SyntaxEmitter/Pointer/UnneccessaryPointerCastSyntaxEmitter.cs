using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    /// <summary>
    /// (int)(void*)IntPtr is unneccessary
    /// Cast to void* while not neccessary here breaks our runtime as an arbitrary number returned by Unsafe.ByteOffset cannot be converted to a pointer
    /// "if ((nint)(void*)Unsafe.ByteOffset(ref *pInputBuffer, ref *pFinalPosWhereCanReadDWordFromInputBuffer) >= 4 * sizeof(uint))",
    /// </summary>
    public class UnneccessaryPointerCastSyntaxEmitter : SyntaxEmitter<CastExpressionSyntax>
    {
        public override bool TryEmit(CastExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Expression.IsKind(SyntaxKind.CastExpression) && node.Expression is CastExpressionSyntax innerCast)
            {
                var outerCastToType = visitor.Global.TryGetTypeSymbol(node.Type, visitor);
                var innerCastToType = visitor.Global.TryGetTypeSymbol(innerCast.Type, visitor);
                if (outerCastToType != null && innerCastToType != null && outerCastToType.IsNumericType() && innerCastToType.IsPointer(out _))
                {
                    visitor.Visit(innerCast.Expression);
                    return true;
                }
            }
            return false;
        }
    }
}
