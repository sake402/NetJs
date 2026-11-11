using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.SystemIndex
{
    sealed class CreateIndexSyntaxEmitter : SyntaxEmitter<PrefixUnaryExpressionSyntax>
    {
        public override bool TryEmit(PrefixUnaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.IndexExpression))
            {
                var element = node.FindClosestParent<ElementAccessExpressionSyntax>();
                if (element != null && element.ArgumentList.Arguments.Count == 1 && element.ArgumentList.Arguments[0].Expression == node)
                {
                    var targetType = visitor.Global.TryGetTypeSymbol(element.Expression, visitor);
                    if (targetType != null)
                    {
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
                                visitor.WriteMemberAccess(node, element.Expression, targetType, null, lenGetProperty);
                                visitor.CurrentTypeWriter.Write(node, " - ");
                                visitor.Visit(node.Operand);
                                return true;
                            }
                        }
                    }
                }
                visitor.WriteCreateIndexFromEnd(node, node.Operand);
                return true;
            }
            return false;
        }
    }
}
