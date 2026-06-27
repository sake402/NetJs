using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        Dictionary<CSharpSyntaxNode, string> flowJumpLabels { get; } = new Dictionary<CSharpSyntaxNode, string>();
        bool PrepareContinueLabelIfNeccessary(CSharpSyntaxNode node, out string? jumpStart)
        {
            //If a control loop has an inner goto, we must label the loop itself
            //So its own continue can have the right label to continue to
            var loopHasGoto = node.DescendantNodes().Any(c => c.IsKind(SyntaxKind.GotoStatement));
            var loopHasContinue = node.DescendantNodes().Any(c => c.IsKind(SyntaxKind.ContinueStatement));
            jumpStart = null;
            if (loopHasGoto && loopHasContinue)
            {
                string loopPrefix = node.IsKind(SyntaxKind.DoStatement) ? "do" : node.IsKind(SyntaxKind.WhileStatement) ? "while" : "for";
                var manglingSeed = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                jumpStart = $"${loopPrefix}JumpStart{manglingSeed}";
                //Save the jump labels for the continue to use
                flowJumpLabels.Add(node, jumpStart);
            }
            return loopHasGoto && loopHasContinue;
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            bool loopNeedsLabel = PrepareContinueLabelIfNeccessary(node, out var jumStartLabel);
            bool conditionIsAlwaysFalse = _global.EvaluateConditionalExpressionAsConstant(node.Condition, this, out var rewittenCondition) == false;
            if (conditionIsAlwaysFalse)
            {
                CurrentTypeWriter.WriteLine(node, $"//{node.Condition.ToString().Replace("\r", "").Replace("\n", "")} {{ ... }}", true);
                return;
            }
            CurrentTypeWriter.Write(node, $"{(loopNeedsLabel ? $"{jumStartLabel}: " : "")}while(", true);
            Visit(rewittenCondition);
            CurrentTypeWriter.WriteLine(node, ")");
            if (!node.Statement.IsKind(SyntaxKind.Block))
                CurrentTypeWriter.WriteLine(node, "{", true);
            Visit(node.Statement);
            if (!node.Statement.IsKind(SyntaxKind.Block))
                CurrentTypeWriter.WriteLine(node, "}", true);
            //base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            bool loopNeedsLabel = PrepareContinueLabelIfNeccessary(node, out var jumStartLabel);
            var rewittenCondition = node.Condition;
            bool conditionIsAlwaysFalse = false;// _global.EvaluateConditionalExpressionAsConstant(node.Condition, this, out var rewittenCondition) == false;
            CodeLineWriter? doLine = null;
            if (!conditionIsAlwaysFalse)
            {
                doLine = CurrentTypeWriter.WriteLine(node, $"{(loopNeedsLabel ? $"{jumStartLabel}: " : "")}do", true);
                if (!node.Statement.IsKind(SyntaxKind.Block))
                    CurrentTypeWriter.WriteLine(node, "{", true);
            }
            else
            {
                CurrentTypeWriter.WriteLine(node, $"//do {{ ", true);
            }
            Visit(node.Statement);
            if (!conditionIsAlwaysFalse)
            {
                if (!node.Statement.IsKind(SyntaxKind.Block))
                    CurrentTypeWriter.WriteLine(node, "}", true);
                var whileLine = CurrentTypeWriter.Write(node, "while(", true);
                //make us insert before do, any request from while condition
                whileLine.RedirectInsertBefore = doLine;
                Visit(rewittenCondition);
                CurrentTypeWriter.WriteLine(node, ");");
            }
            else
            {
                CurrentTypeWriter.WriteLine(node, $"// }} while({node.Condition.ToString().Replace("\r", "").Replace("\n", "")})", true);
            }
            //base.VisitDoStatement(node);
        }

        public override void VisitBreakStatement(BreakStatementSyntax node)
        {
            string? loopStart = null;
            //List<CSharpSyntaxNode> found = new();
            var parentControl = !node.Parent.IsKind(SyntaxKind.SwitchSection) ? node.FindClosestParent<CSharpSyntaxNode>(isCandidate: (c) =>
            {
                //if (found.Contains(c))
                //return false;
                return c is DoStatementSyntax || c is WhileStatementSyntax || c is ForEachStatementSyntax || c is ForStatementSyntax;
            }) : null;
            if (parentControl != null && flowJumpLabels.TryGetValue(parentControl, out var label))
            {
                loopStart = label;
            }
            CurrentTypeWriter.WriteLine(node, $"break {loopStart};", true);
            base.VisitBreakStatement(node);
        }

        public override void VisitContinueStatement(ContinueStatementSyntax node)
        {
            string? loopStart = null;
            //List<CSharpSyntaxNode> found = new();
            var parentControl = node.FindClosestParent<CSharpSyntaxNode>(isCandidate: (c) =>
            {
                //if (found.Contains(c))
                //return false;
                return c is DoStatementSyntax || c is WhileStatementSyntax || c is ForEachStatementSyntax || c is ForStatementSyntax;
            });
            if (parentControl != null && flowJumpLabels.TryGetValue(parentControl, out var label))
            {
                loopStart = label;
            }
            CurrentTypeWriter.WriteLine(node, $"continue {loopStart};", true);
            base.VisitContinueStatement(node);
        }
        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            var condition = _global.EvaluateConditionalExpressionAsConstant(node.Condition, this, out var rewittenCondition);
            if (condition == true)
            {
                CurrentTypeWriter.Write(node, $"/*{node.Condition} ?*/ ");
                Visit(node.WhenTrue);
                CurrentTypeWriter.Write(node, $"/* : {node.WhenFalse}*/ ");
                return;
            }
            else if (condition == false)
            {
                CurrentTypeWriter.Write(node, $"/*{node.Condition} ? {node.WhenTrue} : */ ");
                Visit(node.WhenFalse);
                return;
            }
            Visit(node.Condition);
            CurrentTypeWriter.Write(node, " ? ");
            if (node.WhenTrue.IsKind(SyntaxKind.ThrowExpression)/* is ThrowExpressionSyntax*/)
            {
                WrapStatementsInExpression(node, () =>
                {
                    Visit(node.WhenTrue);
                });
                //CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.Expression}(");
                //CurrentTypeWriter.Write(node, "function(){ ");
                //Visit(node.WhenTrue);
                //CurrentTypeWriter.Write(node, " }.bind(this))");
            }
            else
            {
                Visit(node.WhenTrue);
            }
            CurrentTypeWriter.Write(node, " : ");
            if (node.WhenFalse.IsKind(SyntaxKind.ThrowExpression)/* is ThrowExpressionSyntax*/)
            {
                WrapStatementsInExpression(node, () =>
                {
                    Visit(node.WhenFalse);
                });
                //CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.Expression}(");
                //CurrentTypeWriter.Write(node, "function(){ ");
                //Visit(node.WhenFalse);
                //CurrentTypeWriter.Write(node, " }.bind(this))");
            }
            else
            {
                Visit(node.WhenFalse);
            }
            //base.VisitConditionalExpression(node);
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            var condition = _global.EvaluateConditionalExpressionAsConstant(node.Condition, this, out var rewittenCondition);
            //bool conditionIsAlwaysFalse = condition == false;
            CodeLineWriter? ifLine = null;
            if (condition == true) //always true
            {
                CurrentTypeWriter.WriteLine(node, $"//if ({node.Condition.ToString().Replace("\r", "").Replace("\n", "")})", !node.Parent.IsKind(SyntaxKind.ElseClause));
                if (!node.Statement.IsKind(SyntaxKind.Block))
                    CurrentTypeWriter.WriteLine(node, "{", true);
                Visit(node.Statement);
                if (!node.Statement.IsKind(SyntaxKind.Block))
                    CurrentTypeWriter.WriteLine(node, "}", true);
            }
            else if (condition == false) //always false
            {
                CurrentTypeWriter.WriteLine(node, $"//if ({node.Condition.ToString().Replace("\r", "").Replace("\n", "")}) {{ ... }}", !node.Parent.IsKind(SyntaxKind.ElseClause));
            }
            else
            {
                ifLine = CurrentTypeWriter.Write(node, $"if (", !node.Parent.IsKind(SyntaxKind.ElseClause)/* is not ElseClauseSyntax*/);
                Visit(rewittenCondition);
                CurrentTypeWriter.WriteLine(node, $")");
                if (!node.Statement.IsKind(SyntaxKind.Block))
                    CurrentTypeWriter.WriteLine(node, "{", true);
                Visit(node.Statement);
                if (!node.Statement.IsKind(SyntaxKind.Block))
                    CurrentTypeWriter.WriteLine(node, "}", true);
            }
            if (node.Else != null)
            {
                if (condition == true)
                {
                    CurrentTypeWriter.WriteLine(node, "//else { ... }", true);
                }
                else
                {
                    bool needsClosingBrace = false;
                    if (node.Else.Statement.IsKind(SyntaxKind.IfStatement)/* is IfStatementSyntax*/)
                    {
                        if (condition == null)
                        {
                            bool elseIfConditionIsAlwaysFalse = _global.EvaluateConditionalExpressionAsConstant(((IfStatementSyntax)node.Else.Statement).Condition, this, out var rewittenElseCondition) == false;
                            if (elseIfConditionIsAlwaysFalse)
                                CurrentTypeWriter.Write(node, "/*else*/ ", true);
                            else
                            {
                                var elseLine = CurrentTypeWriter.Write(node, "else ", true);
                                elseLine.RedirectInsertBefore = ifLine;
                            }
                        }
                        else
                        {
                            CurrentTypeWriter.Write(node, "/*else*/ ", true);
                        }
                    }
                    else
                    {
                        if (condition == null)
                        {
                            var elseLine = CurrentTypeWriter.WriteLine(node, "else ", true);
                            elseLine.RedirectInsertBefore = ifLine;
                            if (!node.Else.Statement.IsKind(SyntaxKind.Block))
                            {
                                CurrentTypeWriter.WriteLine(node, "{", true);
                                needsClosingBrace = true;
                            }
                        }
                        else
                        {
                            CurrentTypeWriter.WriteLine(node, "//else ", true);
                        }
                    }
                    Visit(node.Else);
                    if (needsClosingBrace)
                    {
                        CurrentTypeWriter.WriteLine(node, "}", true);
                    }
                }
            }
            //base.VisitIfStatement(node);
        }

        ISymbol? InferReturnType(CSharpSyntaxNode node)
        {
            ISymbol? returnType = null;
            var localMethod = node.FindClosestParent<LocalFunctionStatementSyntax>();
            var method = node.FindClosestParent<MethodDeclarationSyntax>();
            if (localMethod != null)
            {
                var methodSymbol = (IMethodSymbol)_global.GetSymbol(localMethod, this/*, out _, out _*/);
                returnType = methodSymbol;
            }
            else if (method != null)
            {
                var methodSymbol = (IMethodSymbol)_global.GetSymbol(method, this/*, out _, out _*/);
                returnType = methodSymbol;
            }
            else
            {
                var property = node.FindClosestParent<PropertyDeclarationSyntax>();
                if (property != null)
                {
                    IPropertySymbol? propertySymbol = (IPropertySymbol)_global.GetSymbol(property, this/*, out _, out _*/);
                    returnType = propertySymbol;
                }
                else
                {
                    var indexer = node.FindClosestParent<IndexerDeclarationSyntax>();
                    if (indexer != null)
                    {
                        IPropertySymbol? propertySymbol = (IPropertySymbol)_global.GetSymbol(indexer, this/*, out _, out _*/);
                        returnType = propertySymbol;
                    }
                }
            }
            return returnType;
        }

        public void WriteReturn(CSharpSyntaxNode node, CodeNode? expression, string? cacheKeyVariable = null)
        {
            CurrentTypeWriter.Write(node, expression == null || !expression.IsT0 || !expression.AsT0.IsKind(SyntaxKind.ThrowExpression) ? "return " : "", true);
            if (expression != null)
            {
                var returnType = InferReturnType(node);
                //We try to get the required return type from this expression to make sure we return the right type
                var rhsType = expression.IsT0 ? _global.ResolveSymbol(GetExpressionReturnSymbol(expression.AsT0), this/*, out _, out _*/) : null;
                if (rhsType == null)
                    rhsType = returnType != null ? _global.GetTypeSymbol(returnType) : null;
                returnType ??= rhsType;
                if (cacheKeyVariable != null)
                {
                    CurrentTypeWriter.Write(node, cacheKeyVariable);
                    CurrentTypeWriter.Write(node, " ??= ");
                }
                WriteVariableAssignment(node, null, returnType, null, expression, rhsType);
            }
            else
            {
                //Must always return this in a constructor overload
                var inConstructor = node.FindClosestParent<ConstructorDeclarationSyntax>();
                if (inConstructor != null)
                {
                    CurrentTypeWriter.Write(node, "this");
                }
            }
            CurrentTypeWriter.WriteLine(node, ";");
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            if (node.Expression.IsKind(SyntaxKind.ThrowExpression)/* is ThrowExpressionSyntax*/)
                CurrentTypeWriter.Write(node, "", true);
            else
            {
                if (node.Parent.IsKind(SyntaxKind.ConstructorDeclaration))
                {
                    //Arrow constructor body should not use return as we need to explicitly return this
                }
                else
                {
                    WriteReturn(node, node.Expression);
                    return;
                }
            }
            CurrentTypeWriter.WriteLine(node, "", true);
            Visit(node.Expression);
            //base.VisitArrowExpressionClause(node);
            CurrentTypeWriter.WriteLine(node, ";");
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            WriteReturn(node, node.Expression);
            //base.VisitReturnStatement(node);
        }
    }
}
