using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref
{
    /// <summary>
    /// An expression like "Unsafe.Add(ref first, index) = value" 
    /// Rewrite as let a = first.Set(value, index), this will be way faster as it doesnt create the temp js reference object returned by Unsafe.Add
    /// </summary>
    internal class UnneccessaryUnsafeAddWriteSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (UnneccessaryUnsafeAddSyntaxEmitter.IsUnsafeAdd(node.Left, visitor, out var inv) &&
                RefTypeDereferenceOnAccessSyntaxEmitter.IsRefTypeToDereference(node.Left, visitor, out var isWrite) && 
                isWrite == true)
            {
                UnneccessaryUnsafeAddSyntaxEmitter.EmitUnsafeAddSet(node, visitor, inv, node.Right);
                return true;
            }
            return false;
        }
    }
}