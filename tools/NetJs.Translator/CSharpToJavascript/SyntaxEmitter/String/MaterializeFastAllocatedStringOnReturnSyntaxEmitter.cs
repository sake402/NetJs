using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    /// <summary>
    /// Provides a syntax emitter that rewrites return statements to materialize fast-allocated strings before returning
    /// them.
    /// </summary>
    /// <remarks>This emitter targets return statements that return identifiers referencing strings created
    /// via fast allocation. When such a string is detected, the emitter ensures that the string is materialized by
    /// invoking the appropriate collection method before it is returned. This is useful in scenarios where strings are
    /// allocated using specialized mechanisms and require explicit materialization prior to being returned from a
    /// method or property.</remarks>
    sealed class MaterializeFastAllocatedStringOnReturnSyntaxEmitter : SyntaxEmitter<ReturnStatementSyntax>
    {
        public override bool TryEmit(ReturnStatementSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Expression != null && node.Expression.IsKind(SyntaxKind.IdentifierName) && node.Expression is IdentifierNameSyntax id)
            {
                var type = visitor.Global.TryGetTypeSymbol(node.Expression, visitor);
                if (SymbolEqualityComparer.Default.Equals(type, visitor.Global.SystemString))
                {
                    var method = node.FindClosestParent<MethodDeclarationSyntax>();
                    var property = node.FindClosestParent<PropertyDeclarationSyntax>();
                    if (method != null || property != null)
                    {
                        //find the variable declarator for the storage identifier of the string we are returning
                        var stringDeclaration = (method?.DescendantNodes() ?? property!.DescendantNodes()).FirstOrDefault(n =>
                        {
                            if (n.IsKind(SyntaxKind.VariableDeclarator) && n is VariableDeclaratorSyntax vd)
                            {
                                return vd.Identifier.ValueText == id.Identifier.ValueText;
                            }
                            return false;
                        });
                        if (stringDeclaration != null)
                        {
                            if (stringDeclaration is VariableDeclaratorSyntax vd)
                            {
                                if (vd.Initializer != null)
                                {
                                    //if the initializer is a call to string.FastAllocateString, we need to unwrap this return by calling Collect on the proxy;
                                    if (vd.Initializer.ToString().Contains("FastAllocateString"))
                                    {
                                        visitor.WriteReturn(node, new CodeNode(() =>
                                        {
                                            visitor.Visit(node.Expression);
                                            visitor.CurrentTypeWriter.Write(node, ".Collect");
                                        }));
                                        //visitor.CurrentTypeWriter.Write(node, )
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
