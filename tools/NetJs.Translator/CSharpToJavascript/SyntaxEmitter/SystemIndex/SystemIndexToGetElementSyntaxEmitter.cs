using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.SystemIndex
{
    //Handle ArrayLike[^1] syntax. ArrayLike can be eg array, string ...
    //Rewrite as ArrayLike[ArrayLike.Lenght-1]
    sealed class SystemIndexToGetElementSyntaxEmitter : SyntaxEmitter<ElementAccessExpressionSyntax>
    {
        public override bool TryEmit(ElementAccessExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var targetType = visitor.Global.TryGetTypeSymbol(node.Expression, visitor);
            if (targetType != null)
            {
                if (node.ArgumentList.Arguments.Count == 1)
                {
                    var arg = node.ArgumentList.Arguments[0];
                    var argType = visitor.Global.TryGetTypeSymbol(arg, visitor);
                    if (argType != null)
                    {
                        if (argType.Equals(visitor.Global.SystemIndex, SymbolEqualityComparer.Default))
                        {
                            var prefixIndex = arg.Expression as PrefixUnaryExpressionSyntax;
                            var identifierIndex = arg.Expression as IdentifierNameSyntax;
                            var indexGetMethod = ((IPropertySymbol)targetType
                                .GetMembers("this[]", visitor.Global)
                                //TODO: First? What if we have more than one that matched the predicate
                                //We expect the ones in defived type to be first in this list though
                                .First(e => e is IPropertySymbol m && m.Parameters.Count() == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32))
                                .GetMethod;
                            if (indexGetMethod != null)
                            {
                                var lenGetProperty = ((IPropertySymbol)(targetType.GetMembers("Length", visitor.Global).SingleOrDefault() ?? targetType.GetMembers("Count", visitor.Global).Single()));
                                if (lenGetProperty != null)
                                {
                                    bool isStaticCall = lenGetProperty.IsStaticCallConvention(visitor.Global);
                                    bool hasTemplate = lenGetProperty.GetMethod?.GetTemplateAttribute(visitor.Global, visitor) != null;
                                    //if (true)
                                    //{
                                    CodeNode expressionNode;
                                    if (node.Expression.IsKind(SyntaxKind.IdentifierName))
                                    {
                                        expressionNode = node.Expression;
                                    }
                                    else
                                    {
                                        var lhsVariable = $"$t{++visitor.CurrentTypeWriter.CurrentClosure.NameManglingSeed}";
                                        visitor.CurrentTypeWriter.InsertAbove(node, () =>
                                        {
                                            visitor.CurrentTypeWriter.Write(node, "let ");
                                            visitor.CurrentTypeWriter.Write(node, lhsVariable);
                                            visitor.CurrentTypeWriter.Write(node, " = ");
                                            visitor.Visit(node.Expression);
                                            visitor.CurrentTypeWriter.Write(node, ";");
                                        }, true);
                                        expressionNode = new CodeNode(() =>
                                        {
                                            visitor.CurrentTypeWriter.Write(node, lhsVariable);
                                        });
                                    }
                                    visitor.WriteMethodInvocation(node, indexGetMethod, null, [new CodeNode(() => {
                                        //if (!isStaticCall && !hasTemplate)
                                        //{
                                        //    visitor.VisitNode(expressionNode);
                                        //    visitor.CurrentTypeWriter.Write(node, ".");
                                        //}
                                        //visitor.WriteMemberName(node, targetType, lenGetProperty, thisExpression: expressionNode, isGet:true);
                                        if (prefixIndex != null)
                                        {
                                            visitor.WriteMemberAccess(node, expressionNode, targetType, null, lenGetProperty);
                                            visitor.CurrentTypeWriter.Write(node, " - ");
                                            visitor.Visit(prefixIndex.Operand);
                                        }
                                        else
                                        {
                                            //visitor.Visit(identifierIndex);
                                            var getOffset = (IMethodSymbol)visitor.Global.SystemIndex.GetMembers("GetOffset").Single();
                                            visitor.WriteMethodInvocation(node, getOffset, null,[new CodeNode(() => {
                                                visitor.WriteMemberAccess(node, expressionNode, targetType, null, lenGetProperty);
                                            })], identifierIndex, null);
                                        }
                                    })], expressionNode, targetType, null, false);
                                    //}
                                    //else
                                    //{
                                    //    const string sourceName = "$s";
                                    //    const string indexName = "$i";

                                    //    visitor.WrapStatementsInExpression(node, () =>
                                    //    {
                                    //        visitor.CurrentTypeWriter.Write(node, $"var {sourceName} = ", true);
                                    //        visitor.Visit(node.Expression);
                                    //        visitor.CurrentTypeWriter.WriteLine(node, $";");

                                    //        visitor.CurrentTypeWriter.Write(node, $"var {indexName} = ", true);
                                    //        visitor.Visit(arg);
                                    //        visitor.CurrentTypeWriter.Write(node, ".");
                                    //        visitor.WriteMemberName(node, visitor.Global.SystemIndex, "GetOffset");
                                    //        bool isStaticCall = lenGetProperty.IsStaticCallConvention(visitor.Global);
                                    //        if (!isStaticCall)
                                    //        {
                                    //            visitor.CurrentTypeWriter.Write(node, $"({sourceName}.");
                                    //        }
                                    //        visitor.WriteMemberName(node, targetType, lenGetProperty, thisExpression: isStaticCall ? new CodeNode(() =>
                                    //        {
                                    //            visitor.CurrentTypeWriter.Write(node, $"({sourceName}");
                                    //        }) : null);
                                    //        visitor.CurrentTypeWriter.WriteLine(node, $");");

                                    //        var source = SyntaxFactory.IdentifierName(sourceName);
                                    //        var index = SyntaxFactory.IdentifierName(indexName);
                                    //        var disposeSource = visitor.CurrentClosure.DefineIdentifierType(sourceName, CodeSymbol.From(new GeneratedLocalSymbol(targetType, sourceName)));
                                    //        var disposeIndex = visitor.CurrentClosure.DefineIdentifierType(indexName, CodeSymbol.From(new GeneratedLocalSymbol(visitor.Global.SystemInt32, indexName)));
                                    //        visitor.CurrentTypeWriter.Write(node, $"return ", true);
                                    //        visitor.WriteMethodInvocation(node, indexGetMethod, null, [index], source, targetType, null, false);
                                    //        visitor.CurrentTypeWriter.WriteLine(node, $";");
                                    //        disposeSource.Dispose();
                                    //        disposeIndex.Dispose();
                                    //    });
                                    //}
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
