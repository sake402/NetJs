using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript;
using System;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    /// <summary>
    /// Provides a syntax emitter that handles object creation expressions for the string type.
    /// </summary>
    /// <remarks>This class specializes in emitting syntax for object creation expressions where the target
    /// type is a string. It ensures that the correct string constructor is invoked based on the argument types
    /// provided. 
    /// </remarks>
    sealed class StringConstructorSyntaxEmitter : SyntaxEmitter<ExpressionSyntax>
    {
        public override bool TryEmit(ExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.ObjectCreationExpression) || node.IsKind(SyntaxKind.ImplicitObjectCreationExpression))
            {
                var typeSymbol = node.IsKind(SyntaxKind.ObjectCreationExpression) ? visitor.Global.GetTypeSymbol(((ObjectCreationExpressionSyntax)node).Type, visitor)! : visitor.Global.GetTypeSymbol((ImplicitObjectCreationExpressionSyntax)node, visitor)!;
                var arguments = node.IsKind(SyntaxKind.ObjectCreationExpression) ? ((ObjectCreationExpressionSyntax)node).ArgumentList?.Arguments! : ((ImplicitObjectCreationExpressionSyntax)node).ArgumentList.Arguments!;
                if (SymbolEqualityComparer.Default.Equals(typeSymbol, visitor.Global.SystemString))
                {
                    var parameterTypes = arguments?.Select(a => visitor.Global.GetTypeSymbol(a, visitor)).ToArray() ?? [];
                    var ctor = typeSymbol.GetMembers("Create").Cast<IMethodSymbol>().Select((e, i) => (e, i)).FirstOrDefault(e =>
                    {
                        if (e.e.Parameters.Length != parameterTypes.Length)
                            return false;
                        return e.e.Parameters.Select((e, i) => (e, i)).All(e => SymbolEqualityComparer.Default.Equals(e.e.Type, parameterTypes[e.i]));
                    }).e
                    ??
                    typeSymbol.GetMembers("Create").Cast<IMethodSymbol>().Select((e, i) => (e, i)).FirstOrDefault(e =>
                    {
                        if (e.e.Parameters.Length != parameterTypes.Length)
                            return false;
                        return e.e.Parameters.Select((e, i) => (e, i)).All(e => e.e.Type.CanConvertTo(parameterTypes[e.i], visitor.Global, null, out _) > 0);
                    }).e;
                    if (ctor != null)
                    {
                        visitor.WriteMethodInvocation(node, ctor, null, arguments?.Select(a => new CodeNode(a)), null, typeSymbol);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
