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
    sealed class AddressOfStringFirstCharSyntaxEmitter : SyntaxEmitter<PrefixUnaryExpressionSyntax>
    {
        public override bool TryEmit(PrefixUnaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddressOfExpression))
            {
                var field = visitor.Global.TryGetSymbol(node.Operand, visitor) as IFieldSymbol;
                if (field != null && field.Name == "_firstChar" && SymbolEqualityComparer.Default.Equals(field.ContainingType, visitor.Global.SystemString))
                {
                    ExpressionSyntax? lhs = null;
                    if (node.Operand.IsKind(SyntaxKind.SimpleMemberAccessExpression) && node.Operand is MemberAccessExpressionSyntax ma)
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
            }
            return false;
        }
    }
}
