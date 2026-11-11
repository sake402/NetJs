using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref
{
    /// <summary>
    /// Hotpath optimization, especially for json deserilization in ProbabilisticMapState.FastContains
    /// Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(hashEntries), (nuint)offset) => hashEntries[offset]
    /// </summary>
    internal class UnneccessaryUnsafeAddArrayReferenceDereferenceSyntaxEmitter : SyntaxEmitter<InvocationExpressionSyntax>
    {
        public override bool TryEmit(InvocationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (!node.Parent.IsKind(SyntaxKind.RefExpression) && !node.Parent.IsKind(SyntaxKind.Argument))
            {
                if (UnneccessaryUnsafeAddSyntaxEmitter.IsUnsafeAdd(node, visitor, out _) &&
                    node.ArgumentList.Arguments.Count == 2 &&
                    node.ArgumentList.Arguments[0].RefKindKeyword.IsKind(SyntaxKind.RefKeyword) &&
                    node.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.InvocationExpression) &&
                    node.ArgumentList.Arguments[0].Expression is InvocationExpressionSyntax GetArrayDataReference &&
                    GetArrayDataReference.ToString().StartsWith("MemoryMarshal.GetArrayDataReference"))
                {
                    var array = GetArrayDataReference.ArgumentList.Arguments[0].Expression;
                    var offset = node.ArgumentList.Arguments[1].Expression;
                    visitor.Visit(array);
                    visitor.CurrentTypeWriter.Write(node, "[");
                    visitor.Visit(offset);
                    visitor.CurrentTypeWriter.Write(node, "]");
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Hotpath optimization, especially for json deserilization in ProbabilisticMapState.FastContains
    /// ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), start) => RuntimeHelpers.CreateArrayReferenceT<T>(T[] array, int? index = null, bool _checked = false, bool createRef = true)
    /// </summary>
    internal class UnneccessaryUnsafeAddArrayReferenceDereferenceSyntaxEmitter2 : SyntaxEmitter<InvocationExpressionSyntax>
    {
        public override bool TryEmit(InvocationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Parent.IsKind(SyntaxKind.RefExpression))
            {
                if (UnneccessaryUnsafeAddSyntaxEmitter.IsUnsafeAdd(node, visitor, out _) &&
                    node.ArgumentList.Arguments.Count == 2 &&
                    node.ArgumentList.Arguments[0].RefKindKeyword.IsKind(SyntaxKind.RefKeyword) &&
                    node.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.InvocationExpression) &&
                    node.ArgumentList.Arguments[0].Expression is InvocationExpressionSyntax GetArrayDataReference &&
                    GetArrayDataReference.ToString().StartsWith("MemoryMarshal.GetArrayDataReference"))
                {
                    var array = GetArrayDataReference.ArgumentList.Arguments[0].Expression;
                    var offset = node.ArgumentList.Arguments[1].Expression;
                    ITypeSymbol arrayElementType;
                    var arrayType = visitor.Global.GetTypeSymbol(array, visitor);
                    arrayType.IsArray(out arrayElementType);
                    visitor.WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateArrayReferenceT", methodGenericTypes: [arrayElementType], arguments: [
                        array,
                        offset
                    ]);
                    return true;
                }
            }
            return false;
        }
    }
}