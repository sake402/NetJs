using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Delegate
{
    internal class DelegateAddSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                var leftSymbol = visitor.Global.GetSymbol(node.Left, visitor);
                var leftType = visitor.Global.GetTypeSymbol(leftSymbol);
                var rightSymbol = visitor.Global.GetSymbol(node.Right, visitor);
                var rightType = visitor.Global.GetTypeSymbol(rightSymbol);
                if (leftType != null &&
                    rightType != null &&
                    leftType.IsDelegate(out _, out _) &&
                    (rightType.IsDelegate(out _, out _) || rightSymbol.Kind == SymbolKind.Method))
                {
                    if (leftSymbol.Kind == SymbolKind.Local)
                    {
                        var delegateCombineMethod = visitor.Global.SystemDelegate
                            .GetMembers("Combine")
                            .OfType<IMethodSymbol>()
                            .First(m => m.Parameters.Length == 2);
                        visitor.CurrentTypeWriter.Write(node, "", true);
                        visitor.VisitNode(node.Left);
                        visitor.CurrentTypeWriter.Write(node, " = ");
                        visitor.WriteMethodInvocation(node, delegateCombineMethod, null, [node.Left, node.Right], null, null);
                    }
                    else
                    {
                        //var metadata = visitor.Global.GetMetadata(leftSymbol);
                        visitor.Visit(node.Left);
                        visitor.CurrentTypeWriter.Write(node, "$add");
                        visitor.CurrentTypeWriter.Write(node, "(");
                        visitor.Visit(node.Right);
                        visitor.CurrentTypeWriter.Write(node, ")");
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
