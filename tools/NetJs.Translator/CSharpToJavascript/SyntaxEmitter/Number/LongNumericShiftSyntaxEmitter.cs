using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Number
{
    /// <summary>
    /// long >>= int, should be long >>= BigInt(int)
    /// </summary>
    internal class LongNumericShiftSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Parent.IsKind(SyntaxKind.LeftShiftAssignmentExpression) || node.Parent.IsKind(SyntaxKind.RightShiftAssignmentExpression))
            {
                if (_processing.Value!.Contains(node))
                    return false;
                var ass = (AssignmentExpressionSyntax)node.Parent;
                if (ass.Right == node)
                {
                    var leftType = visitor.Global.TryGetTypeSymbol(ass.Left, visitor);
                    if (leftType != null && leftType.IsLongNumericType())
                    {
                        var rightType = visitor.Global.TryGetTypeSymbol(node, visitor);
                        if (rightType != null && rightType.IsNumericType() && !rightType.IsLongNumericType())
                        {
                            _processing.Value!.Push(node);
                            try
                            {
                                if (node is not LiteralExpressionSyntax)
                                    visitor.CurrentTypeWriter.Write(node, "BigInt(");
                                visitor.Visit(node);
                                if (node is not LiteralExpressionSyntax)
                                    visitor.CurrentTypeWriter.Write(node, ")");
                                else
                                    visitor.CurrentTypeWriter.Write(node, "n");
                            }
                            finally { _processing.Value.Pop(); }
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
