using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Nullable
{
    /// <summary>
    /// Handles expression like a.b > 0 where a.b is a nullable struct with an implicit converter to int
    /// rewrites to a.b != null && a.b > 0
    /// </summary>
    internal class NullableBinaryCompareToBoolResultSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (_processing.Value!.Contains(node))
                return false;
            if (node.IsKind(SyntaxKind.GreaterThanExpression) ||
                node.IsKind(SyntaxKind.LessThanExpression) ||
                node.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
                node.IsKind(SyntaxKind.LessThanOrEqualExpression) ||
                node.IsKind(SyntaxKind.EqualsExpression) ||
                node.IsKind(SyntaxKind.NotEqualsExpression))
            {
                if (!node.Left.IsKind(SyntaxKind.NullLiteralExpression) && !node.Right.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    var leftType = visitor.Global.TryGetTypeSymbol(node.Left, visitor);
                    var rightType = visitor.Global.TryGetTypeSymbol(node.Right, visitor);
                    if (rightType != null && node.Right is LiteralExpressionSyntax && rightType.IsNullable(out var rt))
                    {
                        rightType = rt;
                    }
                    ITypeSymbol elementType = leftType!;
                    if (leftType != null &&
                        rightType != null &&
                        !SymbolEqualityComparer.Default.Equals(leftType, rightType) &&
                        (!leftType.IsValueType || leftType.IsNullable(out elementType!)))
                    {
                        var converterMethod = visitor.GetConverterMethod(elementType!, rightType);
                        if (converterMethod != null)
                        {
                            _processing.Value.Push(node);
                            try
                            {
                                var varName = $"$t{++visitor.CurrentTypeWriter.CurrentClosure.NameManglingSeed}";
                                visitor.CurrentTypeWriter.InsertAbove(node, $"let {varName}", true);
                                visitor.CurrentTypeWriter.Write(node, "(");
                                visitor.CurrentTypeWriter.Write(node, varName);
                                visitor.CurrentTypeWriter.Write(node, " = ");
                                using (ImplicitConversionSyntaxEmitter.Disable(node.Left))
                                {
                                    visitor.Visit(node.Left);
                                }
                                visitor.CurrentTypeWriter.Write(node, ", ");
                                visitor.CurrentTypeWriter.Write(node, varName);
                                visitor.CurrentTypeWriter.Write(node, " !== null && ");
                                visitor.WriteMethodInvocation(node, converterMethod, null, [new CodeNode(() => { visitor.CurrentTypeWriter.Write(node, varName); })], null, null);
                                visitor.CurrentTypeWriter.Write(node, " ");
                                visitor.CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);
                                visitor.CurrentTypeWriter.Write(node, " ");
                                visitor.Visit(node.Right);
                                //visitor.Visit(node);
                                visitor.CurrentTypeWriter.Write(node, ")");
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
