using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript
{
    /// <summary>
    /// First rewrite phase make sure all blocks are in place. We do this in case we want to insert viariables into the block especially for conditional access rewriting
    /// </summary>
    public partial class FirstPassRewriter : CSharpSyntaxRewriter
    {
        BlockSyntax WrapInBlock(StatementSyntax expression)
        {
            return SyntaxFactory.Block(expression.WithLeadingTrivia(SyntaxFactory.LineFeed)).WithLeadingTrivia(SyntaxFactory.LineFeed).WithTrailingTrivia(SyntaxFactory.LineFeed);
        }
        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            node = (ForStatementSyntax)base.VisitForStatement(node)!;
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            var processed = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;

            return processed.Statement is not BlockSyntax
                ? processed.WithStatement(WrapInBlock(processed.Statement))
                : processed;
        }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            var processed = (IfStatementSyntax)base.VisitIfStatement(node)!;

            return processed.Statement is not BlockSyntax
                ? processed.WithStatement(WrapInBlock(processed.Statement))
                : processed;
        }

        public override SyntaxNode? VisitElseClause(ElseClauseSyntax node)
        {
            var processed = (ElseClauseSyntax)base.VisitElseClause(node)!;

            return (processed.Statement is not BlockSyntax && processed.Statement is not IfStatementSyntax)
                ? processed.WithStatement(WrapInBlock(processed.Statement))
                : processed;
        }

        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            var processed = (WhileStatementSyntax)base.VisitWhileStatement(node)!;

            return processed.Statement is not BlockSyntax
                ? processed.WithStatement(WrapInBlock(processed.Statement))
                : processed;
        }

        public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
        {
            var processed = (UsingStatementSyntax)base.VisitUsingStatement(node)!;

            return processed.Statement is not BlockSyntax
                ? processed.WithStatement(WrapInBlock(processed.Statement))
                : processed;
        }
    }
}