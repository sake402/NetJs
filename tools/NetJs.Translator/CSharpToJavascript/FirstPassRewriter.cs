using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript
{
    /// <summary>
    /// First rewrite phase make sure all blocks are in place. We do this in case we want to insert viariables into the block especially for conditionalaccess rewriting
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
            node = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            node = (IfStatementSyntax)base.VisitIfStatement(node)!;
            //Keep statement in block so that other auto variables we need to define for the statement can remain in the block
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitElseClause(ElseClauseSyntax node)
        {
            node = (ElseClauseSyntax)base.VisitElseClause(node)!;
            if (node.Statement is not BlockSyntax && node.Statement is not IfStatementSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            node = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }

        public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
        {
            node = (UsingStatementSyntax)base.VisitUsingStatement(node)!;
            if (node.Statement is not BlockSyntax)
            {
                var block = WrapInBlock(node.Statement);
                node = node.ReplaceNode(node.Statement, block);
            }
            return node;
        }
    }
}