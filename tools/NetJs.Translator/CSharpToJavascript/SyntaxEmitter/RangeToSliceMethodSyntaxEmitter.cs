using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.ComponentModel.Design;
using System.Linq.Expressions;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    //Handles an index operator on a ReadOnlySpan and rewrite it as span[range] => span.Slice(range.Start, range.Length)
    sealed class RangeToSliceMethodSyntaxEmitter : SyntaxEmitter<ElementAccessExpressionSyntax>
    {
        public override bool TryEmit(ElementAccessExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var targetType = visitor.Global.TryGetTypeSymbol(node.Expression, visitor);
            if (targetType != null)
            {
                if (node.ArgumentList.Arguments.Count == 1)
                {
                    var arg = node.ArgumentList.Arguments[0];
                    if (arg.Expression.IsKind(SyntaxKind.RangeExpression) && arg.Expression is RangeExpressionSyntax range)
                    {
                        var argType = visitor.Global.TryGetTypeSymbol(arg, visitor);
                        if (argType != null)
                        {
                            if (argType.Equals(visitor.Global.SystemRange, SymbolEqualityComparer.Default))
                            {
                                var sliceMethod = (IMethodSymbol?)targetType.GetMembers("Slice").SingleOrDefault(e => e is IMethodSymbol m && m.Parameters.Count() == 2 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32 && m.Parameters[1].Type.SpecialType == SpecialType.System_Int32) ??
                                     (IMethodSymbol?)targetType.GetMembers("Substring").SingleOrDefault(e => e is IMethodSymbol m && m.Parameters.Count() == 2 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32 && m.Parameters[1].Type.SpecialType == SpecialType.System_Int32);
                                var lenGetProperty = targetType.GetMembers("Length").FirstOrDefault(e => e.Kind == SymbolKind.Property) ??
                                    targetType.GetMembers("Count").FirstOrDefault(e => e.Kind == SymbolKind.Property);
                                if (sliceMethod != null && lenGetProperty != null)
                                {
                                    //if (true || range.RightOperand.IsKind(SyntaxKind.IdentifierName) || range.RightOperand is LiteralExpressionSyntax literal)
                                    //{
                                    var lhsVariable = $"$t{++visitor.CurrentTypeWriter.CurrentClosure.NameManglingSeed}";
                                    CodeNode expressionNode = new CodeNode(() =>
                                    {
                                        visitor.CurrentTypeWriter.Write(node, lhsVariable);
                                    });
                                    CodeNode leftOperand = range.LeftOperand;
                                    CodeNode rightOperand = range.RightOperand;
                                    visitor.CurrentTypeWriter.InsertAbove(node, () =>
                                    {
                                        visitor.CurrentTypeWriter.Write(node, "let ");
                                        visitor.CurrentTypeWriter.Write(node, lhsVariable);
                                        visitor.CurrentTypeWriter.Write(node, " = ");
                                        visitor.Visit(node.Expression);
                                        visitor.CurrentTypeWriter.Write(node, ";");
                                    }, true);
                                    using (ImplicitConversionSyntaxEmitter.Disable(range.LeftOperand))
                                    {
                                        using (ImplicitConversionSyntaxEmitter.Disable(range.RightOperand))
                                        {
                                            visitor.WriteMethodInvocation(node, sliceMethod, null, [new CodeNode(() => 
                                            {
                                                if (range.LeftOperand != null)
                                                {
                                                    if (range.LeftOperand.IsKind(SyntaxKind.IndexExpression))
                                                    {
                                                        visitor.WriteMemberAccess(node, expressionNode, targetType, null, lenGetProperty);
                                                        //bool isStaticCall = lenGetProperty.IsStaticCallConvention(visitor.Global);
                                                        //if (!isStaticCall)
                                                        //{
                                                        //    visitor.VisitNode(expressionNode);
                                                        //    visitor.CurrentTypeWriter.Write(node, ".");
                                                        //}                                                            //visitor.WriteMemberName(node, type, lenGetProperty, thisExpression: isStaticCall ? new CodeNode(() =>
                                                        //{
                                                        //    visitor.VisitNode(expressionNode);
                                                        //    //visitor.CurrentTypeWriter.Write(node, $"({sourceName}");
                                                        //}) : null);
                                                        //visitor.WriteMemberName(node, type, lenGetProperty);
                                                        visitor.CurrentTypeWriter.Write(node, " - ");
                                                        visitor.Visit(((PrefixUnaryExpressionSyntax)range.LeftOperand).Operand);
                                                    }
                                                    else
                                                    {
                                                        visitor.VisitNode(leftOperand);
                                                    }
                                                }
                                                else
                                                {
                                                    visitor.CurrentTypeWriter.Write(node, "0");
                                                }
                                            }), new CodeNode(() => 
                                            {
                                                if (range.RightOperand == null ||
                                                    range.RightOperand.IsKind(SyntaxKind.IndexExpression))
                                                {
                                                    visitor.VisitNode(expressionNode);
                                                    visitor.CurrentTypeWriter.Write(node, ".");
                                                    visitor.WriteMemberName(node, targetType, lenGetProperty);
                                                    if (range.RightOperand is PrefixUnaryExpressionSyntax iee)
                                                    {
                                                        visitor.CurrentTypeWriter.Write(node, " - ");
                                                        visitor.Visit(iee.Operand);
                                                    }
                                                }
                                                else
                                                {
                                                    visitor.VisitNode(rightOperand);
                                                }
                                                if (range.LeftOperand != null)
                                                {
                                                    visitor.CurrentTypeWriter.Write(node, " - ");
                                                    visitor.VisitNode(leftOperand);
                                                }
                                            })], expressionNode, targetType, null, false);
                                        }
                                    }
                                    //}
                                    //else
                                    //{
                                    //    visitor.WrapStatementsInExpression(node, () =>
                                    //    {
                                    //        visitor.CurrentTypeWriter.Write(node, $"var $s = ", true);
                                    //        visitor.Visit(node.Expression);
                                    //        visitor.CurrentTypeWriter.WriteLine(node, $";");

                                    //        visitor.CurrentTypeWriter.Write(node, $"var $i = ", true);
                                    //        visitor.Visit(arg);
                                    //        visitor.CurrentTypeWriter.Write(node, ".");
                                    //        visitor.WriteMemberName(node, visitor.Global.SystemRange, "GetOffsetAndLength");
                                    //        visitor.CurrentTypeWriter.Write(node, $"($s.");
                                    //        visitor.WriteMemberName(node, targetType, lenGetProperty);
                                    //        visitor.CurrentTypeWriter.WriteLine(node, $");");

                                    //        var source = SyntaxFactory.IdentifierName("$s");
                                    //        var index = SyntaxFactory.IdentifierName("$i");
                                    //        var start = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, index, SyntaxFactory.IdentifierName("Item1"));
                                    //        var length = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, index, SyntaxFactory.IdentifierName("Item2"));
                                    //        var disposeSource = visitor.CurrentClosure.DefineIdentifierType("$s", CodeSymbol.From(new GeneratedLocalSymbol(targetType, "$s")));
                                    //        var disposeIndex = visitor.CurrentClosure.DefineIdentifierType("$i", CodeSymbol.From(new GeneratedLocalSymbol(visitor.Global.Compilation.CreateTupleTypeSymbol([visitor.Global.SystemInt32, visitor.Global.SystemInt32]), "$i")));
                                    //        visitor.CurrentTypeWriter.Write(node, $"return ", true);
                                    //        visitor.WriteMethodInvocation(node, sliceMethod, null, [start, length], source, targetType, null, false);
                                    //        visitor.CurrentTypeWriter.WriteLine(node, $";");
                                    //        disposeSource.Dispose();
                                    //        disposeIndex.Dispose();
                                    //    });
                                    //}
                                    //visitor.CurrentTypeWriter.WriteLine(node, $"/*{node}*/ {visitor.Global.GlobalName}.{Constants.Expression}(function()");
                                    //visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                                    //visitor.CurrentTypeWriter.Write(node, $"var $s = ", true);
                                    //visitor.Visit(node.Expression);
                                    //visitor.CurrentTypeWriter.WriteLine(node, $";");

                                    //visitor.CurrentTypeWriter.Write(node, $"var $i = ", true);
                                    //visitor.Visit(arg);
                                    //visitor.CurrentTypeWriter.Write(node, ".");
                                    //visitor.WriteMemberName(node, range, "GetOffsetAndLength");
                                    //visitor.CurrentTypeWriter.Write(node, $"($s.");
                                    //visitor.WriteMemberName(node, dSpan, "Length");
                                    //visitor.CurrentTypeWriter.WriteLine(node, $");");

                                    //var source = SyntaxFactory.IdentifierName("$s");
                                    //var index = SyntaxFactory.IdentifierName("$i");
                                    //var start = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, index, SyntaxFactory.IdentifierName("Item1"));
                                    //var length = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, index, SyntaxFactory.IdentifierName("Item2"));
                                    //var disposeSource = visitor.CurrentClosure.DefineIdentifierType("$s", CodeSymbol.From(new GeneratedLocalSymbol(dSpan, "$s")));
                                    //var disposeIndex = visitor.CurrentClosure.DefineIdentifierType("$i", CodeSymbol.From(new GeneratedLocalSymbol(visitor.Global.Compilation.CreateTupleTypeSymbol([sint, sint]), "$i")));
                                    //visitor.CurrentTypeWriter.Write(node, $"return ", true);
                                    //visitor.WriteMethodInvocation(node, sliceMethod, null, [start, length], source, dSpan, null, false);
                                    //visitor.CurrentTypeWriter.WriteLine(node, $";");
                                    //disposeSource.Dispose();
                                    //disposeIndex.Dispose();
                                    //visitor.CurrentTypeWriter.Write(node, "})", true);
                                    return true;
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
