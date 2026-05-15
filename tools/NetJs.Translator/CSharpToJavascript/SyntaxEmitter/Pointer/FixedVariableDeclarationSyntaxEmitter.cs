using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    sealed class FixedVariableDeclarationSyntaxEmitter : SyntaxEmitter<VariableDeclaratorSyntax>
    {
        public override bool TryEmit(VariableDeclaratorSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Initializer != null)
            {
                var declaration = node.FindClosestParent<VariableDeclarationSyntax>();
                if (declaration != null && declaration.Parent.IsKind(SyntaxKind.FixedStatement))
                {
                    var rhsType = visitor.Global.TryGetTypeSymbol(node.Initializer.Value, visitor);
                    if (rhsType != null && !rhsType.IsPointer(out _))
                    {
                        //check if the rhsType has GetPinnableReference method
                        var getPinnableMethod = rhsType.GetMembers("GetPinnableReference")
                            .FirstOrDefault(m => m is IMethodSymbol method && method.Parameters.Length == 0 && (method.ReturnType.IsPointer(out _) || method.RefKind != RefKind.None)) as IMethodSymbol;
                        if (getPinnableMethod != null)
                        {
                            visitor.CurrentTypeWriter.Write(node, node.Identifier.Text);
                            visitor.CurrentTypeWriter.Write(node, " = ");
                            visitor.WriteMethodInvocation(node, getPinnableMethod, null, null, node.Initializer.Value, null, null, false);
                            return true;
                        }
                        else if (rhsType.IsArray(out var elementType))
                        {
                            visitor.CurrentTypeWriter.Write(node, node.Identifier.Text);
                            visitor.CurrentTypeWriter.Write(node, " = ");
                            visitor.WriteCreateArrayRefOrPointer(node, elementType, node.Initializer.Value, null);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}