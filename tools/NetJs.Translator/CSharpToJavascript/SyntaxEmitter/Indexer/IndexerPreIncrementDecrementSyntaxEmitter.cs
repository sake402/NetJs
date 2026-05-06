using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Indexer
{
    //Handles like of ++array[1], --array[1]
    sealed class IndexerPreIncrementDecrementSyntaxEmitter : SyntaxEmitter<PrefixUnaryExpressionSyntax>
    {
        public override bool TryEmit(PrefixUnaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if ((node.IsKind(SyntaxKind.PreIncrementExpression) || node.IsKind(SyntaxKind.PreDecrementExpression)) &&
                node.Operand is ElementAccessExpressionSyntax elementAccess)
            {
                var boundedTo = visitor.Global.ResolveSymbol(visitor.GetExpressionBoundTarget(node.Operand), visitor);
                if (boundedTo is IPropertySymbol property && property.IsIndexer && property.GetMethod != null && property.SetMethod != null)
                {
                    visitor.WrapStatementsInExpression(node, () =>
                    {
                        visitor.CurrentTypeWriter.Write(node, "let $new = ", true);
                        visitor.WriteMethodInvocation(node, property.GetMethod, elementAccess, [
                            //Index
                            ..elementAccess.ArgumentList.Arguments,
                        ], elementAccess.Expression, null);
                        visitor.CurrentTypeWriter.Write(node, node.IsKind(SyntaxKind.PreIncrementExpression) ? " + 1" : " - 1");
                        visitor.CurrentTypeWriter.WriteLine(node, ";");
                        visitor.CurrentTypeWriter.Write(node, "", true);
                        visitor.WriteMethodInvocation(node, property.SetMethod, elementAccess, [
                            //Index
                            ..elementAccess.ArgumentList.Arguments,
                            //Value
                            new CodeNode(() =>
                            {
                                visitor.CurrentTypeWriter.Write(node, "$new");
                            }),
                        ], elementAccess.Expression, null);
                        visitor.CurrentTypeWriter.WriteLine(node, ";");
                        visitor.CurrentTypeWriter.WriteLine(node, "return $new;", true);
                    });
                    return true;
                }
            }
            return false;
        }
    }
}
