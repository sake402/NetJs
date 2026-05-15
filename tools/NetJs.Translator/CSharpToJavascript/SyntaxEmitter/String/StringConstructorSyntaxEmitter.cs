using Microsoft.CodeAnalysis;
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
    sealed class StringConstructorSyntaxEmitter : SyntaxEmitter<ObjectCreationExpressionSyntax>
    {
        public override bool TryEmit(ObjectCreationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Type.ToString().Equals("string", StringComparison.InvariantCultureIgnoreCase))
            {
                var typeSymbol = visitor.Global.GetTypeSymbol(node.Type, visitor)!;
                if (SymbolEqualityComparer.Default.Equals(typeSymbol, visitor.Global.SystemString))
                {
                    var parameterTypes = node.ArgumentList?.Arguments.Select(a => visitor.Global.GetTypeSymbol(a, visitor)).ToArray() ?? [];
                    var ctor = typeSymbol.GetMembers("Create").Cast<IMethodSymbol>().Select((e, i) => (e, i)).SingleOrDefault(e =>
                    {
                        if (e.e.Parameters.Length != parameterTypes.Length)
                            return false;
                        return e.e.Parameters.Select((e, i) => (e, i)).All(e => SymbolEqualityComparer.Default.Equals(e.e.Type, parameterTypes[e.i]));
                    }).e
                    ??
                    typeSymbol.GetMembers("Create").Cast<IMethodSymbol>().Select((e, i) => (e, i)).SingleOrDefault(e =>
                    {
                        if (e.e.Parameters.Length != parameterTypes.Length)
                            return false;
                        return e.e.Parameters.Select((e, i) => (e, i)).All(e => e.e.Type.CanConvertTo(parameterTypes[e.i], visitor.Global, null, out _) > 0);
                    }).e;
                    if (ctor != null)
                    {
                        visitor.WriteMethodInvocation(node, ctor, null, node.ArgumentList?.Arguments.Select(a => new CodeNode(a)) ?? [], null, typeSymbol);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
