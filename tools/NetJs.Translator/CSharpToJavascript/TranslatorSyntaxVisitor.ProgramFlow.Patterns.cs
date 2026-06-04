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
        ExpressionSyntax GetPatternExpression(CSharpSyntaxNode node)
        {
            var listPattern = node.FindClosestParent<ListPatternSyntax>();
            var containingIsPatternExpression = node.FindClosestParent<IsPatternExpressionSyntax>();
            var containingSwitchExpression = containingIsPatternExpression == null ? node.FindClosestParent<SwitchExpressionSyntax>() : null;
            var containingSwitchStatement = containingIsPatternExpression == null && containingSwitchExpression == null ? node.FindClosestParent<SwitchStatementSyntax>() : null;
            var switchClosure = CurrentClosure.FindHierachy<SwitchStatementSyntax>() ?? CurrentClosure.FindHierachy<SwitchExpressionSyntax>() ?? CurrentClosure;
            var swVariableName = switchClosure.Tags.GetValueOrDefault(SwitchExpressionVariableName);
            var isVariableName = switchClosure.Tags.GetValueOrDefault(IsPatternExpressionVariableName);
            if (containingSwitchExpression != null)
            {
                return containingSwitchExpression.GoverningExpression;
            }
            else if (containingSwitchStatement != null)
            {
                return containingSwitchStatement.Expression;
            }
            else if (containingIsPatternExpression != null)
            {
                return containingIsPatternExpression.Expression;
            }
            throw new InvalidOperationException();
        }

        int patternExpressionWrittenAlready;
        Stack<string> patternExpressions = new Stack<string>();
        string? currentPatternExpression
        {
            get
            {
                if (patternExpressions.TryPeek(out var pe))
                    return pe;
                return null;
            }
        }
        void WritePatternExpressionFilter(CSharpSyntaxNode node, bool dereferenceListPattern = true)
        {
            var listPattern = node.FindClosestParent<ListPatternSyntax>();
            var containingIsPatternExpression = node.FindClosestParent<IsPatternExpressionSyntax>();
            var containingSwitchExpression = containingIsPatternExpression == null ? node.FindClosestParent<SwitchExpressionSyntax>() : null;
            var containingSwitchStatement = containingIsPatternExpression == null && containingSwitchExpression == null ? node.FindClosestParent<SwitchStatementSyntax>() : null;
            var switchClosure = CurrentClosure.FindHierachy<SwitchStatementSyntax>() ?? CurrentClosure.FindHierachy<SwitchExpressionSyntax>() ?? CurrentClosure;
            var swVariableName = switchClosure.Tags.GetValueOrDefault(SwitchExpressionVariableName);
            var isVariableName = switchClosure.Tags.GetValueOrDefault(IsPatternExpressionVariableName);
            void DoWrite()
            {
                if (currentPatternExpression != null)
                {
                    CurrentTypeWriter.Write(node, currentPatternExpression);
                    return;
                }
                if (containingSwitchExpression != null)
                {
                    if (swVariableName != null)
                    {
                        CurrentTypeWriter.Write(node, swVariableName);
                    }
                    else
                    {
                        Visit(containingSwitchExpression.GoverningExpression);
                    }
                }
                else if (containingSwitchStatement != null)
                {
                    if (swVariableName != null)
                    {
                        CurrentTypeWriter.Write(node, swVariableName);
                    }
                    else
                    {
                        Visit(containingSwitchStatement.Expression);
                    }
                }
                else if (containingIsPatternExpression != null)
                {
                    if (isVariableName != null)
                    {
                        CurrentTypeWriter.Write(node, isVariableName);
                    }
                    else
                    {
                        Visit(containingIsPatternExpression.Expression);
                    }
                }
            }
            DoWrite();
            //Inside a list pattern?
            if (dereferenceListPattern && listPattern != null && currentListPatternContext != null)
            {
                CurrentTypeWriter.Write(node, "[");
                if (currentListPatternContext.SpreadStartIndex >= 0 &&
                    currentListPatternContext.PatternSymbol != null &&
                    currentListPatternContext.LenghtProperty != null)
                {
                    WriteMemberAccess(node, new CodeNode(() => DoWrite()), _global.GetTypeSymbol(currentListPatternContext.PatternSymbol), null, currentListPatternContext.LenghtProperty);
                    //DoWrite();
                    //CurrentTypeWriter.Write(node, ".");
                    //WriteMemberName(node, currentListPatternContext.LenghtProperty.ContainingType, currentListPatternContext.LenghtProperty);
                    //CurrentTypeWriter.Write(node, " - ");
                }
                CurrentTypeWriter.Write(node, currentListPatternContext.CurrentIndex.ToString());
                CurrentTypeWriter.Write(node, "]");
            }
        }

        public override void VisitConstantPattern(ConstantPatternSyntax node)
        {
            var containingIsPatternExpression = node.FindClosestParent<IsPatternExpressionSyntax>();
            var leftPatternType = containingIsPatternExpression != null ? _global.GetTypeSymbol(containingIsPatternExpression.Expression, this) : null;
            var rightPatternSymbol = (node.Parent.IsKind(SyntaxKind.NotPattern) || node.Parent.IsKind(SyntaxKind.IsPatternExpression)) && !node.Expression.IsKind(SyntaxKind.NullLiteralExpression) ?
                _global.GetSymbol(node.Expression, this) :
                null;
            if (leftPatternType != null &&
                leftPatternType.Kind == SymbolKind.NamedType &&
                SymbolEqualityComparer.Default.Equals(leftPatternType, _global.SystemObject) &&
                rightPatternSymbol != null)
            {
                var var_i = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                CurrentTypeWriter.InsertAbove(node, $"let $t{var_i};", true);
                CurrentTypeWriter.Write(node, $"({(node.Parent.IsKind(SyntaxKind.NotPattern) ? "!" : "")}{_global.GlobalName}.{Constants.IsTypeName}(");
                WritePatternExpressionFilter(node);
                CurrentTypeWriter.Write(node, $", ");
                CurrentTypeWriter.Write(node, rightPatternSymbol.ComputeOutputTypeName(_global));
                CurrentTypeWriter.Write(node, $", {{ set {Constants.RefValueName}(v){{ $t{var_i} = v }} }}");
                CurrentTypeWriter.Write(node, $") && $t{var_i} === ");
                Visit(node.Expression);
                CurrentTypeWriter.Write(node, $")");
            }
            else if (rightPatternSymbol != null && rightPatternSymbol.Kind == SymbolKind.NamedType && node.Expression is not LiteralExpressionSyntax)
            {
                CurrentTypeWriter.Write(node, $"{(node.Parent.IsKind(SyntaxKind.NotPattern) ? "!" : "")}{_global.GlobalName}.{Constants.IsTypeName}(");
                WritePatternExpressionFilter(node);
                CurrentTypeWriter.Write(node, $", ");
                Visit(node.Expression);
                CurrentTypeWriter.Write(node, $")");
            }
            else
            {
                if (patternExpressionWrittenAlready == 0/* && !node.Parent.IsKind(SyntaxKind.PropertyPatternClause) && !node.Parent.IsKind(SyntaxKind.Subpattern)*/)
                    WritePatternExpressionFilter(node);
                CurrentTypeWriter.Write(node, node.Parent.IsKind(SyntaxKind.NotPattern) ? " !== " : " === ");
                if (node.Expression is BinaryExpressionSyntax)
                {
                    CurrentTypeWriter.Write(node, $"(");
                }
                Visit(node.Expression);
                if (node.Expression is BinaryExpressionSyntax)
                {
                    CurrentTypeWriter.Write(node, $")");
                }
            }
        }

        public override void VisitUnaryPattern(UnaryPatternSyntax node)
        {
            //WritePatternExpressionFilter(node);
            Visit(node.Pattern);
        }

        public override void VisitRelationalPattern(RelationalPatternSyntax node)
        {
            if (patternExpressionWrittenAlready == 0)
                WritePatternExpressionFilter(node);
            CurrentTypeWriter.Write(node, " ");
            CurrentTypeWriter.Write(node, node.OperatorToken.ValueText);
            CurrentTypeWriter.Write(node, " ");
            Visit(node.Expression);
            //base.VisitRelationalPattern(node);
        }

        public override void VisitBinaryPattern(BinaryPatternSyntax node)
        {
            CurrentTypeWriter.Write(node, "(");
            //WritePatternExpressionFilter(node);
            Visit(node.Left);
            CurrentTypeWriter.Write(node, ")");
            switch (node.OperatorToken.ValueText)
            {
                case "or":
                    CurrentTypeWriter.Write(node, " || ");
                    break;
                case "and":
                    CurrentTypeWriter.Write(node, " && ");
                    break;
                default:
                    CurrentTypeWriter.Write(node, $" {node.OperatorToken.ValueText} ");
                    break;
            }
            CurrentTypeWriter.Write(node, "(");
            //WritePatternExpressionFilter(node);
            Visit(node.Right);
            CurrentTypeWriter.Write(node, ")");
            //base.VisitBinaryPattern(node);
        }

        public override void VisitDiscardPattern(DiscardPatternSyntax node)
        {
            base.VisitDiscardPattern(node);
        }

        public override void VisitTypePattern(TypePatternSyntax node)
        {
            //var switchStatement = node.FindClosestParent<SwitchStatementSyntax>();
            //var switchExpression = node.FindClosest<SwitchExpressionSyntax>();

            //if (switchStatement != null && IsTypeSwitchStatement(switchStatement))
            //{
            //    Visit(node.Type);
            //}
            //else
            //{
            if (node.Parent.IsKind(SyntaxKind.NotPattern))
            {
                CurrentTypeWriter.Write(node, "!");
            }
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.IsTypeName}(");
            WritePatternExpressionFilter(node);
            CurrentTypeWriter.Write(node, ", ");
            Visit(node.Type);
            CurrentTypeWriter.Write(node, ")");
            //}
        }

        public override void VisitSubpattern(SubpatternSyntax node)
        {
            base.VisitSubpattern(node);
        }
        public override void VisitExpressionColon(ExpressionColonSyntax node)
        {
            base.VisitExpressionColon(node);
        }

        public override void VisitPropertyPatternClause(PropertyPatternClauseSyntax node)
        {
            var containingIsPatternExpression = node.FindClosestParent<IsPatternExpressionSyntax>() ?? throw new InvalidOperationException();
            int ix = 0;
            foreach (var sub in node.Subpatterns)
            {
                if (ix > 0)
                    CurrentTypeWriter.Write(node, " && ");
                if (sub.Pattern.IsKind(SyntaxKind.ConstantPattern) || sub.Pattern.IsKind(SyntaxKind.RelationalPattern))
                {
                    List<SubpatternSyntax> pathToRoot = new List<SubpatternSyntax>();
                    sub.Pattern.VisitParentHierachy((p, d) =>
                    {
                        if (p == containingIsPatternExpression)
                            return false;
                        if (p is SubpatternSyntax ss)
                        {
                            pathToRoot.Add(ss);
                        }
                        return true;
                    });
                    pathToRoot.Reverse();
                    if (patternExpressionWrittenAlready == 0)
                        WritePatternExpressionFilter(node);
                    //SubpatternSyntax? last = null;
                    //ITypeSymbol currentTypeSymbol = _global.GetTypeSymbol(containingIsPatternExpression.Expression, this);
                    foreach (var p in pathToRoot)
                    {
                        if (p.NameColon != null)
                        {
                            var id = p.NameColon.Name;
                            var property = (IPropertySymbol)_global.GetSymbol(id, this);
                            WriteMemberAccess(p, new CodeNode(() => { }), property.ContainingType, id.Identifier.ValueText, null);
                        }
                        else if (p.ExpressionColon != null)
                        {
                            CurrentTypeWriter.Write(node, ".");
                            Visit(p.ExpressionColon.Expression);
                        }
                    }
                    patternExpressionWrittenAlready++;
                    Visit(pathToRoot.Last().Pattern);
                    patternExpressionWrittenAlready--;
                    //CurrentTypeWriter.Write(node, ".");
                    //Visit(sub.ExpressionColon.Expression);
                }
                else
                {
                    Visit(sub.Pattern);
                }
                //Writer.Write(node, "(");
                //else if (sub.ExpressionColon?.Expression is IdentifierNameSyntax id)
                //{
                //    var lhsType = _global.GetTypeSymbol(containingIsPatternExpression.Expression, this);
                //    WriteMemberAccess(sub.ExpressionColon.Expression, new CodeNode(() => WritePatternExpressionFilter(node)), lhsType, id.Identifier.ValueText, null);
                //    patternExpressionWrittenAlready = true;
                //    Visit(sub.Pattern);
                //    patternExpressionWrittenAlready = false;
                //    //CurrentTypeWriter.Write(node, ".");
                //    //Visit(sub.ExpressionColon.Expression);
                //    ix++;
                //    continue;
                //}

                //if (node.Parent.IsKind(SyntaxKind.RecursivePattern) && node.Parent.Parent.IsKind(SyntaxKind.Subpattern) && node.Parent.Parent.Parent.IsKind(SyntaxKind.PropertyPatternClause))
                //{
                //    if (sub.ExpressionColon?.Expression is IdentifierNameSyntax id)
                //    {
                //        var lhsType = _global.GetTypeSymbol(containingIsPatternExpression.Expression, this);
                //        WriteMemberAccess(sub.ExpressionColon.Expression, new CodeNode(() => WritePatternExpressionFilter(node)), lhsType, id.Identifier.ValueText, null);
                //        CurrentTypeWriter.Write(node, ".");
                //        Visit(sub.ExpressionColon.Expression);
                //        ix++;
                //        continue;
                //    }
                //    else
                //    {

                //    }
                //}
                //else if (sub.ExpressionColon?.Expression is IdentifierNameSyntax id)
                //{
                //    var lhsType = _global.GetTypeSymbol(containingIsPatternExpression.Expression, this);
                //    WriteMemberAccess(id, new CodeNode(() => WritePatternExpressionFilter(node)), lhsType, id.Identifier.ValueText, null);
                //    //CurrentTypeWriter.Write(node, " != null && ");
                //    //WriteMemberAccess(id, new CodeNode(() => WritePatternExpressionFilter(node)), lhsType, id.Identifier.ValueText, null);
                //}
                //else
                //{
                //    WritePatternExpressionFilter(node);
                //    if (sub.ExpressionColon != null)
                //    {
                //        CurrentTypeWriter.Write(node, ".");
                //        Visit(sub.ExpressionColon.Expression);
                //    }
                //    else
                //    {

                //    }
                //}
                //patternExpressionWrittenAlready = true;
                //Visit(sub.Pattern);
                //patternExpressionWrittenAlready = false;
                ////Writer.Write(node, ")");
                ix++;
            }
            if (node.Subpatterns.Count == 0)
            {
                CurrentTypeWriter.Write(node, "true");
            }
            //base.VisitPropertyPatternClause(node);
        }

        public override void VisitPositionalPatternClause(PositionalPatternClauseSyntax node)
        {
            var containingIsPatternExpression = node.FindClosestParent<IsPatternExpressionSyntax>();
            var typeSymbol = containingIsPatternExpression != null ? _global.GetTypeSymbol(containingIsPatternExpression.Expression, this) : null;
            bool isTuple = typeSymbol?.IsTupleType ?? false;
            Dictionary<SubpatternSyntax, int> variables = new();
            if (!isTuple)
            {
                for (int i = 0; i < node.Subpatterns.Count; i++)
                {
                    var varIndex = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                    CurrentTypeWriter.InsertAbove(node, $"let $t{varIndex};", true);
                    variables.Add(node.Subpatterns[i], varIndex);
                }
                CurrentTypeWriter.InsertAbove(node, () =>
                {
                    WritePatternExpressionFilter(node);
                    CurrentTypeWriter.Write(node, "?.");
                    CurrentTypeWriter.Write(node, "Deconstruct(");
                    CurrentTypeWriter.Write(node, string.Join(", ", variables.Select(v => $"{{ set $v(v) {{ $t{v.Value} = v }} }}")));
                    CurrentTypeWriter.Write(node, ");");
                }, true);
            }
            int ix = 0;
            foreach (var sub in node.Subpatterns)
            {
                if (sub.Pattern.IsKind(SyntaxKind.DiscardPattern))
                    continue;
                if (ix > 0)
                    CurrentTypeWriter.Write(node, " && ");
                CurrentTypeWriter.Write(node, "(");
                if (isTuple)
                {
                    WritePatternExpressionFilter(node);
                    CurrentTypeWriter.Write(node, ".");
                    CurrentTypeWriter.Write(node, "Item");
                    CurrentTypeWriter.Write(node, (ix + 1).ToString());
                    patternExpressionWrittenAlready++;
                    Visit(sub.Pattern);
                    patternExpressionWrittenAlready--;
                }
                else
                {
                    patternExpressions.Push($"$t{variables[sub]}");
                    Visit(sub.Pattern);
                    patternExpressions.Pop();
                }
                CurrentTypeWriter.Write(node, ")");
                ix++;
            }
            //base.VisitPositionalPatternClause(node);
        }

        class ListPatternBuidingContext
        {
            public int Items;
            public int PatternIndex;
            public int SpreadStartIndex = -1;
            public int SpreadRemainingElements = -1;
            public ISymbol? PatternSymbol = null;
            public IPropertySymbol? LenghtProperty = null;
            public int CurrentIndex
            {
                get
                {
                    if (SpreadStartIndex < 0)
                    {
                        return PatternIndex;
                    }
                    return -(Items - PatternIndex);
                }
            }

        }
        ListPatternBuidingContext? currentListPatternContext;
        public override void VisitListPattern(ListPatternSyntax node)
        {
            var patternExpression = GetPatternExpression(node);
            var patterySymbol = _global.TryGetSymbol(patternExpression, this);
            var patternType = patterySymbol != null ? _global.TryGetTypeSymbol(patterySymbol) : null;
            var lenghtPropertyName = "Length";
            var lenghtProperty = (IPropertySymbol?)(patternType?.GetMembers(null, _global).FirstOrDefault(m => m.Name == "Length") ??
                patternType?.GetMembers(null, _global).FirstOrDefault(m => m.Name == "Count"));
            if (lenghtProperty != null)
            {
                lenghtPropertyName = lenghtProperty.Name;
            }
            string lengthComparisonOperator = " === ";
            int countOffset = 0;
            if (node.Patterns.Any(p => p.IsKind(SyntaxKind.SlicePattern)))
            {
                lengthComparisonOperator = " >= ";
                countOffset = -1;
            }
            CurrentTypeWriter.Write(node, "(");
            bool isStaticConvention = lenghtProperty?.IsStaticCallConvention(_global) ?? false;
            bool hasTemplate = lenghtProperty?.GetTemplateAttribute(_global) != null;
            if (lenghtProperty != null)
            {
                WriteMemberAccess(node, new CodeNode(() => WritePatternExpressionFilter(node)), patternType, null, lenghtProperty);
                //if (isStaticConvention || hasTemplate)
                //{
                //    WriteMemberName(node, patteryType!, lenghtProperty, new CodeNode(() => WritePatternExpressionFilter(node)));
                //}
                //else
                //{
                //    WritePatternExpressionFilter(node);
                //    CurrentTypeWriter.Write(node, ".");
                //    WriteMemberName(node, patteryType!, lenghtProperty);
                //}
            }
            else
            {
                WritePatternExpressionFilter(node);
                CurrentTypeWriter.Write(node, ".length");
            }
            CurrentTypeWriter.Write(node, lengthComparisonOperator);
            CurrentTypeWriter.Write(node, (node.Patterns.Count + countOffset).ToString());
            ListPatternBuidingContext context = new();
            context.Items = node.Patterns.Count;
            context.PatternSymbol = patterySymbol;
            context.LenghtProperty = lenghtProperty;
            currentListPatternContext = context;
            foreach (var pattern in node.Patterns)
            {
                if (pattern.IsKind(SyntaxKind.SlicePattern))
                {
                    context.SpreadStartIndex = context.PatternIndex;
                    context.SpreadRemainingElements = node.Patterns.Count - context.PatternIndex;
                    var slice = (SlicePatternSyntax)pattern;
                    if (patternType != null && patternType.IsArray(out var elementType) && slice.Pattern is VarPatternSyntax varPattern)
                    {
                        CurrentTypeWriter.InsertAbove(node, $"let {((SingleVariableDesignationSyntax)varPattern.Designation).Identifier.ValueText};", true);
                        CurrentTypeWriter.Write(node, " && ");
                        CurrentTypeWriter.Write(node, $"({((SingleVariableDesignationSyntax)varPattern.Designation).Identifier.ValueText} = ");
                        WriteCreateSubArray(node, elementType, new CodeNode(() => WritePatternExpressionFilter(node, dereferenceListPattern: false)), new CodeNode(() =>
                        {
                            WriteCreateRange(node, new CodeNode(() =>
                            {
                                WriteCreateIndexFromStart(node, new CodeNode(() =>
                                {
                                    CurrentTypeWriter.Write(node, context.PatternIndex.ToString());
                                }));
                            }), new CodeNode(() =>
                            {
                                WriteCreateIndexFromStart(node, new CodeNode(() =>
                                {
                                    CurrentTypeWriter.Write(node, context.PatternIndex.ToString());
                                    CurrentTypeWriter.Write(node, " + ");
                                    WriteMemberAccess(node, new CodeNode(() => WritePatternExpressionFilter(node, dereferenceListPattern: false)), patternType, null, lenghtProperty);
                                    CurrentTypeWriter.Write(node, " - ");
                                    CurrentTypeWriter.Write(node, node.Patterns.Count(e => !e.IsKind(SyntaxKind.SlicePattern)).ToString());
                                }));
                            }));
                        }));
                        CurrentTypeWriter.Write(node, ")");
                    }
                }
                else if (pattern.IsKind(SyntaxKind.DiscardPattern))
                {
                }
                else
                {
                    CurrentTypeWriter.Write(node, " && ");
                    Visit(pattern);
                }
                context.PatternIndex++;
            }
            CurrentTypeWriter.Write(node, ")");
            currentListPatternContext = null;
            //base.VisitListPattern(node);
        }

        public override void VisitRecursivePattern(RecursivePatternSyntax node)
        {
            int ix = 0;
            bool hasOpeningBracket = false;
            if (node.Designation is SingleVariableDesignationSyntax sv)
            {
                CurrentTypeWriter.Write(node, "(");
                hasOpeningBracket = true;

                CurrentTypeWriter.InsertInCurrentClosure(node, $"let {sv.Identifier.ValueText};", true);
                CurrentTypeWriter.Write(node, $"{sv.Identifier.ValueText} = ");
                WritePatternExpressionFilter(node);
                CurrentTypeWriter.Write(node, ", ");
            }
            if (node.Type != null)
            {
                if (ix > 0)
                    CurrentTypeWriter.Write(node, " && ");
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.IsTypeName}(");
                WritePatternExpressionFilter(node);
                CurrentTypeWriter.Write(node, $", ");
                Visit(node.Type);
                CurrentTypeWriter.Write(node, $")");
                ix++;
            }
            if (node.PropertyPatternClause != null)
            {
                if (ix > 0)
                    CurrentTypeWriter.Write(node, " && ");
                Visit(node.PropertyPatternClause);
                ix++;
            }
            if (node.PositionalPatternClause != null)
            {
                if (ix > 0)
                    CurrentTypeWriter.Write(node, " && ");
                Visit(node.PositionalPatternClause);
                ix++;
            }
            if (ix == 0)
            {
                CurrentTypeWriter.Write(node, "true");
            }
            //VisitChildren(node.ChildNodes().Where(e => !e.IsKind(SyntaxKind.SingleVariableDesignation)));
            //base.VisitRecursivePattern(node);
            if (hasOpeningBracket)
                CurrentTypeWriter.Write(node, ")");
        }

        public override void VisitVarPattern(VarPatternSyntax node)
        {
            var varName = ((SingleVariableDesignationSyntax)node.Designation).Identifier.ValueText;
            CurrentTypeWriter.InsertAbove(node, $"let {varName};", true);
            CurrentTypeWriter.Write(node, $"{(node.Parent.IsKind(SyntaxKind.NotPattern) ? "!" : "")}({varName} = ");
            WritePatternExpressionFilter(node);
            CurrentTypeWriter.Write(node, $")");
            //CurrentTypeWriter.Write(node, $"{(node.Parent.IsKind(SyntaxKind.NotPattern) ? "!" : "")}{_global.GlobalName}.{Constants.IsTypeName}(");
            //WritePatternExpressionFilter(node);
            //CurrentTypeWriter.Write(node, $", ");
            //CurrentTypeWriter.Write(node, "null");
            //CurrentTypeWriter.Write(node, $", {{ set {Constants.RefValueName}(v){{ {varName} = v }} }}");
            //CurrentTypeWriter.Write(node, $")");
            //base.VisitVarPattern(node);
        }

        const string IsPatternExpressionVariableName = "__isPatternExpressionVariableName__";
        public override void VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            var declarationPattern = node.Pattern as DeclarationPatternSyntax;
            if ((node.Pattern is UnaryPatternSyntax un && un.Pattern is DeclarationPatternSyntax dp2))
            {
                declarationPattern = dp2;
            }
            if (declarationPattern != null)
            {
                //IdentifierNameSyntax? id = null;
                SingleVariableDesignationSyntax? svd = null;
                if (declarationPattern.Designation is SingleVariableDesignationSyntax isvd)
                {
                    //id = iid;
                    svd = isvd;
                }
                if (svd != null)
                {
                    CurrentTypeWriter.InsertInCurrentClosure(node, $"let {svd.Identifier.ValueText};", true);
                    //CurrentTypeWriter.Write(node, "(");
                    //CurrentTypeWriter.Write(node, svd.Identifier.ValueText);
                    //CurrentTypeWriter.Write(node, $" = ");
                    //Visit(node.Expression);
                    //CurrentTypeWriter.Write(node, $", ");
                }
                CurrentTypeWriter.Write(node, $"{(node.Pattern.IsKind(SyntaxKind.NotPattern) ? "!" : "")}{_global.GlobalName}.{Constants.IsTypeName}(");
                //if (svd != null)
                //{
                //    CurrentTypeWriter.Write(node, svd.Identifier.ValueText);
                //}
                //else
                //{
                Visit(node.Expression);
                //}
                CurrentTypeWriter.Write(node, $", ");
                Visit(declarationPattern.Type);
                if (svd != null)
                {
                    CurrentTypeWriter.Write(node, $", ");
                    CurrentTypeWriter.Write(node, $"{{ set {Constants.RefValueName}(v){{ {svd.Identifier.ValueText} = v; }} }}");
                }
                CurrentTypeWriter.Write(node, $")");
                //if (svd != null)
                //{
                //    CurrentTypeWriter.Write(node, ")");
                //}
                if (svd != null)
                {
                    var localSymbol = _global.TryGetSymbol(svd, this/*, out _, out _*/);
                    if (localSymbol != null)
                    {
                        CurrentClosure.DefineIdentifierType(svd.Identifier.ValueText, CodeSymbol.From(localSymbol));
                    }
                    else
                    {
                        CurrentClosure.DefineIdentifierType(svd.Identifier.ValueText, CodeSymbol.From(declarationPattern.Type, SymbolKind.Local));
                        //Writer.Write(node, $", {svd.Identifier.ValueText} = {id}");
                    }
                }
                //Visit(d.Pattern);
            }
            //else if (node.Pattern.IsKind(SyntaxKind.ConstantPattern))
            //{
            //    Visit(node.Expression);
            //    Writer.Write(node, " === ");
            //    Visit(node.Pattern);
            //}
            //else if (node.Pattern.IsKind(SyntaxKind.NotPattern))
            //{
            //    Visit(node.Expression);
            //    Writer.Write(node, " !== ");
            //    Visit(node.Pattern);
            //}
            //else if (node.Pattern is BinaryPatternSyntax bp)
            //{
            //    Visit(node.Expression);
            //    Visit(bp.Left);
            //    Writer.Write(node, bp.IsKind(SyntaxKind.AndPattern) ? " && " : " || ");
            //    Visit(node.Expression);
            //    Visit(bp.Right);
            //}
            else
            {
                bool needsVar = false && NeedsCachePatternExpressionInTempVariable(node.Expression);
                if (needsVar)
                {
                    //We used lazy variable evaluation because:

                    //If we have a statement like:
                    //if (provider.GetType() == typeof(CultureInfo) && ((CultureInfo)provider)._dateTimeInfo is { } info)

                    //This will typically produce:
                    //let $is1 = ($.$cast(provider, $.System.Globalization.CultureInfo))._dateTimeInfo;
                    //if ($.System.Type.op_Equality($.System.Object.GetType.call(provider), $.$typeof($.System.Globalization.CultureInfo)) && $is1 != null && ((info = $is1,..

                    //But this will fail as (CultureInfo)provider)._dateTimeInfo get evaluated before the type check provider.GetType() == typeof(CultureInfo)
                    //We therefore make the temp variable $is a lazy evaluation
                    var i = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                    CurrentClosure.Tags.Add(IsPatternExpressionVariableName, $"$is{i}.{Constants.LazyVariableValueName}");
                    CurrentTypeWriter.InsertInCurrentClosure(node, () =>
                    {
                        CurrentTypeWriter.Write(node, $"let $is{i} = ");
                        WriteLazyVariable(node, node.Expression);
                        //WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.LazyValue", arguments: [new CodeNode(() => {
                        //    Writer.Write(node, "() => ");
                        //    Visit(node.Expression);
                        //    //Writer.Write(node, ";");
                        //})]);
                    }, true);
                    //Visit(node.Expression);
                }
                bool testIfNotNull = true;
                var expressionValueType = _global.GetTypeSymbol(node.Expression, this);
                if (expressionValueType.IsValueType)
                {
                    testIfNotNull = false;
                }
                if (node.Pattern.IsKind(SyntaxKind.ConstantPattern) && (((ConstantPatternSyntax)node.Pattern).Expression.IsKind(SyntaxKind.NullLiteralExpression)))
                {
                    testIfNotNull = false;
                }
                if (node.Pattern.IsKind(SyntaxKind.ConstantPattern))
                {
                    var patternValueType = _global.GetTypeSymbol(((ConstantPatternSyntax)node.Pattern).Expression, this);
                    if (patternValueType.IsValueType)
                    {
                        testIfNotNull = false;
                    }
                }
                if (node.Pattern.IsKind(SyntaxKind.NotPattern))
                {
                    testIfNotNull = false;
                }
                if (testIfNotNull)
                {
                    WritePatternExpressionFilter(node);
                    CurrentTypeWriter.Write(node, " !== null && (");
                }
                Visit(node.Pattern);
                if (testIfNotNull)
                {
                    CurrentTypeWriter.Write(node, ")");
                }
                if (needsVar)
                {
                    CurrentClosure.Tags.Remove(IsPatternExpressionVariableName);
                }
            }
            //base.VisitIsPatternExpression(node);
        }
    }
}
