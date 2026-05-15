using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    /// <summary>
    /// Provides a syntax emitter that handles ref expressions targeting the first character field of a string instance.
    /// </summary>
    /// <remarks>This emitter specifically recognizes ref expressions that access the internal '_firstChar'
    /// field of the System.String type. It emits code to ensure the string is represented as a proxy before referencing
    /// its first character. 
    /// </remarks>
    sealed class RefToStringFirstCharSyntaxEmitter : SyntaxEmitter<RefExpressionSyntax>
    {
        public override bool TryEmit(RefExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var field = visitor.Global.TryGetSymbol(node.Expression, visitor) as IFieldSymbol;
            if (field != null && field.Name == "_firstChar" && SymbolEqualityComparer.Default.Equals(field.ContainingType, visitor.Global.SystemString))
            {
                ExpressionSyntax? lhs = null;
                if (node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression) && node.Expression is MemberAccessExpressionSyntax ma)
                {
                    lhs = ma.Expression;
                }
                visitor.WriteMethodInvocation(node, "System.String.EnsureIsProxy", arguments: [new CodeNode(() =>
                {
                    if (lhs != null)
                        visitor.Visit(lhs);
                    else
                        visitor.CurrentTypeWriter.Write(node, "this");
                })]);
                visitor.CurrentTypeWriter.Write(node, ".Reference");
                return true;
            }
            return false;
        }
    }
}
