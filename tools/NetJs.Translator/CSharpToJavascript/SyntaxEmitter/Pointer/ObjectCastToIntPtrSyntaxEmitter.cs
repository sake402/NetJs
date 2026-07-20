using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    /// <summary>
    /// (IntPtr)&object rewites as InteropUtility.castObject2Address(object)
    /// </summary>
    public class ObjectCastToIntPtrSyntaxEmitter : SyntaxEmitter<CastExpressionSyntax>
    {
        public override bool TryEmit(CastExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Expression.IsKind(SyntaxKind.AddressOfExpression) ||
                (node.Expression.IsKind(SyntaxKind.ParenthesizedExpression) && ((ParenthesizedExpressionSyntax)node.Expression).Expression.IsKind(SyntaxKind.AddressOfExpression)))
            {
                var addressOfTarget = node.Expression.IsKind(SyntaxKind.AddressOfExpression) ?
                    ((PrefixUnaryExpressionSyntax)node.Expression).Operand :
                    ((PrefixUnaryExpressionSyntax)((ParenthesizedExpressionSyntax)node.Expression).Expression).Operand;
                var castFromType = visitor.Global.TryGetTypeSymbol(addressOfTarget, visitor);
                var castToType = visitor.Global.TryGetTypeSymbol(node.Type, visitor);
                if (castFromType != null && castToType != null && SymbolEqualityComparer.Default.Equals(castToType, visitor.Global.SystemIntPtr) && !castFromType.IsIntegerNumericType())
                {
                    visitor.WriteMethodInvocation(node, "InteropUtility.castObject2Address", arguments: [addressOfTarget]);
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// *(ReadOnlySpan<int>*)IntPtr rewites as InteropUtility.castAddress2Object(IntPtr)
    /// </summary>
    public class IntPtrCastToObjectSyntaxEmitter : SyntaxEmitter<PrefixUnaryExpressionSyntax>
    {
        public override bool TryEmit(PrefixUnaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.PointerIndirectionExpression) && node.Operand.IsKind(SyntaxKind.CastExpression))
            {
                var cast = (CastExpressionSyntax)node.Operand;
                var castFromType = visitor.Global.TryGetTypeSymbol(cast.Expression, visitor);
                var castToType = visitor.Global.TryGetTypeSymbol(cast.Type, visitor);
                if (castFromType != null && castToType != null && SymbolEqualityComparer.Default.Equals(castFromType, visitor.Global.SystemIntPtr) && castToType.IsPointer(out var pointedType))
                {
                    var method = "InteropUtility.castAddress2Object";
                    //if (pointedType.IsPointer(out _))
                    //{
                    //     method = "InteropUtility.castAddress2Pointer";
                    //}
                    visitor.WriteMethodInvocation(node, method, arguments: [cast.Expression]);
                    if (node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
                    {
                        visitor.CurrentTypeWriter.Write(node, ".");
                        visitor.CurrentTypeWriter.Write(node, Constants.RefValueName);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
