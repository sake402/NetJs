using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.AssignmentConverter
{
    //Convert likes of [1,2,4] to ReadOnlySpan<>
    public class CollectionExpressionToReadOnlySpanAssignmentConverter : IAssignmentConverter
    {
        public Type ExpressionType => typeof(CollectionExpressionSyntax);

        public bool CanConvertTo(TranslatorSyntaxVisitor visitor, INamedTypeSymbol lhsType, CSharpSyntaxNode rhsExpression)
        {
            CollectionExpressionSyntax collection = (CollectionExpressionSyntax)rhsExpression;
            var rhsType = (INamedTypeSymbol?)visitor.Global.GetTypeSymbol(collection, visitor/*, out _, out _*/);
            if (rhsType == null)
                return false;
            return lhsType.IsType("System.ReadOnlySpan<>", true) &&
                ((rhsType.IsArray(out var elementType) && SymbolEqualityComparer.Default.Equals(elementType, lhsType.TypeArguments[0])) || (rhsType.IsType("System.ReadOnlySpan<>", true) && SymbolEqualityComparer.Default.Equals(rhsType.TypeArguments[0], lhsType.TypeArguments[0])));
        }

        public void WriteAssignment(TranslatorSyntaxVisitor visitor, INamedTypeSymbol readOnlySpanType, CSharpSyntaxNode rhsExpression)
        {
            var arrayConstructor = readOnlySpanType.GetMembers(".ctor").Cast<IMethodSymbol>().Single(t => t.Parameters.Count() == 1 && t.Parameters[0].Type.IsArray(out _));
            visitor.WriteConstructorCall(rhsExpression, readOnlySpanType, arrayConstructor, null, null, suffixArguments: (Action)(() =>
            {
                visitor.WriteCollectionElementsAsArray((CollectionExpressionSyntax)rhsExpression);
                //visitor.Visit(rhsExpression);
            }));
        }
    }

}
