using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    //Handle likes of "double d = 1.0; d is 1u" or "ReadOnlySpan<char> sp = "hello"; sp is "hello""
    sealed class IsLiteralSyntaxEmitter : SyntaxEmitter<IsPatternExpressionSyntax>
    {
        public override bool TryEmit(IsPatternExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Pattern.IsKind(SyntaxKind.ConstantPattern) && ((ConstantPatternSyntax)node.Pattern).Expression is LiteralExpressionSyntax lit)
            {
                var left = node.Expression;
                var right = lit;
                var leftOperandType = visitor.Global.TryGetTypeSymbol(left, visitor);
                var rightOperandType = visitor.Global.TryGetTypeSymbol(right, visitor);
                if (leftOperandType != null && rightOperandType != null && !SymbolEqualityComparer.Default.Equals(leftOperandType, rightOperandType))
                {
                    if (leftOperandType.IsNullable(out var t))
                        leftOperandType = t!;
                    var leftOperators = leftOperandType.GetMembers(TranslatorSyntaxVisitor.ImplicitOperatorName, visitor.Global, false)
                        .Cast<IMethodSymbol>()
                        .ToList();
                    var rightOperators = rightOperandType.GetMembers(TranslatorSyntaxVisitor.ImplicitOperatorName, visitor.Global, false)
                        .Cast<IMethodSymbol>()
                        .ToList();
                    var rightToLeftConverter = leftOperators.SingleOrDefault(e =>
                        e.ReturnType.Equals(leftOperandType, SymbolEqualityComparer.Default) &&
                        e.Parameters.First().Type.Equals(rightOperandType, SymbolEqualityComparer.Default))
                        ??
                        rightOperators.SingleOrDefault(e =>
                        e.ReturnType.Equals(leftOperandType, SymbolEqualityComparer.Default) &&
                        e.Parameters.First().Type.Equals(rightOperandType, SymbolEqualityComparer.Default));
                    var leftToRightConverter = leftOperators.SingleOrDefault(e =>
                        e.ReturnType.Equals(rightOperandType, SymbolEqualityComparer.Default) &&
                        e.Parameters.First().Type.Equals(leftOperandType, SymbolEqualityComparer.Default))
                        ??
                        rightOperators.SingleOrDefault(e =>
                        e.ReturnType.Equals(rightOperandType, SymbolEqualityComparer.Default) &&
                        e.Parameters.First().Type.Equals(leftOperandType, SymbolEqualityComparer.Default));

                    //ReadOnlySpan<char> to string conversion is special cased as it wont work if we call the operator method as it compares the references of the two spans instead of their content
                    if (leftToRightConverter == null &&
                        leftOperandType.IsType("System.ReadOnlySpan<>", true) &&
                        leftOperandType is INamedTypeSymbol nt &&
                        SymbolEqualityComparer.Default.Equals(nt.TypeArguments[0], visitor.Global.SystemChar) &&
                        SymbolEqualityComparer.Default.Equals(rightOperandType, visitor.Global.SystemString))
                    {
                        var toString = leftOperandType.GetMembers("ToString").Cast<IMethodSymbol>().First(c => c.Parameters.Length == 0);
                        visitor.WriteMethodInvocation(node, toString, null, [], left, leftOperandType);
                        //visitor.Visit(left);
                        //visitor.CurrentTypeWriter.Write(node, ".");
                        //visitor.CurrentTypeWriter.Write(node, "ToString()");
                        visitor.CurrentTypeWriter.Write(node, " == ");
                        visitor.Visit(right);
                        return true;
                    }
                    else if (leftToRightConverter != null)
                    {
                        CodeNode leftToRight = new CodeNode(() =>
                        {
                            visitor.WriteMethodInvocation(node, leftToRightConverter, null, [left], null, rightOperandType);
                        });
                        visitor.WriteCompareEquals(node, rightOperandType, leftToRight, right);
                        return true;
                    }
                    else if (rightToLeftConverter != null)
                    {
                        CodeNode rightToLeft = new CodeNode(() =>
                        {
                            visitor.WriteMethodInvocation(node, rightToLeftConverter, null, [right], null, leftOperandType);
                        });
                        visitor.WriteCompareEquals(node, leftOperandType, left, rightToLeft);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
