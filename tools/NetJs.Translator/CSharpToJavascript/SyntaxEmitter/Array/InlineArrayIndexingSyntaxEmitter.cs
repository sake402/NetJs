using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Array
{
    /// <summary>
    /// Handle likes of iarray[3] where iarray is an inline array. Rewrite as iarray.$fields[3]
    /// </summary>
    sealed class InlineArrayIndexingSyntaxEmitter : SyntaxEmitter<ElementAccessExpressionSyntax>
    {
        public override bool TryEmit(ElementAccessExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.ArgumentList.Arguments.Count == 1)
            {
                var type = visitor.Global.GetTypeSymbol(node.Expression, visitor);
                if (type != null && visitor.Global.IsInlineArray(type, out var size, out var elementType))
                {
                    visitor.Visit(node.Expression);
                    visitor.CurrentTypeWriter.Write(node, ".");
                    visitor.CurrentTypeWriter.Write(node, Constants.StructFieldsLayoutName);
                    visitor.CurrentTypeWriter.Write(node, "[");
                    visitor.Visit(node.ArgumentList.Arguments[0]);
                    visitor.CurrentTypeWriter.Write(node, "]");
                    return true;
                }
            }
            return false;
        }
    }
}
