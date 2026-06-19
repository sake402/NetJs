using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.QCall
{
    /// <summary>
    /// Simplifies ObjectHandleOnStack.Create(ref t) to { set $v(v){ t = $v } }
    /// </summary>
    sealed class SimpleObjectHandleOnStackCreateSyntaxEmitter : SyntaxEmitter<InvocationExpressionSyntax>
    {
        public override bool TryEmit(InvocationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.ArgumentList.Arguments.Count == 1 && node.ArgumentList.Arguments[0].RefKindKeyword.ValueText == "ref")
            {
                var methodSymbol = visitor.Global.TryGetSymbol(node, visitor) as IMethodSymbol;
                if (methodSymbol != null && methodSymbol.ContainingType.IsType("System.Runtime.CompilerServices.ObjectHandleOnStack") && methodSymbol.Name == "Create")
                {
                    var argLocal = node.ArgumentList.Arguments[0].Expression;
                    visitor.WriteCreateSimpleRef(node, argLocal);
                    return true;
                }
            }
            return false;
        }
    }
}
