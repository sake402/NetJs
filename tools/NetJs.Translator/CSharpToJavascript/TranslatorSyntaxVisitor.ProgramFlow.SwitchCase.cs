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
            if (svd != null)
            {
                CurrentTypeWriter.InsertInCurrentClosure(node, $"let {svd.Identifier.ValueText};", true);
                //CurrentTypeWriter.Write(node, "(");
                //CurrentTypeWriter.Write(node, svd.Identifier.ValueText);
                //CurrentTypeWriter.Write(node, $" = ");
                //WritePatternExpressionFilter(node);
                //CurrentTypeWriter.Write(node, $", ");

            }
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.IsTypeName}(");
            if (svd != null)
            {
                CurrentTypeWriter.Write(node, svd.Identifier.ValueText);
            }
            else
            {
                WritePatternExpressionFilter(node);
            }
            CurrentTypeWriter.Write(node, $", ");
            Visit(node.Type);
            if (svd != null)
            {
                CurrentTypeWriter.Write(node, $", {{ set $v(v){{ {svd.Identifier.ValueText} = v; }} }}");
            }
            CurrentTypeWriter.Write(node, $")");
            if (svd != null)
            {
                //CurrentTypeWriter.Write(node, ")");
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
                    if (!type.IsValueType)
                    {
                        WritePatternExpressionFilter(node);
                        CurrentTypeWriter.Write(node, " != null && ");
                    }
                    Visit(node.Pattern);
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

        bool HasGotoCase(SwitchStatementSyntax node)
        {
            return node.DescendantNodes().Any(c => c.IsKind(SyntaxKind.GotoCaseStatement) || c.IsKind(SyntaxKind.GotoDefaultStatement));
        }

        bool IsSimpleSwitchCase(SwitchLabelSyntax node)
        {
            return node.IsKind(SyntaxKind.CaseSwitchLabel) || node.IsKind(SyntaxKind.DefaultSwitchLabel);
        }

        bool IsSimpleSwitchCase(SwitchStatementSyntax node)
        {
            return node.Sections.SelectMany(c => c.Labels).All(c => IsSimpleSwitchCase(c));
        }

        //bool IsTypeSwitchStatement(SwitchStatementSyntax node)
        //{
        //    bool isTypeSwitch = node.ChildNodes()
        //                   .Any(c => c.IsKind(SyntaxKind.CasePatternSwitchLabel) || (c is SwitchSectionSyntax cc && cc.Labels.Any(l => l.IsKind(SyntaxKind.CasePatternSwitchLabel))));
        //    return isTypeSwitch;
        //}

        const string SwitchExpressionVariableName = "__switchExpressionVariableName__";
        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            bool isSimpleSwitchCase = IsSimpleSwitchCase(node);
            var hasGotoCase = HasGotoCase(node);
            //if any of the case is a CasePatternSwitchLabelSyntax, use.GetType()
            //bool isTypeSwitch = IsTypeSwitchStatement(node);
            OpenClosure(node);
            var switchClosure = CurrentClosure;
            if (hasGotoCase)
            {
                var manglingSeed = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                string jumpStart = $"$switchJumpStart{manglingSeed}";
                string jumpState = $"$switchJumpState{manglingSeed}";
                CurrentClosure.JumpStartLabelName = jumpStart;
                CurrentClosure.JumpStateMachineVariableName = jumpState;
                CurrentTypeWriter.WriteLine(node, $"let {jumpState} = null;", true);
                CurrentTypeWriter.WriteLine(node, $"{jumpStart}: while(true)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
            }
            else if (!isSimpleSwitchCase)
            {
                CurrentTypeWriter.WriteLine(node, $"while(true)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
            }
            var i = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
            switchClosure.Tags.Add(SwitchExpressionVariableName, $"$switch{i}");
            if (!isSimpleSwitchCase)
            {
                CurrentTypeWriter.WriteLine(node, $"//switch ({node.Expression.ToString().Escape()})", true);
            }
            CurrentTypeWriter.Write(node, $"let $switch{i} = ", true);
            Visit(node.Expression);
            CurrentTypeWriter.WriteLine(node, $";");
            if (isSimpleSwitchCase)
            {
                CurrentTypeWriter.Write(node, "switch(", true);
                if (hasGotoCase)
                {
                    CurrentTypeWriter.Write(node, $"{CurrentClosure.JumpStateMachineVariableName} ?? ");
                }
                //if (isTypeSwitch)
                //{
                //    Writer.Write(node, $"{_global.GlobalName}.System.Object.GetType.call(");
                //}
                CurrentTypeWriter.Write(node, $"$switch{i}");
                //if (isTypeSwitch)
                //{
                //    Writer.Write(node, $").{Constants.TypePrototypeName}");
                //}
                CurrentTypeWriter.WriteLine(node, ")");
                CurrentTypeWriter.WriteLine(node, "{", true, forbidInsertion: true);
            }
            else
            {

            }
            VisitChildren(node.Sections);
            CloseClosure();
            //base.VisitSwitchStatement(node);
            if (isSimpleSwitchCase)
            {
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            if (hasGotoCase || !isSimpleSwitchCase)
            {
                CurrentTypeWriter.WriteLine(node, "break;", true); //end while
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            switchClosure.Tags.Remove(SwitchExpressionVariableName);
        }

        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            var switchStatement = node.FindClosestParent<SwitchStatementSyntax>() ?? throw new InvalidOperationException("Case should be inside a switch");
            bool isSimpleSwitch = IsSimpleSwitchCase(switchStatement);
            bool sectionIsDefault = node.Labels.All(l => l.IsKind(SyntaxKind.DefaultSwitchLabel));
            //bool hasGotoCase = HasGotoCase(switchStatement);
            if (!isSimpleSwitch)
            {
                if (!sectionIsDefault)
                {
                    //make sure all case are in a closure lest we have variable conflict
                    if (node.Labels.Any(l => l.IsKind(SyntaxKind.CasePatternSwitchLabel)))
                    {
                        OpenClosure(node);
                        CurrentTypeWriter.WriteLine(node, "{", true);
                    }
                    foreach (var label in node.Labels)
                    {
                        CurrentTypeWriter.WriteLine(node, $"//{label.ToString().Escape()}", true);
                    }
                    CurrentTypeWriter.Write(node, "if (", true);
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
                Visit(label);
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
            if (!isSimpleSwitch)
            {
                if (sectionIsDefault)
                {
                    CurrentTypeWriter.WriteLine(node, $"//default", true);
                }
            }
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
            VisitChildren(node.ChildNodes().Except(node.Labels));
            //base.VisitSwitchSection(node);
            if (!childIsBlock)
            {
                if (!isSimpleSwitch)
                    CurrentTypeWriter.WriteLine(node, "}", true);
                CloseClosure();
            }
            if (!isSimpleSwitch)
            {
                if (!sectionIsDefault)
                {
                    if (node.Labels.Any(l => l.IsKind(SyntaxKind.CasePatternSwitchLabel)))
                    {
                        CloseClosure();
                        CurrentTypeWriter.WriteLine(node, "}", true);
                    }
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
