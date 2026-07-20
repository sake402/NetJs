using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        bool gotoVariableDeclarationActive;
        int gotoGeneratorActive;
        List<CSharpSyntaxNode> gotoDeclarationDefined = new List<CSharpSyntaxNode>();

        bool GotoHasDefinedVariable(CSharpSyntaxNode variable) => gotoGeneratorActive > 0 && gotoDeclarationDefined.Contains(variable);

        List<LabeledStatementSyntax> CollectGotoLabelsIntoCurrentClosure(CSharpSyntaxNode node)
        {
            List<LabeledStatementSyntax> rlabels = new();
            var labels = node.ChildNodes().Where(e => e.IsKind(SyntaxKind.LabeledStatement)).Cast<LabeledStatementSyntax>();
            foreach (var label in labels)
            {
                rlabels.Add(label);
                CurrentClosure.GotoJumpLabels.Add(label.Identifier.ValueText);
                //Handle scenario. Not 64 bit, so labels cascade inside each other. DiffNextInt inside DiffOffset0
                //DiffOffset0:
                // If we reached here, we already see a difference in the unrolled loop above
                //#if TARGET_64BIT
                //                if (*(int*)a == *(int*)b)
                //                {
                //                    a += 2; b += 2;
                //                }
                //#endif // TARGET_64BIT
                //DiffNextInt:
                var labelCascades = label.ChildNodes().Where(e => e.IsKind(SyntaxKind.LabeledStatement)).Cast<LabeledStatementSyntax>().ToList();
                foreach (var mlabel in labelCascades)
                {
                    CurrentClosure.GotoJumpLabels.Add(mlabel.Identifier.ValueText);
                    rlabels.Add(mlabel);
                }
            }
            return rlabels;
        }

        bool BlockTryHandleJumpLabels(CSharpSyntaxNode node, IEnumerable<SyntaxNode>? statements = null)
        {
            //Debug.Assert(CurrentClosure.Syntax == node);
            var labels = CollectGotoLabelsIntoCurrentClosure(node);
            if (labels.Count > 0)
            {
                gotoVariableDeclarationActive = true;

                //var firstLabel = labels.First();
                //FileLinePositionSpan span = node.SyntaxTree.GetLineSpan(firstLabel.Span);
                //int lineNumber = span.StartLinePosition.Line;

                var initStatements = statements ?? node.ChildNodes();
                //Since we use switch case for goto, define all declarations in the block initially so they are visible througout the block scope
                var declarations = initStatements
                    .Concat(labels.SelectMany(l => l.ChildNodes()))
                    .Where(e => e.IsKind(SyntaxKind.LocalDeclarationStatement))
                    .Cast<LocalDeclarationStatementSyntax>()
                    .SelectMany(e => e.Declaration.Variables)
                    .DistinctBy(v => v.Identifier.ValueText);
                    //.ToList();
                //Handle likes of Unsafe.SkipInit(out Buf12 bufQuo), define bufQuo initially
                var outDeclarations = initStatements
                    .Where(e => e is ExpressionStatementSyntax satetment && satetment.Expression.IsKind(SyntaxKind.InvocationExpression))
                    .SelectMany(d => d.DescendantNodes())
                    .Where(e => e.Parent.IsKind(SyntaxKind.DeclarationExpression) && e.IsKind(SyntaxKind.SingleVariableDesignation))
                    .Cast<SingleVariableDesignationSyntax>()
                    .DistinctBy(d => d.Identifier.ValueText);
                foreach (var d in declarations)
                {
                    CurrentTypeWriter.WriteLine(node, $"/*{((VariableDeclarationSyntax)d.Parent!).Type.ToString().Trim()}*/ let {d.Identifier.ValueText};", true);
                    gotoDeclarationDefined.Add((CSharpSyntaxNode)d.Parent!);
                }
                foreach (var d in outDeclarations)
                {
                    CurrentTypeWriter.WriteLine(node, $"/*{((DeclarationExpressionSyntax)d.Parent!).Type.ToString().Trim()}*/ let {d.Identifier.ValueText};", true);
                    gotoDeclarationDefined.Add((CSharpSyntaxNode)d.Parent!);
                }
                //VisitChildren(declarations);
                //gotoDeclarationDefined.AddRange(declarations.Select(d => (CSharpSyntaxNode)d.Parent!));
                //gotoDeclarationDefined.AddRange(outDeclarations.Select(d => (CSharpSyntaxNode)d.Parent!));
                gotoVariableDeclarationActive = false;

                gotoGeneratorActive++;
                var manglingSeed = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                string jumpStart = $"$gotoJumpStart{manglingSeed}";
                string jumpState = $"$gotoJumpState{manglingSeed}";
                CurrentClosure.JumpStartLabelName = jumpStart;
                CurrentClosure.JumpStateMachineVariableName = jumpState;
                CurrentTypeWriter.WriteLine(node, $"let {jumpState} = 0;", true);
                CurrentTypeWriter.WriteLine(node, $"{jumpStart}: while(true)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                CurrentTypeWriter.WriteLine(node, $"switch({jumpState})", true);
                CurrentTypeWriter.WriteLine(node, "{", true, forbidInsertion: true);
                CurrentTypeWriter.WriteLine(node, $"case 0:", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                //write every statement with no label first
                List<SyntaxNode> writtenNodes = new List<SyntaxNode>();
                SyntaxNode? lastNode = null;
                foreach (var mnode in initStatements)
                {
                    if (labels.Contains(mnode))
                    {
                        break;
                    }
                    bool skipVisit = false;
                    //if a variable is not initialized, no need to write it again
                    //as we have already defined it up above
                    if (mnode.IsKind(SyntaxKind.LocalDeclarationStatement))
                    {
                        var local = (LocalDeclarationStatementSyntax)mnode;
                        if (local.Declaration.Variables.All(e => e.Initializer == null))
                            skipVisit = true;
                    }
                    if (!skipVisit)
                        Visit(mnode);
                    writtenNodes.Add(mnode);
                    lastNode = mnode;
                }
                //Writer.WriteLine(node, "break;", true);
                CurrentTypeWriter.WriteLine(node, "}", true); //end case 0
                List<SyntaxNode> labelledStatements = new List<SyntaxNode>();
                void EmitLabelledStatements(LabeledStatementSyntax label)
                {
                    var index = CurrentClosure.GotoJumpLabels.IndexOf(label.Identifier.ValueText) + 1;
                    CurrentTypeWriter.Write(node, $"case ", true);
                    CurrentTypeWriter.Write(node, index.ToString());
                    CurrentTypeWriter.WriteLine(node, $": /*{label.Identifier.ValueText}*/");
                    foreach (var node in labelledStatements)
                    {
                        Visit(node);
                    }
                    labelledStatements.Clear();
                }
                LabeledStatementSyntax? currentLabel = null;
                foreach (var mnode in statements ?? node.ChildNodes())
                {
                    if (writtenNodes.Contains(mnode))
                        continue;
                    if (labels.Contains(mnode))
                    {
                        if (currentLabel != null)
                            EmitLabelledStatements(currentLabel);
                        currentLabel = (LabeledStatementSyntax)mnode;
                        labelledStatements.Add(currentLabel.Statement);
                        continue;
                    }
                    labelledStatements.Add(mnode);
                }
                if (currentLabel != null)
                    EmitLabelledStatements(currentLabel);
                CurrentTypeWriter.WriteLine(node, "}", true); //end switch
                CurrentTypeWriter.WriteLine(node, "break;", true);
                CurrentTypeWriter.WriteLine(node, "}", true); //end while
                gotoGeneratorActive--;
                //gotoDeclarationDefined.Remove(declarations.Select(d => (VariableDeclarationSyntax)d.Parent!));

                return true;
            }
            return false;
        }

        public override void VisitLabeledStatement(LabeledStatementSyntax node)
        {
            if (node.Parent.IsKind(SyntaxKind.SwitchSection))
            {
                CurrentTypeWriter.Write(node, $"case \"$", true);
                CurrentTypeWriter.Write(node, node.Identifier.ValueText);
                CurrentTypeWriter.WriteLine(node, $"$\": /* label {node.Identifier.ValueText} */");
                Visit(node.Statement);
            }
            else
            {
                bool writeCase = true;
                //If this label statements is inlined into the respective goto places, no need of case
                CSharpSyntaxNode? blockOrSwitch = (CSharpSyntaxNode?)node.FindClosestParent<BlockSyntax>(e =>
                {
                    var blockClosure = GetClosureOf(e);
                    if (blockClosure.GotoJumpLabels.Contains(node.Identifier.ValueText))
                        return true;
                    return false;
                }) ?? node.FindClosestParent<SwitchStatementSyntax>(e =>
                {
                    var switchClosure = GetClosureOf(e);
                    if (switchClosure.GotoJumpLabels.Contains(node.Identifier.ValueText))
                        return true;
                    return false;
                });
                if (blockOrSwitch != null)
                {
                    var blockOrSwitchClosure = GetClosureOf(blockOrSwitch);
                    var statements = blockOrSwitchClosure.GotoInsertInlineStatements.GetValueOrDefault(node.Identifier.ValueText);
                    if (statements != null)
                    {
                        writeCase = false;
                    }
                }
                if (writeCase)
                {
                    var index = CurrentClosure.GotoJumpLabels.IndexOf(node.Identifier.ValueText);
                    if (index == -1) //label was not in a block or not identified by the current closure
                    {

                    }
                    else
                    {
                        CurrentTypeWriter.Write(node, $"case ", true);
                        CurrentTypeWriter.Write(node, (index + 1).ToString());
                        CurrentTypeWriter.WriteLine(node, $": /*{node.Identifier.ValueText}*/");
                    }
                }
                else
                {
                    CurrentTypeWriter.WriteLine(node, $"// {node.Identifier.ValueText}: labelled statement inlined", true);
                }
                Visit(node.Statement);
                //Writer.WriteLine(node, "break;", true);
                //base.VisitLabeledStatement(node);
            }
        }

        public override void VisitGotoStatement(GotoStatementSyntax node)
        {
            if (node.IsKind(SyntaxKind.GotoCaseStatement) || node.IsKind(SyntaxKind.GotoDefaultStatement))
            {
                var _switch = node.FindClosestParent<SwitchStatementSyntax>(e =>
                {
                    var switchClosure = GetClosureOf(e);
                    if (switchClosure.JumpStartLabelName != null)
                        return true;
                    return false;
                }) ?? throw new InvalidOperationException("Goto case must be within a switch");
                var switchClosure = GetClosureOf(_switch);
                CurrentTypeWriter.Write(node, $"{switchClosure.JumpStateMachineVariableName} = ", true);
                if (node.IsKind(SyntaxKind.GotoCaseStatement))
                {
                    //Debug.Assert(node.Expression is LiteralExpressionSyntax);
                    Visit(node.Expression); //this should be a literal expression
                }
                else
                {
                    //default
                    //we want to make sure we assign a value to JumpStateMachineVariableName that will not match any case in the switch
                    //undefined or null will not work as we generate the switch using JumpStateMachineVariableName ?? b
                    //TODO: For now we use this magic string. Hopefully it wont conflict with any user defined case value
                    CurrentTypeWriter.Write(node, "\"$_$_CaseDefault_$_$\"");
                }
                CurrentTypeWriter.WriteLine(node, $"; /*goto case {node.Expression?.ToString() ?? "default"}*/");
                CurrentTypeWriter.WriteLine(node, $"continue {switchClosure.JumpStartLabelName};", true);
            }
            else if (node.Expression is IdentifierNameSyntax id)
            {
                CSharpSyntaxNode? blockOrSwitch = node.FindClosestParent<CSharpSyntaxNode>(e =>
                {
                    var blockClosure = TryGetClosureOf(e);
                    if (blockClosure?.GotoJumpLabels.Contains(id.Identifier.ValueText) ?? false)
                        return true;
                    return false;
                }) ?? node.FindClosestParent<SwitchStatementSyntax>(e =>
                {
                    var switchClosure = GetClosureOf(e);
                    if (switchClosure.GotoJumpLabels.Contains(id.Identifier.ValueText))
                        return true;
                    return false;
                }) ?? throw new InvalidOperationException("Goto must be within a block");
                //CSharpSyntaxNode blockOrSwitch = (CSharpSyntaxNode?)node.FindClosestParent<BlockSyntax>(e =>
                //{
                //    var blockClosure = GetClosureOf(e);
                //    if (blockClosure.GotoJumpLabels.Contains(id.Identifier.ValueText))
                //        return true;
                //    return false;
                //}) ?? node.FindClosestParent<SwitchStatementSyntax>(e =>
                //{
                //    var switchClosure = GetClosureOf(e);
                //    if (switchClosure.GotoJumpLabels.Contains(id.Identifier.ValueText))
                //        return true;
                //    return false;
                //}) ?? throw new InvalidOperationException("Goto must be within a block");
                var blockOrSwitchClosure = GetClosureOf(blockOrSwitch);
                var statements = blockOrSwitchClosure.GotoInsertInlineStatements.GetValueOrDefault(id.Identifier.ValueText);
                //If this goto target statements is to be inlined, simply inline the statements
                if (statements != null)
                {
                    CurrentTypeWriter.WriteLine(node, $"//Begin Inlined goto {id.Identifier.ValueText}", true);
                    foreach (var statement in statements)
                        Visit(statement);
                    CurrentTypeWriter.WriteLine(node, $"//End Inlined goto {id.Identifier.ValueText}", true);
                }
                else
                {
                    var index = blockOrSwitchClosure.GotoJumpLabels.IndexOf(id.Identifier.ValueText);
                    if (index == -1 || (blockOrSwitchClosure.Syntax.IsKind(SyntaxKind.SwitchStatement) && node.Parent.IsKind(SyntaxKind.SwitchSection)))
                    {
                        CurrentTypeWriter.Write(node, $"{blockOrSwitchClosure.JumpStateMachineVariableName} = ", true);
                        CurrentTypeWriter.Write(node, $"\"${id.Identifier.ValueText}$\"");
                    }
                    else
                    {
                        CurrentTypeWriter.Write(node, $"{blockOrSwitchClosure.JumpStateMachineVariableName} = ", true);
                        CurrentTypeWriter.Write(node, (index + 1).ToString());
                    }
                    CurrentTypeWriter.WriteLine(node, $"; /*goto {id.Identifier.ValueText}*/");
                    CurrentTypeWriter.WriteLine(node, $"continue {blockOrSwitchClosure.JumpStartLabelName};", true);
                }
            }
            else
            {

            }
            //base.VisitGotoStatement(node);
        }
    }
}
