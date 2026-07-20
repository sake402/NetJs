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
        void HandleDeclarationPatternInSwitchStatement(DeclarationPatternSyntax node)
        {
            SingleVariableDesignationSyntax? svd = null;
            if (node.Designation is SingleVariableDesignationSyntax isvd)
            {
                svd = isvd;
            }
            bool newVariableInserted = false;
            if (svd != null)
            {
                newVariableInserted = CurrentTypeWriter.InsertAbove(node, $"let {svd.Identifier.ResolveIdentifierName()};", true, skipIfAlreadyInserted: true/*multiple identical var declaration can occur in case int b and case short b*/);
                //CurrentTypeWriter.Write(node, "(");
                //CurrentTypeWriter.Write(node, svd.Identifier.ValueText);
                //CurrentTypeWriter.Write(node, $" = ");
                //WritePatternExpressionFilter(node);
                //CurrentTypeWriter.Write(node, $", ");

            }
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.IsTypeName}(");
            //if (svd != null)
            //{
            //    CurrentTypeWriter.Write(node, svd.Identifier.ResolveIdentifierName());
            //}
            //else
            //{
            WritePatternExpressionFilter(node);
            //}
            CurrentTypeWriter.Write(node, $", ");
            var type = _global.TryGetTypeSymbol(node.Type, this);
            if (type != null)
                CurrentTypeWriter.Write(node, type.ComputeOutputTypeName(_global));
            else
                Visit(node.Type);
            if (svd != null)
            {
                CurrentTypeWriter.Write(node, $", {{ set $v(v){{ {svd.Identifier.ResolveIdentifierName()} = v; }} }}");
            }
            CurrentTypeWriter.Write(node, $")");
            if (svd != null)
            {
                //CurrentTypeWriter.Write(node, ")");
            }
            if (newVariableInserted && svd != null)
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

        public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
        {
            var switchStatement = node.FindClosestParent<SwitchStatementSyntax>() ?? throw new InvalidOperationException("Case should be inside a switch");
            if (IsSimpleSwitchCase(switchStatement))
            {
                CurrentTypeWriter.Write(node, "case ", true);
                Visit(node.Value);
                CurrentTypeWriter.WriteLine(node, ":");
            }
            else
            {
                var valueType = _global.GetTypeSymbol(node.Value, this);
                if (valueType.IsType("System.Type"))
                {
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.IsTypeName}(");
                    WritePatternExpressionFilter(node);
                    CurrentTypeWriter.Write(node, $", ");
                    var type = _global.TryGetTypeSymbol(node.Value, this);
                    if (type != null)
                        CurrentTypeWriter.Write(node, type.ComputeOutputTypeName(_global));
                    else
                        Visit(node.Value);
                    CurrentTypeWriter.Write(node, $")");
                }
                else
                {
                    WritePatternExpressionFilter(node);
                    CurrentTypeWriter.Write(node, " == ");
                    Visit(node.Value);
                }
            }
            //base.VisitCaseSwitchLabel(node);
        }

        public override void VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
        {
            var switchStatement = node.FindClosestParent<SwitchStatementSyntax>();
            if (switchStatement != null && !IsSimpleSwitchCase(switchStatement))
            {
                if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern))
                {
                    var type = _global.GetTypeSymbol(node.Pattern, this);
                    if (!type.IsValueType && !node.Pattern.IsKind(SyntaxKind.DeclarationPattern))
                    {
                        WritePatternExpressionFilter(node);
                        CurrentTypeWriter.Write(node, " != null && ");
                    }
                    if (node.Pattern.IsKind(SyntaxKind.OrPattern) && !node.Parent.IsKind(SyntaxKind.OrPattern))
                    {
                        CurrentTypeWriter.Write(node, "(");
                    }
                    Visit(node.Pattern);
                    if (node.Pattern.IsKind(SyntaxKind.OrPattern) && !node.Parent.IsKind(SyntaxKind.OrPattern))
                    {
                        CurrentTypeWriter.Write(node, ")");
                    }
                }
                if (node.WhenClause != null)
                {
                    if (!node.Pattern.IsKind(SyntaxKind.DiscardPattern))
                    {
                        CurrentTypeWriter.Write(node, " && ");
                    }
                    Visit(node.WhenClause);
                }
            }
            else
            {
                CurrentTypeWriter.Write(node, "case ", true);
                Visit(node.Pattern);
                CurrentTypeWriter.WriteLine(node, ":");
            }
        }

        static bool SwitchHasGotoCase(SwitchStatementSyntax node)
        {
            return node.DescendantNodes().Any(c => c.IsKind(SyntaxKind.GotoCaseStatement) || c.IsKind(SyntaxKind.GotoDefaultStatement));
        }

        static bool SwitchCaseHasGotoStatement(SwitchSectionSyntax node)
        {
            //if (node.IsKind(SyntaxKind.GotoStatement))
            //    return true;
            return node.Statements.Any(e => e.IsKind(SyntaxKind.GotoStatement));
        }

        //bool SwitchCaseHasLabeledStatement(SwitchLabelSyntax node)
        //{
        //    return node.DescendantNodes().Any(e => e.IsKind(SyntaxKind.LabeledStatement));
        //}

        static bool SwitchCaseHasLabeledStatement(SwitchSectionSyntax node)
        {
            return node.Statements.Any(e => e.IsKind(SyntaxKind.LabeledStatement));
        }

        static bool IsSimpleSwitchCase(SwitchLabelSyntax node)
        {
            //if (/*SwitchCaseHasLabeledStatement(node) || */SwitchCaseHasGotoStatement(node))
            //{
            //    return false;
            //}
            return node.IsKind(SyntaxKind.CaseSwitchLabel) || node.IsKind(SyntaxKind.DefaultSwitchLabel);
        }

        static bool SwitchCaseHasGotoJump(SwitchStatementSyntax node)
        {
            return node.Sections.Any(c => SwitchCaseHasGotoStatement(c));
        }

        static bool SwitchCaseHasLabeledStatement(SwitchStatementSyntax node)
        {
            return node.Sections.Any(c => SwitchCaseHasLabeledStatement(c));
        }

        static bool IsSimpleSwitchCase(SwitchStatementSyntax node)
        {
            //if (SwitchHasGotoCase(node))
            //return false;
            //if (SwitchCaseHasGotoJump(node))
            //    return false;
            //if (SwitchCaseHasLabeledStatement(node))
            //    return false;
            return node.Sections.SelectMany(c => c.Labels).All(c => IsSimpleSwitchCase(c));
            //&&
            //node.Sections.All(c => !SwitchCaseHasLabeledStatement(c)/* && !SwitchCaseHasGotoStatement(c)*/);
        }

        //bool IsTypeSwitchStatement(SwitchStatementSyntax node)
        //{
        //    bool isTypeSwitch = node.ChildNodes()
        //                   .Any(c => c.IsKind(SyntaxKind.CasePatternSwitchLabel) || (c is SwitchSectionSyntax cc && cc.Labels.Any(l => l.IsKind(SyntaxKind.CasePatternSwitchLabel))));
        //    return isTypeSwitch;
        //}

        const string SwitchExpressionVariableName = "__switchExpressionVariableName__";
        static List<StatementSyntax> GetStatementsFromSwitchLabel(LabeledStatementSyntax labelNode)
        {
            static bool IsStopStatement(StatementSyntax statement)
            {
                // Stop immediately on break
                if (statement is BreakStatementSyntax)
                {
                    return true;
                }

                //// Stop immediately on a new label to prevent fall-through bleeding
                //if (statement is LabeledStatementSyntax)
                //{
                //    return true;
                //}

                return false;
            }

            var collectedStatements = new List<StatementSyntax>();

            if (labelNode == null)
            {
                return collectedStatements;
            }

            if (labelNode.Statement != null)
            {
                if (IsStopStatement(labelNode.Statement))
                {
                    return collectedStatements;
                }
                collectedStatements.Add(labelNode.Statement);
            }

            var nextNode = labelNode.NextSibling();
            while (nextNode != null)
            {
                // Stop if we hit a completely different switch section
                if (nextNode is SwitchSectionSyntax)
                {
                    break;
                }

                if (nextNode is StatementSyntax statement)
                {
                    if (IsStopStatement(statement))
                    {
                        break;
                    }
                    collectedStatements.Add(statement);
                }

                nextNode = nextNode.NextSibling();
            }

            return collectedStatements;
        }

        string[] CacheTupleItemsIntoTempVariableNames(CSharpSyntaxNode node, TupleExpressionSyntax tuple, string varPrefix = "$t")
        {
            var names = new string[tuple.Arguments.Count];
            int ix = 0;
            int manglingSeed = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
            foreach (var arg in tuple.Arguments)
            {
                var argCacheName = $"{varPrefix}{manglingSeed}";
                names[ix] = argCacheName;
                //switchClosure.Tags.Add(SwitchExpressionVariableName, switchCacheVariableName);
                CurrentTypeWriter.Write(node, $"let {argCacheName} = ", true);
                Visit(arg.Expression);
                CurrentTypeWriter.WriteLine(node, $";");
                ix++;
                manglingSeed = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
            }
            return names;
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            bool isSimpleSwitchCase = IsSimpleSwitchCase(node);
            var hasGotoCase = SwitchHasGotoCase(node);
            bool hasGotoLabel = SwitchCaseHasLabeledStatement(node);
            //bool hasGotoJump = SwitchCaseHasGotoJump(node);
            //if any of the case is a CasePatternSwitchLabelSyntax, use.GetType()
            //bool isTypeSwitch = IsTypeSwitchStatement(node);
            OpenClosure(node);
            var switchClosure = CurrentClosure;
            if (hasGotoLabel)
            {
                foreach (var section in node.Sections)
                {
                    var labels = CollectGotoLabelsIntoCurrentClosure(section);
                    //Collect each label statements, an queue them for insertion directly into the goto places
                    //foreach (var label in labels)
                    //{
                    //alreadyTriedImport added by CollectGotoLabelsIntoCurrentClosure
                    //switchClosure.GotoJumpLabels.Add(label.Identifier.ValueText);
                    ////get all statement after this label, until we see break;
                    //var statements = GetStatementsFromSwitchLabel(label);
                    ////force a break after, unless we already jumped again
                    //if (!statements.Last().IsKind(SyntaxKind.GotoStatement))
                    //    statements.Add(SyntaxFactory.BreakStatement());
                    //switchClosure.GotoInsertInlineStatements.Add(label.Identifier.ValueText, statements);
                    //}
                }
            }
            //if (hasGotoCase || hasGotoLabel/* || hasGotoJump*/)
            //{
            //    var manglingSeed = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
            //    string jumpStart = $"$switchJumpStart{manglingSeed}";
            //    string jumpState = $"$switchJumpState{manglingSeed}";
            //    CurrentClosure.JumpStartLabelName = jumpStart;
            //    CurrentClosure.JumpStateMachineVariableName = jumpState;
            //    CurrentTypeWriter.WriteLine(node, $"let {jumpState} = null;", true);
            //    CurrentTypeWriter.WriteLine(node, $"{jumpStart}: while(true)", true);
            //    CurrentTypeWriter.WriteLine(node, "{", true);
            //}
            //else 
            var manglingSeed = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
            if ((!Constants.ComplexSwitchUseIfElse && !isSimpleSwitchCase) || hasGotoCase || hasGotoLabel)
            {
                if (hasGotoCase || hasGotoLabel)
                {
                    string jumpStart = $"$switchJumpStart{manglingSeed}";
                    string jumpState = $"$switchJumpState{manglingSeed}";
                    CurrentClosure.JumpStartLabelName = jumpStart;
                    CurrentClosure.JumpStateMachineVariableName = jumpState;
                    CurrentTypeWriter.WriteLine(node, $"let {jumpState};", true);
                    CurrentTypeWriter.Write(node, $"{jumpStart}: ", true);
                    flowJumpLabels.Add(node, jumpStart);
                }
                else
                {
                    CurrentTypeWriter.Write(node, "", true);
                }
                CurrentTypeWriter.WriteLine(node, $"while(true)");
                CurrentTypeWriter.WriteLine(node, "{", true);
            }
            if (!isSimpleSwitchCase)
            {
                CurrentTypeWriter.WriteLine(node, $"//switch ({node.Expression.ToString().EscapeString()})", true);
            }
            bool cachingSwitchVariable = NeedsCachePatternExpressionInTempVariable(node.Expression);
            string? switchCacheVariableName = null;
            if (cachingSwitchVariable)
            {
                if (node.Expression.IsKind(SyntaxKind.TupleExpression) && !hasGotoCase && !hasGotoLabel)
                {
                    var tuple = (TupleExpressionSyntax)node.Expression;
                    switchClosure.SwitchExpressionCacheVariableNames = CacheTupleItemsIntoTempVariableNames(node, tuple, "$switch");
                }
                else
                {
                    switchCacheVariableName = $"$switch{manglingSeed}";
                    switchClosure.SwitchExpressionCacheVariableNames = [switchCacheVariableName];
                    //switchClosure.Tags.Add(SwitchExpressionVariableName, switchCacheVariableName);
                    CurrentTypeWriter.Write(node, $"let {switchCacheVariableName} = ", true);
                    if (hasGotoCase || hasGotoLabel)
                    {
                        CurrentTypeWriter.Write(node, $"{CurrentClosure.JumpStateMachineVariableName} ?? ");
                        if (!node.Expression.IsKind(SyntaxKind.IdentifierName))
                        {
                            CurrentTypeWriter.Write(node, "(");
                        }
                    }
                    Visit(node.Expression);
                    if (hasGotoCase || hasGotoLabel)
                    {
                        if (!node.Expression.IsKind(SyntaxKind.IdentifierName))
                        {
                            CurrentTypeWriter.Write(node, ")");
                        }
                    }
                    CurrentTypeWriter.WriteLine(node, $";");
                }
            }
            if (isSimpleSwitchCase)
            {
                CurrentTypeWriter.Write(node, "switch(", true);
                if (cachingSwitchVariable && switchCacheVariableName != null)
                {
                    CurrentTypeWriter.Write(node, switchCacheVariableName);
                }
                else
                {
                    if (hasGotoCase || hasGotoLabel)
                    {
                        CurrentTypeWriter.Write(node, $"{CurrentClosure.JumpStateMachineVariableName} ?? ");
                        if (!node.Expression.IsKind(SyntaxKind.IdentifierName))
                        {
                            CurrentTypeWriter.Write(node, "(");
                        }
                    }
                    Visit(node.Expression);
                    if (hasGotoCase || hasGotoLabel)
                    {
                        if (!node.Expression.IsKind(SyntaxKind.IdentifierName))
                        {
                            CurrentTypeWriter.Write(node, ")");
                        }
                    }
                }
                CurrentTypeWriter.WriteLine(node, ")");
                CurrentTypeWriter.WriteLine(node, "{", true, forbidInsertion: true);
            }
            else if (Constants.ComplexSwitchUseIfElse && !hasGotoCase && !hasGotoLabel)
            {
                CurrentTypeWriter.WriteLine(node, "{", true); //define a closure that switch var are defined into
                string jumpStart = $"$switchJumpStart{manglingSeed}";
                CurrentClosure.JumpStartLabelName = jumpStart;
                var switchStartLine = CurrentTypeWriter.WriteLine(node, $"{jumpStart}: ", true);
                switchClosure.SwitchStartLine = switchStartLine;
                flowJumpLabels.Add(node, jumpStart);
            }
            VisitChildren(node.Sections);
            CloseClosure(node);
            //base.VisitSwitchStatement(node);
            if (isSimpleSwitchCase)
            {
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            else if (Constants.ComplexSwitchUseIfElse && !hasGotoCase && !hasGotoLabel)
            {
                CurrentTypeWriter.WriteLine(node, "}", true); //close closure that switch var are defined into
            }
            if ((!Constants.ComplexSwitchUseIfElse && !isSimpleSwitchCase) || hasGotoCase || hasGotoLabel)
            {
                CurrentTypeWriter.WriteLine(node, "break;", true); //end while
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            //switchClosure.Tags.Remove(SwitchExpressionVariableName);
        }

        Stack<CodeLineWriter> switchFirstIfLine = new();
        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            var switchStatement = node.FindClosestParent<SwitchStatementSyntax>() ?? throw new InvalidOperationException("Case should be inside a switch");
            bool isFirst = switchStatement.Sections.IndexOf(node) == 0;
            bool isLast = switchStatement.Sections.IndexOf(node) == switchStatement.Sections.Count - 1;
            var switchClosure = GetClosureOf(switchStatement);
            bool isSimpleSwitch = IsSimpleSwitchCase(switchStatement);
            bool sectionIsDefault = node.Labels.All(l => l.IsKind(SyntaxKind.DefaultSwitchLabel));
            //bool hasGotoCase = HasGotoCase(switchStatement);
            bool wrapBodyInClosure = false;
            if (!isSimpleSwitch)
            {
                if (!sectionIsDefault)
                {
                    if (!Constants.ComplexSwitchUseIfElse)
                    {
                        wrapBodyInClosure = node.Labels.Any(l => l.IsKind(SyntaxKind.CasePatternSwitchLabel));
                        //make sure all case are in a closure lest we have variable conflict if defined in below if statement
                        if (wrapBodyInClosure)
                        {
                            OpenClosure(node);
                            CurrentTypeWriter.WriteLine(node, "{", true);
                        }
                    }
                    foreach (var label in node.Labels)
                    {
                        CurrentTypeWriter.WriteLine(node, $"//{label.ToString().EscapeString()}", true);
                    }
                    //if (Constants.ComplexSwitchUseIfElse && !isFirst)
                    //{
                    //    CurrentTypeWriter.Write(node, $"else ", true);
                    //    //CurrentTypeWriter.WriteLine(node, "{", true);
                    //    //OpenClosure(node);
                    //}
                    if (isFirst)
                    {
                    }
                    var line = CurrentTypeWriter.Write(node, $"{(Constants.ComplexSwitchUseIfElse && !isFirst ? "else " : "")}if (", true);
                    if (Constants.ComplexSwitchUseIfElse)
                    {
                        line.RedirectInsertBefore = switchClosure.SwitchStartLine;
                        //if (isFirst)
                        //{
                        //    switchFirstIfLine.Push(line);
                        //}
                        //else
                        //{
                        //    line.RedirectInsertBefore = switchFirstIfLine.Peek();
                        //}
                        //if (isLast)
                        //{
                        //    switchFirstIfLine.Pop();
                        //}
                    }
                }
                else
                {
                    CurrentTypeWriter.WriteLine(node, $"//default", true);
                    if (Constants.ComplexSwitchUseIfElse)
                    {
                        CurrentTypeWriter.WriteLine(node, $"else", true);
                        //CurrentTypeWriter.WriteLine(node, "{", true);
                    }
                }
                //if (hasGotoCase)
                //{
                //    Writer.Write(node, $"{CurrentClosure.JumpStateMachineVariableName} == ");
                //    WritePatternExpressionFilter(node);
                //    Writer.Write(node, " || ");
                //}
            }
            int ix = 0;
            foreach (var label in node.Labels)
            {
                if (!isSimpleSwitch && ix > 0)
                {
                    CurrentTypeWriter.Write(node, " || ");
                }
                if (!isSimpleSwitch && node.Labels.Count > 1)
                {
                    CurrentTypeWriter.Write(node, "(");
                }
                Visit(label);
                if (!isSimpleSwitch && node.Labels.Count > 1)
                {
                    CurrentTypeWriter.Write(node, ")");
                }
                ix++;
            }
            if (!isSimpleSwitch)
            {
                if (!sectionIsDefault)
                {
                    CurrentTypeWriter.WriteLine(node, ")");
                }
            }
            var swClosure = CurrentClosure;
            bool childIsBlock = Utilities.ChildIsBlock(node);
            //if (!isSimpleSwitch)
            //{
            //    if (sectionIsDefault)
            //    {
            //        CurrentTypeWriter.WriteLine(node, $"//default", true);
            //        if (Constants.ComplexSwitchUseIfElse)
            //            CurrentTypeWriter.WriteLine(node, "{", true);
            //    }
            //}
            if (!childIsBlock)
            {
                OpenClosure(node);
                if (!isSimpleSwitch)
                    CurrentTypeWriter.WriteLine(node, "{", true);
            }
            //foreach (var label in node.Labels)
            //{
            //    if (label is CasePatternSwitchLabelSyntax cp && cp.Pattern is DeclarationPatternSyntax dps && dps.Designation is SingleVariableDesignationSyntax svd)
            //    {
            //        var swExpressionVaribleName = swClosure.Tags[SwitchExpressionVariableName];
            //        Writer.WriteLine(node, $"let {svd.Identifier.ValueText} = {swExpressionVaribleName};", true);
            //        //Visit(dps.Designation);
            //        //Writer.Write(node, " = ");
            //        //Visit(dps.Type);
            //    }
            //}
            //if (!BlockTryHandleJumpLabels(node, node.ChildNodes().Except(node.Labels)))
            VisitChildren(node.Statements);
            //base.VisitSwitchSection(node);
            if (!childIsBlock)
            {
                if (!isSimpleSwitch)
                    CurrentTypeWriter.WriteLine(node, "}", true);
                CloseClosure(node);
            }
            if (!isSimpleSwitch)
            {
                if (!sectionIsDefault)
                {
                    if (!Constants.ComplexSwitchUseIfElse && wrapBodyInClosure)
                    {
                        CurrentTypeWriter.WriteLine(node, "}", true);
                        CloseClosure(node);
                    }
                    //if (Constants.ComplexSwitchUseIfElse && !isFirst) //close else
                    //{
                    //    CurrentTypeWriter.WriteLine(node, "}", true);
                    //    CloseClosure(node);
                    //}
                }
                if (sectionIsDefault)
                {
                    //if (Constants.ComplexSwitchUseIfElse)
                    //CurrentTypeWriter.WriteLine(node, "}", true); //close default else
                }
            }
        }

        public override void VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node)
        {
            var switchStatement = node.FindClosestParent<SwitchStatementSyntax>() ?? throw new InvalidOperationException("Case should be inside a switch");
            if (IsSimpleSwitchCase(switchStatement))
            {
                CurrentTypeWriter.WriteLine(node, "default:", true);
            }
            //base.VisitDefaultSwitchLabel(node);
        }
    }
}
