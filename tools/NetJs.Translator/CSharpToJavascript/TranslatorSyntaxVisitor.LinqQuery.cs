using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {        
        public override void VisitQueryExpression(QueryExpressionSyntax node)
        {
            CurrentTypeWriter.WriteLine(node, "dotnetJs.Expression(function()");
            CurrentTypeWriter.WriteLine(node, "{", true);
            CurrentTypeWriter.WriteLine(node, "let $ret = [];", true);
            Visit(node.FromClause);
            VisitChildren(node.ChildNodes().Except([node.FromClause]));
            CurrentTypeWriter.WriteLine(node, "return $ret;", true);
            CurrentTypeWriter.Write(node, "}.bind(this))", true);
            //base.VisitQueryExpression(node);
        }

        public override void VisitFromClause(FromClauseSyntax node)
        {
            var enumarableName = $"$en{CurrentTypeWriter.ClosureDepth}";
            CurrentTypeWriter.Write(node, $"var {enumarableName} = ", true);
            Visit(node.Expression);
            CurrentTypeWriter.WriteLine(node, ";");
            CurrentTypeWriter.WriteLine(node, $"while ({enumarableName}.MoveNext())", true);
            var children = node.ChildNodes().Except([node.Expression]);
            //bool explicitBlock = false;
            //if (children.Count() == 1 && children.Single() is not BlockSyntax)
            //{
            //    explicitBlock = true;
            //}
            //if (explicitBlock)
            //    Writer.WriteLine("{", true);
            VisitChildren(children);
            //if (explicitBlock)
            //    Writer.WriteLine("}", true);
            //base.VisitFromClause(node);
        }
        
        public override void VisitWhereClause(WhereClauseSyntax node)
        {
            var from = node.FindClosestParent<QueryExpressionSyntax>()!.FromClause;
            CurrentTypeWriter.Write(node, "Enumerable.Where(");
            VisitNode(from.Expression);
            CurrentTypeWriter.Write(node, ", ");
            Visit(node.Condition);
            CurrentTypeWriter.WriteLine(node, ")");
            //CurrentTypeWriter.Write(node, "if (", true);
            //Visit(node.Condition);
            //CurrentTypeWriter.WriteLine(node, ")");
            //base.VisitWhereClause(node);
        }

        public override void VisitSelectClause(SelectClauseSyntax node)
        {
            CurrentTypeWriter.Write(node, $"$ret.Push(", true);
            Visit(node.Expression);
            CurrentTypeWriter.WriteLine(node, ");");
            //base.VisitSelectClause(node);
        }

        public override void VisitQueryBody(QueryBodySyntax node)
        {
            var enumarableName = $"$en{CurrentTypeWriter.ClosureDepth}";
            var query = (QueryExpressionSyntax)node.Parent!;
            CurrentTypeWriter.WriteLine(node, "{", true);
            CurrentTypeWriter.WriteLine(node, $"var {query.FromClause.Identifier.ValueText/*.ToFullString().Trim()*/} = {enumarableName}.Current;", true);
            VisitChildren(node.ChildNodes());
            CurrentTypeWriter.WriteLine(node, "}", true);
            //base.VisitQueryBody(node);
        }

        public override void VisitOrderByClause(OrderByClauseSyntax node)
        {
            var from = node.FindClosestParent<QueryExpressionSyntax>()!.FromClause;
            CurrentTypeWriter.Write(node, "Enumerable.OrderBy(");
            VisitNode(from.Expression);
            CurrentTypeWriter.Write(node, ", ");
            //Visit(node.Condition);
            CurrentTypeWriter.WriteLine(node, ")");
            //base.VisitOrderByClause(node);
        }
    }
}
