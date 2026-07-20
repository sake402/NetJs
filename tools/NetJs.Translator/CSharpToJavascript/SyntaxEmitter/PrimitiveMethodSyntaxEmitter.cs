using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    //int.GetType() should be inlined to System.Int32.$type
    sealed class PrimitiveMethodSyntaxEmitter : SyntaxEmitter<InvocationExpressionSyntax>
    {
        public override bool TryEmit(InvocationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccess = (MemberAccessExpressionSyntax)node.Expression;
                var lhs = visitor.Global.GetSymbol(memberAccess.Expression, visitor);
                var lhsType = visitor.Global.GetTypeSymbol(lhs);
                var methodSymbol = visitor.Global.GetSymbol(node, visitor) as IMethodSymbol;
                if (methodSymbol != null && lhsType.IsJsPrimitive())
                {
                    if (methodSymbol.Name == "GetType" &&
                        lhsType.IsSealed &&
                        methodSymbol.Parameters.Length == 0 &&
                        SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, visitor.Global.SystemObject) &&
                        SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, visitor.Global.SystemType))
                    {
                        visitor.CurrentTypeWriter.Write(node, lhsType.ComputeOutputTypeName(visitor.Global));
                        visitor.CurrentTypeWriter.Write(node, ".");
                        visitor.CurrentTypeWriter.Write(node, Constants.PrototypeTypeName);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
