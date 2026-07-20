using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.ComponentModel.Design;
using System.Reflection;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.SystemIndex
{
    //Handle ArrayLike[^1] = value syntax. ArrayLike can be eg array, string ...
    sealed class SystemIndexToSetElementSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            bool IsEqualToken()
            {
                return node.OperatorToken.IsKind(SyntaxKind.EqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.PlusEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.MinusEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.PercentEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.AmpersandEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.AsteriskEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.SlashEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.BarEqualsToken) ||
                node.OperatorToken.IsKind(SyntaxKind.CaretEqualsToken);
            }
            if (node.Left is ElementAccessExpressionSyntax elementAccess && IsEqualToken())
            {
                var targetType = visitor.Global.TryGetTypeSymbol(elementAccess.Expression, visitor);
                if (targetType != null)
                {
                    if (elementAccess.ArgumentList.Arguments.Count == 1)
                    {
                        var arg = elementAccess.ArgumentList.Arguments[0];
                        var argType = visitor.Global.TryGetTypeSymbol(arg, visitor);
                        if (argType != null)
                        {
                            //var indexType = (ITypeSymbol)visitor.Global.GetTypeSymbol("System.Index", visitor);
                            if (argType.Equals(visitor.Global.SystemIndex, SymbolEqualityComparer.Default))
                            {
                                var prefixIndex = arg.Expression as PrefixUnaryExpressionSyntax;
                                var identifierIndex = arg.Expression as IdentifierNameSyntax;
                                //var sint = (ITypeSymbol)visitor.Global.GetTypeSymbol("System.Int32", visitor);
                                var indexSetMethod = ((IPropertySymbol)targetType
                                    .GetMembers("this[]", visitor.Global)
                                    //TODO: First? What if we have more that matched the predicate
                                    //We expect the ones in defived type to be first in this list thought
                                    .First(e => e is IPropertySymbol m && m.Parameters.Count() == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32))
                                    .SetMethod;
                                if (indexSetMethod != null)
                                {
                                    var lenGetProperty = ((IPropertySymbol)(targetType.GetMembers("Length", visitor.Global).SingleOrDefault() ?? targetType.GetMembers("Count", visitor.Global).Single()));
                                    if (lenGetProperty != null)
                                    {
                                        bool isStaticCall = lenGetProperty.IsStaticCallConvention(visitor.Global);
                                        bool hasTemplate = lenGetProperty.GetMethod?.GetTemplateAttribute(visitor.Global, visitor) != null;
                                        //if (true)
                                        //{
                                        CodeNode expressionNode;
                                        if (elementAccess.Expression.IsKind(SyntaxKind.IdentifierName))
                                        {
                                            expressionNode = elementAccess.Expression;
                                        }
                                        else
                                        {
                                            var lhsVariable = $"$t{++visitor.CurrentTypeWriter.CurrentClosure.NameManglingSeed}";
                                            visitor.CurrentTypeWriter.InsertAbove(node, () =>
                                            {
                                                visitor.CurrentTypeWriter.Write(node, "let ");
                                                visitor.CurrentTypeWriter.Write(node, lhsVariable);
                                                visitor.CurrentTypeWriter.Write(node, " = ");
                                                visitor.Visit(elementAccess.Expression);
                                                visitor.CurrentTypeWriter.Write(node, ";");
                                            }, true);
                                            expressionNode = new CodeNode(() =>
                                            {
                                                visitor.CurrentTypeWriter.Write(node, lhsVariable);
                                            });
                                        }
                                        visitor.WriteMethodInvocation(node, indexSetMethod, null, [new CodeNode(() => {
                                            //if (!isStaticCall && !hasTemplate)
                                            //{
                                            //    visitor.VisitNode(expressionNode);
                                            //    visitor.CurrentTypeWriter.Write(node, ".");
                                            //}
                                            if (prefixIndex!= null)
                                            {
                                                visitor.WriteMemberAccess(node, expressionNode,targetType, null, lenGetProperty);
                                                //visitor.WriteMemberName(node, targetType, lenGetProperty, thisExpression: expressionNode, isGet:true);
                                                visitor.CurrentTypeWriter.Write(node, " - ");
                                                visitor.Visit(prefixIndex.Operand);
                                            }
                                            else
                                            {
                                                visitor.Visit(identifierIndex);
                                            }
                                        })], expressionNode, targetType, null, false, suffixArguments: node.Right);
                                        //}
                                        //else
                                        //{
                                        //    visitor.WrapStatementsInExpression(node, () =>
                                        //    {
                                        //        var rhsType = visitor.Global.GetTypeSymbol(node.Right, visitor);

                                        //        const string sourceName = "$s";
                                        //        const string indexName = "$i";
                                        //        const string rhsName = "$r";

                                        //        visitor.CurrentTypeWriter.Write(node, $"var {sourceName} = ", true);
                                        //        visitor.Visit(elementAccess.Expression);
                                        //        visitor.CurrentTypeWriter.WriteLine(node, $";");

                                        //        visitor.CurrentTypeWriter.Write(node, $"var {indexName} = ", true);
                                        //        visitor.Visit(arg);
                                        //        visitor.CurrentTypeWriter.Write(node, ".");
                                        //        visitor.WriteMemberName(node, visitor.Global.SystemIndex, "GetOffset");
                                        //        var member = targetType.GetMembers("Length", visitor.Global).SingleOrDefault() ?? targetType.GetMembers("Count", visitor.Global).Single();
                                        //        bool isStaticCall = member.IsStaticCallConvention(visitor.Global);
                                        //        if (!isStaticCall)
                                        //        {
                                        //            visitor.CurrentTypeWriter.Write(node, $"({sourceName}.");
                                        //        }
                                        //        visitor.WriteMemberName(node, targetType, member, thisExpression: isStaticCall ? new CodeNode(() =>
                                        //        {
                                        //            visitor.CurrentTypeWriter.Write(node, $"({sourceName}");
                                        //        }) : null);
                                        //        visitor.CurrentTypeWriter.WriteLine(node, $");");

                                        //        visitor.CurrentTypeWriter.Write(node, $"var {rhsName} = ", true);
                                        //        if (!node.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                                        //        {
                                        //            visitor.Visit(node.Left);
                                        //            visitor.CurrentTypeWriter.Write(node, " ");
                                        //            visitor.CurrentTypeWriter.Write(node, node.OperatorToken.ValueText.Substring(0, node.OperatorToken.ValueText.Length - 1));
                                        //            visitor.CurrentTypeWriter.Write(node, " ");
                                        //        }
                                        //        visitor.Visit(node.Right);
                                        //        visitor.CurrentTypeWriter.WriteLine(node, $";");

                                        //        var source = SyntaxFactory.IdentifierName(sourceName);
                                        //        var index = SyntaxFactory.IdentifierName(indexName);
                                        //        var rhs = SyntaxFactory.IdentifierName(rhsName);
                                        //        var disposeSource = visitor.CurrentClosure.DefineIdentifierType(sourceName, CodeSymbol.From(new GeneratedLocalSymbol(targetType, sourceName)));
                                        //        var disposeIndex = visitor.CurrentClosure.DefineIdentifierType(indexName, CodeSymbol.From(new GeneratedLocalSymbol(visitor.Global.SystemInt32, indexName)));
                                        //        var disposeRhs = visitor.CurrentClosure.DefineIdentifierType(rhsName, CodeSymbol.From(new GeneratedLocalSymbol(rhsType, rhsName)));
                                        //        visitor.CurrentTypeWriter.Write(node, $"", true);
                                        //        visitor.WriteMethodInvocation(node, indexSetMethod, null, [index], source, targetType, null, false, rhs);
                                        //        visitor.CurrentTypeWriter.WriteLine(node, $";");
                                        //        visitor.CurrentTypeWriter.WriteLine(node, $"return {rhsName};", true);
                                        //        disposeSource.Dispose();
                                        //        disposeIndex.Dispose();
                                        //        disposeRhs.Dispose();
                                        //    });

                                        //    //var rhsType = visitor.Global.ResolveSymbol(visitor.GetExpressionReturnSymbol(node.Right), visitor)!.GetTypeSymbol();

                                        //    //const string sourceName = "$s";
                                        //    //const string indexName = "$i";
                                        //    //const string rhsName = "$r";

                                        //    //visitor.CurrentTypeWriter.WriteLine(node, $"/*{node}*/ {visitor.Global.GlobalName}.{Constants.Expression}(function()");
                                        //    //visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                                        //    //visitor.CurrentTypeWriter.Write(node, $"var {sourceName} = ", true);
                                        //    //visitor.Visit(elementAccess.Expression);
                                        //    //visitor.CurrentTypeWriter.WriteLine(node, $";");

                                        //    //visitor.CurrentTypeWriter.Write(node, $"var {indexName} = ", true);
                                        //    //visitor.Visit(arg);
                                        //    //visitor.CurrentTypeWriter.Write(node, ".");
                                        //    //visitor.WriteMemberName(node, indexType, "GetOffset");
                                        //    //var member = targetType.GetMembers("Length", visitor.Global).SingleOrDefault() ?? targetType.GetMembers("Count", visitor.Global).Single();
                                        //    //bool isStaticCall = member.IsStaticCallConvention(visitor.Global);
                                        //    //if (!isStaticCall)
                                        //    //{
                                        //    //    visitor.CurrentTypeWriter.Write(node, $"({sourceName}.");
                                        //    //}
                                        //    //visitor.WriteMemberName(node, targetType, member, _this: isStaticCall ? new CodeNode(() =>
                                        //    //{
                                        //    //    visitor.CurrentTypeWriter.Write(node, $"({sourceName}");
                                        //    //}) : null);
                                        //    //visitor.CurrentTypeWriter.WriteLine(node, $");");

                                        //    //visitor.CurrentTypeWriter.Write(node, $"var {rhsName} = ", true);
                                        //    //if (!node.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                                        //    //{
                                        //    //    visitor.Visit(node.Left);
                                        //    //    visitor.CurrentTypeWriter.Write(node, " ");
                                        //    //    visitor.CurrentTypeWriter.Write(node, node.OperatorToken.ValueText.Substring(0, node.OperatorToken.ValueText.Length - 1));
                                        //    //    visitor.CurrentTypeWriter.Write(node, " ");
                                        //    //}
                                        //    //visitor.Visit(node.Right);
                                        //    //visitor.CurrentTypeWriter.WriteLine(node, $";");

                                        //    //var source = SyntaxFactory.IdentifierName(sourceName);
                                        //    //var index = SyntaxFactory.IdentifierName(indexName);
                                        //    //var rhs = SyntaxFactory.IdentifierName(rhsName);
                                        //    //var disposeSource = visitor.CurrentClosure.DefineIdentifierType(sourceName, CodeSymbol.From(new GeneratedLocalSymbol(targetType, sourceName)));
                                        //    //var disposeIndex = visitor.CurrentClosure.DefineIdentifierType(indexName, CodeSymbol.From(new GeneratedLocalSymbol(sint, indexName)));
                                        //    //var disposeRhs = visitor.CurrentClosure.DefineIdentifierType(rhsName, CodeSymbol.From(new GeneratedLocalSymbol(rhsType, rhsName)));
                                        //    //visitor.CurrentTypeWriter.Write(node, $"", true);
                                        //    //visitor.WriteMethodInvocation(node, indexSetMethod, null, [index], source, targetType, null, false, rhs);
                                        //    //visitor.CurrentTypeWriter.WriteLine(node, $";");
                                        //    //visitor.CurrentTypeWriter.WriteLine(node, $"return {rhsName};", true);
                                        //    //disposeSource.Dispose();
                                        //    //disposeIndex.Dispose();
                                        //    //disposeRhs.Dispose();
                                        //    //visitor.CurrentTypeWriter.Write(node, "}.bind(this))", true);
                                        //}
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
