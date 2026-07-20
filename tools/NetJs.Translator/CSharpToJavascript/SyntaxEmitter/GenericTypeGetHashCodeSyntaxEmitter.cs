using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    //T.GetHashCode() should call Object.GetHashCodeT
    sealed class GenericTypeGetHashCodeSyntaxEmitter : SyntaxEmitter<InvocationExpressionSyntax>
    {
        public override bool TryEmit(InvocationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccess = (MemberAccessExpressionSyntax)node.Expression;
                var lhs = visitor.Global.GetSymbol(memberAccess.Expression, visitor);
                var lhsType = visitor.Global.GetTypeSymbol(lhs);
                var methodSymbol = visitor.Global.GetSymbol(node, visitor) as IMethodSymbol;
                if (methodSymbol != null && lhsType.TypeKind == TypeKind.TypeParameter)
                {
                    if (methodSymbol.Name == "GetHashCode" &&
                        methodSymbol.Parameters.Length == 0 &&
                        SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, visitor.Global.SystemInt32))
                    {
                        //Write Object.GetHashCodeT<T>(T value)
                        var getHashCode = (IMethodSymbol)visitor.Global.SystemObject
                            .GetMembers("GetHashCodeT")
                            .Single(e => e is IMethodSymbol ms && ms.TypeParameters.Length == 1 && ms.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, ms.TypeParameters[0]));
                        getHashCode = getHashCode.Construct(lhsType);
                        visitor.WriteMethodInvocation(node, getHashCode, null, [new CodeNode(() => {
                            visitor.Visit(memberAccess.Expression);
                        })], null, null);
                        return true;
                    }
                }
            }
            if (node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                var lhs = visitor.Global.GetSymbol(((ConditionalAccessExpressionSyntax)node.Parent).Expression, visitor);
                var lhsType = visitor.Global.GetTypeSymbol(lhs);
                var methodSymbol = visitor.Global.GetSymbol(node, visitor) as IMethodSymbol;
                if (methodSymbol != null && lhsType.TypeKind == TypeKind.TypeParameter)
                {
                    if (methodSymbol.Name == "GetHashCode" &&
                        methodSymbol.Parameters.Length == 0 &&
                        SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, visitor.Global.SystemInt32))
                    {
                        //Write Object.GetHashCodeT<T>(T value)
                        var getHashCode = (IMethodSymbol)visitor.Global.SystemObject
                            .GetMembers("GetHashCodeT")
                            .Single(e => e is IMethodSymbol ms && ms.TypeParameters.Length == 1 && ms.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, ms.TypeParameters[0]));
                        getHashCode = getHashCode.Construct(lhsType);
                        visitor.WriteMethodInvocation(node, getHashCode, null, [new CodeNode(() => {
                            visitor.CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                        })], null, null);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
