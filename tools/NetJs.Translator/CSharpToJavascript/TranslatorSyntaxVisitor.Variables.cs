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
        public void WriteLazyVariable(CSharpSyntaxNode node, ExpressionSyntax expression)
        {
            WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.LazyValue", arguments: [new CodeNode(() =>
            {
                CurrentTypeWriter.Write(node, "() => ");
                Visit(expression);
                //Writer.Write(node, ";");
            })]);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            //var closure = CurrentTypeWriter.CurrentClosure;
            //if (node.UsingKeyword.Text.Length > 0)
            //{
            //    CurrentTypeWriter.WriteLine(node, "try", true);
            //    CurrentTypeWriter.WriteLine(node, "{", true);
            //}
            CurrentTypeWriter.Write(node, "", true);
            base.VisitLocalDeclarationStatement(node);
            CurrentTypeWriter.WriteLine(node, ";");
            if (node.UsingKeyword.Text.Length > 0)
            {
                //closure.OnClosing += (s, e) =>
                //{
                //CurrentTypeWriter.WriteLine(node, "}", true);
                //CurrentTypeWriter.WriteLine(node, "finally", true);
                //CurrentTypeWriter.WriteLine(node, "{", true);
                foreach (var variable in node.Declaration.Variables)
                {
                    WriteMethodInvocation(node, "System.IDisposable.Dispose", lhsExpression: new CodeNode(() =>
                    {
                        CurrentTypeWriter.Write(node, $"{variable.Identifier.ValueText}?", true);
                    }));
                    CurrentTypeWriter.WriteLine(node, ";");
                }
                //CurrentTypeWriter.WriteLine(node, "}", true);
                //};
            }
        }

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            EnsureImported(node.Type);
            var vType = node.Type;
            //bool isRef = false;
            if (vType.IsKind(SyntaxKind.RefType))
            {
                vType = ((RefTypeSyntax)vType).Type;
                //isRef = true;
            }
            var variableType = _global.TryGetSymbol(vType, this/*, out _, out _*/);
            IDisposable? disposeDelegateType = null;
            if (variableType is ITypeSymbol ts)
            {
                if (ts.IsDelegate(out var delegateReturnType, out var delegateParameterTypes))
                {
                    disposeDelegateType = CurrentClosure.DefineAnonymousMethodParameterTypes(delegateReturnType == null ? delegateParameterTypes! : [.. delegateParameterTypes!, delegateReturnType]);
                }
            }
            //In a goto state machine, we already define this variable in the initializer phase
            if (gotoVariableDeclarationActive)
            {
                CurrentTypeWriter.Write(node, $"/*{node.Type.ToString().Trim()}*/ ", true);
                CurrentTypeWriter.Write(node, $"let ", true);
            }
            else if (!GotoHasDefinedVariable(node))
            {
                CurrentTypeWriter.Write(node, $"/*{node.Type.ToString().Trim()}*/ ");
                CurrentTypeWriter.Write(node, $"let ");
            }
            VisitChildren(node.Variables, ", ");
            disposeDelegateType?.Dispose();
            //base.VisitVariableDeclaration(node);
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (gotoVariableDeclarationActive)
            {
                CurrentTypeWriter.Write(node, node.Identifier.Text);
                return;
            }
            var parent = (VariableDeclarationSyntax)node.Parent!;
            CurrentTypeWriter.Write(node, node.Identifier.ResolveIdentifierName());
            var localSymbol = _global.TryGetSymbol(node, this/*, out _, out _*/);
            if (localSymbol != null)
            {
                CurrentClosure.DefineIdentifierType(node.Identifier.ValueText, CodeSymbol.From(localSymbol));
            }
            else if (!parent.Type.IsVar)
            {
                //Register type lookups for this vairable in the current closure
                CurrentClosure.DefineIdentifierType(node.Identifier.ValueText, CodeSymbol.From(parent.Type, SymbolKind.Local));
            }
            else if (node.Initializer != null)
            {
                var returnType = GetExpressionReturnSymbol(node.Initializer.Value);
                //Register type lookups for this vairable in the current closure
                CurrentClosure.DefineIdentifierType(node.Identifier.ValueText, returnType);
            }
            else if (memberAccesChainCurrentType.TypeSyntaxOrSymbol != null)
            {
                CurrentClosure.DefineIdentifierType(node.Identifier.ValueText, memberAccesChainCurrentType);
            }
            base.VisitVariableDeclarator(node);
        }

        public override void VisitDeclarationExpression(DeclarationExpressionSyntax node)
        {
            if (node.Designation.IsKind(SyntaxKind.DiscardDesignation))
            {
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.DiscardRefName}");
            }
            else
            {
                CurrentTypeWriter.Write(node, $"/*{node.Type}*/ ");
                CurrentTypeWriter.Write(node, $"let ");
                Visit(node.Designation);
            }
            //base.VisitDeclarationExpression(node);
        }

        public override void VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
        {
            //Writer.InsertInCurrentClosure($"let {node.Identifier.ValueText};", true);
            CurrentTypeWriter.Write(node, node.Identifier.ResolveIdentifierName());
            //base.VisitSingleVariableDesignation(node);
        }

        public override void VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
            var switchExpression = node.FindClosestParent<SwitchExpressionSyntax>();
            if (switchExpression != null)
            {
                HandleDeclarationPatternInSwitchExpression(node);
            }
            else
            {
                var switchStatement = node.FindClosestParent<SwitchStatementSyntax>();
                if (switchStatement != null)
                {
                    HandleDeclarationPatternInSwitchStatement(node);
                }
                else
                {
                    base.VisitDeclarationPattern(node);
                }
            }
        }

    }
}
