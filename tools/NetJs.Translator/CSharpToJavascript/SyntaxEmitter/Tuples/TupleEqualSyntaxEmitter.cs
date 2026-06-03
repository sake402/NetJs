using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Tuples
{
    internal class TupleEqualSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.EqualsExpression) || node.IsKind(SyntaxKind.NotEqualsExpression))
            {
                var leftType = visitor.Global.TryGetTypeSymbol(node.Left, visitor) as INamedTypeSymbol;
                var rightType = visitor.Global.TryGetTypeSymbol(node.Right, visitor) as INamedTypeSymbol;
                if (leftType != null && rightType != null && leftType.IsTupleType && rightType.IsTupleType)
                {
                    var leftElements = leftType.TupleElements;
                    var rightElements = rightType.TupleElements;
                    if (leftElements.Length == rightElements.Length)
                    {
                        for (int i = 0; i < leftElements.Length; i++)
                        {
                            if (!SymbolEqualityComparer.Default.Equals(leftElements[i].Type, rightElements[i].Type))
                                return false;
                        }
                        var equalsMethod = leftType.GetMembers("Equals")
                            .FirstOrDefault(m => m is IMethodSymbol method && 
                            method.Parameters.Length == 1 && 
                            SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, rightType)) as IMethodSymbol;
                        if (equalsMethod != null)
                        {
                            if (node.IsKind(SyntaxKind.NotEqualsExpression))
                            {
                                visitor.CurrentTypeWriter.Write(node, "!");
                            }
                            visitor.WriteMethodInvocation(node, equalsMethod, null, [node.Right], node.Left, leftType);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
