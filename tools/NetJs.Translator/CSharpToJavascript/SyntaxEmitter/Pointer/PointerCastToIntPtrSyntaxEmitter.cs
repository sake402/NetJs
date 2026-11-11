using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    /// <summary>
    /// (IntPtr)voidPointer rewites as InteropUtility.castPtr2Address(ptr)
    /// </summary>
    public class PointerCastToIntPtrSyntaxEmitter : SyntaxEmitter<CastExpressionSyntax>
    {
        public override bool TryEmit(CastExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var castFromType = visitor.Global.TryGetTypeSymbol(node.Expression, visitor);
            var castToType = visitor.Global.TryGetTypeSymbol(node.Type, visitor);
            if (castFromType != null &&
                castToType != null &&
                (SymbolEqualityComparer.Default.Equals(castToType, visitor.Global.SystemIntPtr) || SymbolEqualityComparer.Default.Equals(castToType, visitor.Global.SystemUIntPtr)) &&
                castFromType.IsPointer(out _))
            {
                visitor.WriteMethodInvocation(node, "InteropUtility.castPtr2Address", arguments: [node.Expression]);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// (void*)IntPtr rewites as InteropUtility.castAddress2Pointer(address)
    /// </summary>
    public class IntPtrCastToPointerSyntaxEmitter : SyntaxEmitter<CastExpressionSyntax>
    {
        public override bool TryEmit(CastExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var castFromType = visitor.Global.TryGetTypeSymbol(node.Expression, visitor);
            var castToType = visitor.Global.TryGetTypeSymbol(node.Type, visitor);
            if (castFromType != null &&
                castToType != null &&
                (SymbolEqualityComparer.Default.Equals(castFromType, visitor.Global.SystemIntPtr) || SymbolEqualityComparer.Default.Equals(castFromType, visitor.Global.SystemUIntPtr)) &&
                castToType.IsPointer(out _))
            {
                visitor.WriteMethodInvocation(node, "InteropUtility.castAddress2Ptr", arguments: [node.Expression]);
                return true;
            }
            return false;
        }
    }
}
