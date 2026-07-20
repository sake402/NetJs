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
        void HandleDeclarationPatternInSwitchExpression(DeclarationPatternSyntax node)
        {
            SingleVariableDesignationSyntax? svd = null;
            if (node.Designation is SingleVariableDesignationSyntax isvd)
            {
                svd = isvd;
            }
            if (svd != null)
            {
                CurrentTypeWriter.InsertAbove(node, $"let {svd.Identifier.ResolveIdentifierName()};", true);
                CurrentTypeWriter.Write(node, "(");
                CurrentTypeWriter.Write(node, svd.Identifier.ResolveIdentifierName());
                CurrentTypeWriter.Write(node, $" = ");
                WritePatternExpressionFilter(node);
                CurrentTypeWriter.Write(node, $", ");
            }
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.IsTypeName}(");
            if (svd != null)
            {
                CurrentTypeWriter.Write(node, svd.Identifier.ResolveIdentifierName());
            }
            else
            {
                WritePatternExpressionFilter(node);
            }
            CurrentTypeWriter.Write(node, $", ");
            var type = _global.TryGetTypeSymbol(node.Type, this);
            if (type != null)
                CurrentTypeWriter.Write(node, type.ComputeOutputTypeName(_global));
            else
                Visit(node.Type);
            CurrentTypeWriter.Write(node, $")");
            if (svd != null)
            {
                CurrentTypeWriter.Write(node, ")");
            }
            if (svd != null)
            {
                var localSymbol = _global.TryGetSymbol(svd, this/*, out _, out _*/);
                if (localSymbol != null)
                {
                    CurrentClosure.DefineIdentifierType(svd.Identifier.ValueText, CodeSymbol.From(localSymbol));
                }
                else
                {
                    CurrentClosure.DefineIdentifierType(svd.Identifier.ValueText, CodeSymbol.From(node.Type, SymbolKind.Local));
                    //Writer.Write(node, $", {svd.Identifier.ValueText} = {id}");
                }
            }
        }

        public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
        {
            if (node.Pattern.IsKind(SyntaxKind.DeclarationPattern) || node.Pattern.IsKind(SyntaxKind.VarPattern)) //need a closure to declare the variable into
            {
                OpenClosure(node);
                CurrentTypeWriter.WriteLine(node, "{", true);
            }
            //var governor = node.FindClosest<SwitchExpressionSyntax>()!.GoverningExpression;
            if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern) || node.WhenClause != null)
            {
                CurrentTypeWriter.Write(node, "if ", true);
                if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern))
                {
                    CurrentTypeWriter.Write(node, "(");
                }
                if (node.WhenClause != null)
                {
                    CurrentTypeWriter.Write(node, "(");
                }
            }
            //if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern)) { 
            //    WritePatternExpressionFilter(node);
            //}
            Visit(node.Pattern);
            if (node.WhenClause != null)
            {
                if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern))
                {
                    CurrentTypeWriter.Write(node, ")");
                }
                if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern) && node.WhenClause != null)
                {
                    CurrentTypeWriter.Write(node, " && ");
                }
                Visit(node.WhenClause);
            }
            if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern) || node.WhenClause != null)
            {
                CurrentTypeWriter.WriteLine(node, ")");
                CurrentTypeWriter.WriteLine(node, "{", true);
            }
            if (node.Expression.IsKind(SyntaxKind.ThrowExpression))
            {
                CurrentTypeWriter.Write(node, "", true);
                Visit(node.Expression);
                CurrentTypeWriter.WriteLine(node, ";");
            }
            else
                WriteReturn(node, node.Expression);
            //Writer.Write(node, node.Expression.IsKind(SyntaxKind.ThrowExpression) ? "" : "return ", true);
            //Visit(node.Expression);
            //Writer.WriteLine(node, ";");
            if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern) || node.WhenClause != null)
            {
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            if (node.Pattern.IsKind(SyntaxKind.DeclarationPattern) || node.Pattern.IsKind(SyntaxKind.VarPattern)) //need a closure to declare the variable into
            {
                CloseClosure(node);
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            //base.VisitSwitchExpressionArm(node);
        }

        bool NeedsCachePatternExpressionInTempVariable(ExpressionSyntax syntax)
        {
            if (syntax.IsKind(SyntaxKind.IdentifierName))
            {
                return false;
            }
            var inWhile = syntax.FindClosestParent<WhileStatementSyntax>();
            if (inWhile != null)
            {
                if (inWhile.Condition.DescendantNodes().Contains(syntax))
                    return false;
            }
            var inFor = syntax.FindClosestParent<ForStatementSyntax>();
            if (inFor != null)
            {
                if (inFor.Condition != null && inFor.Condition.DescendantNodes().Contains(syntax))
                    return false;
            }
            return true;
        }

        public override void VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            WrapStatementsInExpression(node, () =>
            {
                OpenClosure(node);
                var switchClosure = CurrentClosure;
                bool needsVar = false;
                if (node.GoverningExpression.IsKind(SyntaxKind.TupleExpression))
                {
                    var tuple = (TupleExpressionSyntax)node.GoverningExpression;
                    switchClosure.SwitchExpressionCacheVariableNames = CacheTupleItemsIntoTempVariableNames(node, tuple, "$switch");
                }
                else
                {
                    needsVar = NeedsCachePatternExpressionInTempVariable(node.GoverningExpression);
                    if (needsVar)
                    {
                        var i = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                        CurrentClosure.SwitchExpressionCacheVariableNames = [$"$switch{i}"];//.Tags.Add(SwitchExpressionVariableName, $"$switch{i}");
                        CurrentTypeWriter.Write(node, $"let $switch{i} = ", true);
                        Visit(node.GoverningExpression);
                        CurrentTypeWriter.WriteLine(node, ";");
                    }
                }
                foreach (var arm in node.Arms)
                {
                    Visit(arm);
                }
                //if (needsVar)
                //{
                //    //CurrentClosure.Tags.Remove(SwitchExpressionVariableName);
                //}
                CloseClosure(node);
            });
            //base.VisitSwitchExpression(node);
        }

    }
}
