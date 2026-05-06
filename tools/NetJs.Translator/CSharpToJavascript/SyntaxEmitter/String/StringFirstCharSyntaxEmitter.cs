using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    /// <summary>
    /// Reading string._firstChar produces string.charCodeAt(0)
    /// </summary>
    sealed class StringFirstCharSyntaxEmitter : SyntaxEmitter<MemberAccessExpressionSyntax>
    {
        public override bool TryEmit(MemberAccessExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var field = visitor.Global.TryGetTypeSymbol(node, visitor) as IFieldSymbol;
            if (field != null && field.Name == "_firstChar" && SymbolEqualityComparer.Default.Equals(field.ContainingType, visitor.Global.SystemString))
            {
                visitor.Visit(node.Expression);
                visitor.CurrentTypeWriter.Write(node, ".charCodeAt(0)");
                return true;
            }
            return false;
        }
    }
}
