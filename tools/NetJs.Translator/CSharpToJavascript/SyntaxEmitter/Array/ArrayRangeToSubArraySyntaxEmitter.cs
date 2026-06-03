using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Array
{
    //Handles like of array[1..3]
    sealed class ArrayRangeToSubArraySyntaxEmitter : SyntaxEmitter<ElementAccessExpressionSyntax>
    {
        public override bool TryEmit(ElementAccessExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.ArgumentList.Arguments.Count == 1 && node.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.RangeExpression))
            {
                var type = visitor.Global.GetTypeSymbol(node.Expression, visitor);
                if (type != null && type.IsArray(out var elementType))
                {
                    visitor.WriteCreateSubArray(node, elementType, node.Expression, node.ArgumentList.Arguments[0].Expression);
                    //var runtimeHelpers = (ITypeSymbol)visitor.Global.GetSymbol("System.Runtime.CompilerServices.RuntimeHelpers", visitor);
                    //var getSubArray = (IMethodSymbol)runtimeHelpers.GetMembers("GetSubArray").Single();
                    //getSubArray = getSubArray.Construct(elementType);
                    //visitor.WriteMethodInvocation(node, getSubArray, null, [node.Expression, .. node.ArgumentList.Arguments], null, runtimeHelpers, null, false);
                    return true;
                }
            }
            return false;
        }
    }
}
