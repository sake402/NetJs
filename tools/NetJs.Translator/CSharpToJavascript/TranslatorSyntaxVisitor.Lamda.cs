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
        void WriteLambdaExpression(CSharpSyntaxNode node, string? modifiers, IEnumerable<ParameterSyntax>? lamdaParameters)
        {
            var previousClosure = CurrentClosure;
            OpenClosure(node);
            if (lamdaParameters != null)
            {
                IEnumerable<ISymbol>? inferedParameters = null;
                int ix = 0;
                foreach (var parameter in lamdaParameters)
                {
                    var localSymbol = _global.TryGetTypeSymbol(parameter, this/*, out _, out _*/);
                    if (localSymbol != null)
                    {
                        CurrentClosure.DefineIdentifierType(parameter.Identifier.Text, CodeSymbol.From(localSymbol));
                    }
                    else
                    {
                        if (parameter.Type == null)
                        {
                            inferedParameters ??= previousClosure.GetAnonymousMethodParameterTypes();
                        }
                        if (parameter.Type != null)
                        {
                            CurrentClosure.DefineIdentifierType(parameter.Identifier.Text, parameter.Type, SymbolKind.Parameter);
                        }
                        else if (inferedParameters != null)
                        {
                            var parameterType = inferedParameters.ElementAt(ix);
                            CurrentClosure.DefineIdentifierType(parameter.Identifier.Text, CodeSymbol.From(parameterType));
                        }
                    }
                    ix++;
                }
            }
            var parameters = string.Join(", ", lamdaParameters?.Select(p => $"/*{p.Type?.ToFullString().Trim() ?? _global.ResolveSymbol(GetIdentifierTypeInScope(p.Identifier.Text), this/*, out _, out _*/)?.GetTypeSymbol()?.Name}*/ {p.Identifier.Text}") ?? Enumerable.Empty<string>());
            CurrentTypeWriter.WriteLine(node, $"/*{modifiers}*/ ({parameters}) =>");
            CurrentTypeWriter.WriteLine(node, "{", true);
            var child = node.ChildNodes().Where(t => !t.IsKind(SyntaxKind.ParameterList)/* is not ParameterListSyntax*/ && !t.IsKind(SyntaxKind.Parameter)/* is not ParameterSyntax*/);
            bool implicitReturn = false;
            bool isThrow = false;
            if (child.Count() == 1 && child.Single().IsKind(SyntaxKind.ThrowExpression))
                isThrow = true;
            if (child.Count() == 1 && child.Single() is BlockSyntax block)
            {
                child = block.Statements;
            }
            else
            {
                implicitReturn = child.Count() == 1 && child.Single() is not ReturnStatementSyntax;
            }
            if (implicitReturn)
            {
                if (!isThrow)
                    CurrentTypeWriter.Write(node, "return ", true);
                else
                    CurrentTypeWriter.Write(node, "", true);
            }
            VisitChildren(child);
            if (implicitReturn)
            {
                CurrentTypeWriter.WriteLine(node, ";");
            }
            else
            {
                CurrentTypeWriter.EnsureNewLine();
            }
            bool _static = modifiers?.Contains("static") ?? false;
            CurrentTypeWriter.Write(node, "}", true);
            CloseClosure();
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            WriteLambdaExpression(node, GetMethodModifier(node, node.Modifiers, null), node.ParameterList?.Parameters);
            //base.VisitAnonymousMethodExpression(node);
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            WriteLambdaExpression(node, GetMethodModifier(node, node.Modifiers, null), node.ParameterList.Parameters);
            //base.VisitParenthesizedLambdaExpression(node);
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            WriteLambdaExpression(node, GetMethodModifier(node, node.Modifiers, null), [node.Parameter]);
            //base.VisitSimpleLambdaExpression(node);
        }

    }
}
