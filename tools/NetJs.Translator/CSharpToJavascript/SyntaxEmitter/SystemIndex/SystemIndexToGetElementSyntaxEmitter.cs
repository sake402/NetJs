using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.SystemIndex
{
    //Handle ArrayLike[^1] syntax. ArrayLike can be eg array, string ...
    //Rewrite as ArrayLike[ArrayLike.Lenght-2]
    sealed class SystemIndexToGetElementSyntaxEmitter : SyntaxEmitter<ElementAccessExpressionSyntax>
    {
        public override bool TryEmit(ElementAccessExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var targetType = visitor.Global.ResolveSymbol(visitor.GetExpressionReturnSymbol(node.Expression), visitor)?.GetTypeSymbol();
            if (targetType != null)
            {
                if (node.ArgumentList.Arguments.Count == 1)
                {
                    var arg = node.ArgumentList.Arguments[0];
                    var argType = visitor.Global.ResolveSymbol(visitor.GetExpressionReturnSymbol(arg), visitor)?.GetTypeSymbol();
                    if (argType != null)
                    {
                        var indexType = (ITypeSymbol)visitor.Global.GetTypeSymbol("System.Index", visitor);
                        if (argType.Equals(indexType, SymbolEqualityComparer.Default))
                        {
                            var sint = (ITypeSymbol)visitor.Global.GetTypeSymbol("System.Int32", visitor);
                            var indexGetMethod = ((IPropertySymbol)targetType
                                .GetMembers("this[]", visitor.Global)
                                //TODO: First? What if we have more that matched the predicate
                                //We expect the ones in defived type to be first in this list thought
                                .First(e => e is IPropertySymbol m && m.Parameters.Count() == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32))
                                .GetMethod;
                            if (indexGetMethod != null)
                            {
                                const string sourceName = "$s";
                                const string indexName = "$i";

                                visitor.WrapStatementsInExpression(node, () =>
                                {
                                    visitor.CurrentTypeWriter.Write(node, $"var {sourceName} = ", true);
                                    visitor.Visit(node.Expression);
                                    visitor.CurrentTypeWriter.WriteLine(node, $";");

                                    visitor.CurrentTypeWriter.Write(node, $"var {indexName} = ", true);
                                    visitor.Visit(arg);
                                    visitor.CurrentTypeWriter.Write(node, ".");
                                    visitor.WriteMemberName(node, indexType, "GetOffset");
                                    var member = targetType.GetMembers("Length", visitor.Global).SingleOrDefault() ?? targetType.GetMembers("Count", visitor.Global).Single();
                                    bool isStaticCall = member.IsStaticCallConvention(visitor.Global);
                                    if (!isStaticCall)
                                    {
                                        visitor.CurrentTypeWriter.Write(node, $"({sourceName}.");
                                    }
                                    visitor.WriteMemberName(node, targetType, member, _this: isStaticCall ? new CodeNode(() =>
                                    {
                                        visitor.CurrentTypeWriter.Write(node, $"({sourceName}");
                                    }) : null);
                                    visitor.CurrentTypeWriter.WriteLine(node, $");");

                                    var source = SyntaxFactory.IdentifierName(sourceName);
                                    var index = SyntaxFactory.IdentifierName(indexName);
                                    var disposeSource = visitor.CurrentClosure.DefineIdentifierType(sourceName, CodeSymbol.From(new GeneratedLocalSymbol(targetType, sourceName)));
                                    var disposeIndex = visitor.CurrentClosure.DefineIdentifierType(indexName, CodeSymbol.From(new GeneratedLocalSymbol(sint, indexName)));
                                    visitor.CurrentTypeWriter.Write(node, $"return ", true);
                                    visitor.WriteMethodInvocation(node, indexGetMethod, null, [index], source, targetType, null, false);
                                    visitor.CurrentTypeWriter.WriteLine(node, $";");
                                    disposeSource.Dispose();
                                    disposeIndex.Dispose();
                                });
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
