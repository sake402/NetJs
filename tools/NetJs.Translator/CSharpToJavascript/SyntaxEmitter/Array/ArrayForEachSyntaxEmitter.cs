using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Array
{
    sealed class ArrayForEachSyntaxEmitter : SyntaxEmitter<ForEachStatementSyntax>
    {
        public override bool TryEmit(ForEachStatementSyntax node, TranslatorSyntaxVisitor visitor)
        {
            var type = visitor.Global.GetTypeSymbol(node.Expression, visitor);
            if (type.IsArray(out var elementType))
            {
                var i = ++visitor.CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                var index = $"$i{i}";
                var array = $"$arr{i}";
                visitor.CurrentTypeWriter.WriteLine(node, $"let {index} = 0;", true);
                visitor.CurrentTypeWriter.Write(node, $"let {array} = ", true);
                visitor.Visit(node.Expression);
                visitor.CurrentTypeWriter.WriteLine(node, $";");
                visitor.CurrentTypeWriter.WriteLine(node, $"while ({index} < {array}.length)", true);
                visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                visitor.OpenClosure(node);
                visitor.CurrentTypeWriter.Write(node, $"let ", true);
                visitor.CurrentTypeWriter.Write(node, node.Identifier.ResolveIdentifierName());
                visitor.CurrentTypeWriter.Write(node, $" = ");
                visitor.CurrentTypeWriter.WriteLine(node, $"{array}[{index}++];");
                //visitor.CurrentTypeWriter.WriteLine(node, $"{index}++;", true);
                if (node.Statement.IsKind(SyntaxKind.Block))
                {
                    visitor.VisitChildren(node.Statement.ChildNodes());
                }
                else
                {
                    visitor.Visit(node.Statement);
                }
                visitor.CloseClosure(node);
                visitor.CurrentTypeWriter.WriteLine(node, "}", true);
                return true;
            }
            return false;
        }
    }
}
