using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.StaticConvention
{
    internal class StaticConventionPropertySetterSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var leftType = visitor.Global.TryGetSymbol(node.Left, visitor);
            if (leftType != null && leftType.Kind == SymbolKind.Property)
            {
                var property = (IPropertySymbol)leftType;
                if (property.IsStaticCallConvention(visitor.Global))
                {
                    CodeNode? leftLeftNode = null;
                    if (node.Left.IsKind(SyntaxKind.IdentifierName))
                    {
                        leftLeftNode = SyntaxFactory.ThisExpression();
                    }
                    else if (node.Left.IsKind(SyntaxKind.SimpleMemberAccessExpression) && node.Left is MemberAccessExpressionSyntax ma)
                    {
                        leftLeftNode = ma.Expression;
                    }
                    if (leftLeftNode != null)
                    {
                        visitor.WriteMemberAccess(node, leftLeftNode, property.ContainingType, null, property, new CodeNode(node.Right));
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
