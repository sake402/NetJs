using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Indexer
{
    //Handles like of array[1]++, array[1]--

    sealed class IndexerPostIncrementDecrementSyntaxEmitter : SyntaxEmitter<PostfixUnaryExpressionSyntax>
    {
        public override bool TryEmit(PostfixUnaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if ((node.IsKind(SyntaxKind.PostIncrementExpression) || node.IsKind(SyntaxKind.PostDecrementExpression)) &&
                node.Operand is ElementAccessExpressionSyntax elementAccess)
            {
                var boundedTo = visitor.Global.ResolveSymbol(visitor.GetExpressionBoundTarget(node.Operand), visitor);
                if (boundedTo is IPropertySymbol property && property.IsIndexer && property.GetMethod != null && property.SetMethod != null)
                {
                    var varName = $"$t{++visitor.CurrentTypeWriter.CurrentClosure.NameManglingSeed}";
                    void Emit(bool tab)
                    {
                        visitor.CurrentTypeWriter.Write(node, $"let {varName} = ", tab);
                        visitor.WriteMethodInvocation(node, property.GetMethod, null, [
                            //Index
                            ..elementAccess.ArgumentList.Arguments,
                        ], elementAccess.Expression, null);
                        visitor.CurrentTypeWriter.WriteLine(node, ";");
                        visitor.CurrentTypeWriter.Write(node, "", true);
                        visitor.WriteMethodInvocation(node, property.SetMethod, null, [
                            //Index
                            ..elementAccess.ArgumentList.Arguments,
                            //Value
                            new CodeNode(() =>
                            {
                                visitor.CurrentTypeWriter.Write(node, varName);
                                visitor.CurrentTypeWriter.Write(node,node.IsKind(SyntaxKind.PostIncrementExpression)? " + 1" : " - 1");
                            }),
                        ], elementAccess.Expression, null);
                    }
                    if (node.Parent.IsKind(SyntaxKind.ExpressionStatement))
                    {
                        Emit(false);
                    }
                    else
                    {
                        visitor.WrapStatementsInExpression(node, () =>
                        {
                            Emit(true);
                            visitor.CurrentTypeWriter.WriteLine(node, ";");
                            visitor.CurrentTypeWriter.WriteLine(node, $"return {varName};", true);
                        });
                    }
                    //visitor.WriteMethodInvocation(node, property.SetMethod, elementAccess, [
                    //    //Index
                    //    ..elementAccess.ArgumentList.Arguments,
                    //    //Value
                    //    new CodeNode(() =>
                    //    {
                    //        visitor.WriteMethodInvocation(node, property.GetMethod, elementAccess, [
                    //            //Index
                    //            ..elementAccess.ArgumentList.Arguments,
                    //        ], elementAccess.Expression, null);
                    //        visitor.Writer.Write(node,node.IsKind(SyntaxKind.PostIncrementExpression)? " + 1" : " - 1");
                    //    }),
                    //], elementAccess.Expression, null);
                    return true;
                }
            }
            return false;
        }
    }
}
