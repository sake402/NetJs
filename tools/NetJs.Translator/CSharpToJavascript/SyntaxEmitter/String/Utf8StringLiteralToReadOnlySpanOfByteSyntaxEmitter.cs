using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.String
{
    //Convert likes of "ABC"u8 to ReadOnlySpan<byte>
    sealed class Utf8StringLiteralToReadOnlySpanOfByteSyntaxEmitter : SyntaxEmitter<LiteralExpressionSyntax>
    {
        public override bool TryEmit(LiteralExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.Utf8StringLiteralExpression))
            {
                var bytes = node.Token.ValueText.ToArray().Select(e => e & 0xFF);
                int? concatBytesWritten = null;
                if (visitor.States.TryGetValue(nameof(Utf8StringLiteralConcatSyntaxEmitter), out var v))
                {
                    concatBytesWritten = (int)v;
                }
                if (concatBytesWritten != null)
                {
                    int ix = concatBytesWritten.Value;
                    foreach (var b in bytes)
                    {
                        if (ix > 0)
                            visitor.CurrentTypeWriter.Write(node, ", ");
                        visitor.CurrentTypeWriter.Write(node, b.ToString());
                        ix++;
                    }
                    visitor.States[nameof(Utf8StringLiteralConcatSyntaxEmitter)] = ix;
                }
                else
                {
                    var readOnlySpan = (INamedTypeSymbol)visitor.Global.GetSymbol("System.ReadOnlySpan<>", visitor);
                    var ssbyte = (ITypeSymbol)visitor.Global.GetSymbol("System.Byte", visitor);
                    readOnlySpan = readOnlySpan.Construct(ssbyte);
                    var constructor = readOnlySpan.GetMembers(".ctor").Cast<IMethodSymbol>().Single(t => t.Parameters.Count() == 1 && t.Parameters[0].Type.IsArray(out _));
                    visitor.WriteConstructorCall(node, readOnlySpan, constructor, null, [new CodeNode(() =>
                    {
                        visitor.CurrentTypeWriter.Write(node,"[");
                        int ix = 0;
                        foreach (var b in bytes)
                        {
                            if (ix > 0)
                                visitor.CurrentTypeWriter.Write(node,", ");
                            visitor.CurrentTypeWriter.Write(node,b.ToString());
                            ix++;
                        }
                        visitor.CurrentTypeWriter.Write(node,"]");
                    })]);
                }
                return true;
            }
            return false;
        }
    }

    sealed class Utf8StringLiteralConcatSyntaxEmitter : SyntaxEmitter<BinaryExpressionSyntax>
    {
        public override bool TryEmit(BinaryExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if ((node.Left.IsKind(SyntaxKind.Utf8StringLiteralExpression) || node.Right.IsKind(SyntaxKind.Utf8StringLiteralExpression)) && node.IsKind(SyntaxKind.AddExpression))
            {
                int? concatBytesWritten = null;
                if (visitor.States.TryGetValue(nameof(Utf8StringLiteralConcatSyntaxEmitter), out var v))
                {
                    concatBytesWritten = (int)v;
                }
                if (concatBytesWritten != null)
                {
                    visitor.Visit(node.Left);
                    visitor.Visit(node.Right);
                    return true;
                }
                if (concatBytesWritten == null)
                    concatBytesWritten = 0;
                visitor.States[nameof(Utf8StringLiteralConcatSyntaxEmitter)] = concatBytesWritten;
                var readOnlySpan = (INamedTypeSymbol)visitor.Global.GetSymbol("System.ReadOnlySpan<>", visitor);
                var ssbyte = (ITypeSymbol)visitor.Global.GetSymbol("System.Byte", visitor);
                readOnlySpan = readOnlySpan.Construct(ssbyte);
                var constructor = readOnlySpan.GetMembers(".ctor").Cast<IMethodSymbol>().Single(t => t.Parameters.Count() == 1 && t.Parameters[0].Type.IsArray(out _));
                visitor.WriteConstructorCall(node, readOnlySpan, constructor, null, [new CodeNode(() =>
                {
                    visitor.CurrentTypeWriter.Write(node,"[");
                    visitor.Visit(node.Left);
                    visitor.Visit(node.Right);
                    visitor.CurrentTypeWriter.Write(node,"]");
                })]);
                visitor.States.Remove(nameof(Utf8StringLiteralConcatSyntaxEmitter));
                return true;
            }
            return false;
        }
    }
}
