using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Array
{
    /// <summary>
    /// Handle likes of (Span<int>)iarray where iarray is an inline array or a fixed size buffer. Create a span using the implicit operator
    /// </summary>
    sealed class InlineArrayCastToSpanSyntaxEmitter : SyntaxEmitter<CastExpressionSyntax>
    {
        public override bool TryEmit(CastExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var castFrom = visitor.Global.TryGetSymbol(node.Expression, visitor);
            var castFromType = visitor.Global.TryGetTypeSymbol(node.Expression, visitor);
            var castToType = visitor.Global.TryGetTypeSymbol(node.Type, visitor);
            if (castFromType != null &&
                castToType != null &&
                (visitor.Global.IsInlineArray(castFromType, out _, out _) || (castFrom != null && visitor.Global.IsFixedSizeField(castFrom, out _, out _))) &&
                (SymbolEqualityComparer.Default.Equals(castToType.OriginalDefinition, visitor.Global.SystemReadOnlySpan) ||
                SymbolEqualityComparer.Default.Equals(castToType.OriginalDefinition, visitor.Global.SystemSpan)))
            {
                var spanType = castToType;
                var implicitConverter = spanType.GetMembers("op_Implicit", visitor.Global)
                    .Cast<IMethodSymbol>()
                    .FirstOrDefault(e => e.Parameters.Length == 1 && e.Parameters[0].Type.IsArray(out var t) && e.ReturnType.Equals(spanType, SymbolEqualityComparer.Default));
                visitor.WriteMethodInvocation(node, implicitConverter, null, [new CodeNode(() => {
                    visitor.Visit(node.Expression);
                    visitor.CurrentTypeWriter.Write(node, ".");
                    visitor.CurrentTypeWriter.Write(node, Constants.StructFieldsLayoutName);
                })], null, null, null, false);
                return true;
            }
            return false;
        }
    }
}
