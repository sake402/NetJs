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
        void WriteLambdaExpression(CSharpSyntaxNode node, string? modifiers, IEnumerable<ParameterSyntax>? lamdaParameters, CSharpSyntaxNode? body)
        {
            var previousClosure = CurrentClosure;
            OpenClosure(node);
            if (lamdaParameters != null)
            {
                IEnumerable<ISymbol>? inferedParameters = null;
                int ix = 0;
                foreach (var parameter in lamdaParameters)
                {
                    var localSymbol = _global.TryGetSymbol(parameter, this/*, out _, out _*/);
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
            var parameters = string.Join(", ", lamdaParameters?.Select((p, i) => $"/*{p.Type?.ToString().Trim() ?? _global.TryGetSymbol(p.Identifier.Text, this)?.Name}*/ {(p.Identifier.Text == "_" ? $"_{i}" : p.Identifier.Text)}") ?? Enumerable.Empty<string>());
            CurrentTypeWriter.WriteLine(node, $"/*{modifiers}*/ ({parameters}) =>");
            CurrentTypeWriter.WriteLine(node, "{", true);
            //var child = node.ChildNodes().Where(t => !t.IsKind(SyntaxKind.ParameterList)/* is not ParameterListSyntax*/ && !t.IsKind(SyntaxKind.Parameter)/* is not ParameterSyntax*/);
            bool implicitReturn = false;
            bool isThrow = false;
            if (body.IsKind(SyntaxKind.ThrowExpression))
                isThrow = true;
            if (body is BlockSyntax block)
            {
                //body = block.Statements;
            }
            else
            {
                implicitReturn = body is not ReturnStatementSyntax;
            }
            if (implicitReturn)
            {
                if (!isThrow)
                    CurrentTypeWriter.Write(node, "return ", true);
                else
                    CurrentTypeWriter.Write(node, "", true);
            }
            Visit(body);
            if (implicitReturn)
            {
                CurrentTypeWriter.WriteLine(node, ";");
            }
            else
            {
                CurrentTypeWriter.EnsureNewLine();
            }
            CurrentTypeWriter.Write(node, "}", true);
            CloseClosure();
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            WriteLambdaExpression(node, GetMethodModifier(node, node.Modifiers, null), node.ParameterList?.Parameters, node.Body??node.ExpressionBody);
            //base.VisitAnonymousMethodExpression(node);
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            WriteLambdaExpression(node, GetMethodModifier(node, node.Modifiers, null), node.ParameterList.Parameters, node.Body ?? node.ExpressionBody);
            //base.VisitParenthesizedLambdaExpression(node);
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            WriteLambdaExpression(node, GetMethodModifier(node, node.Modifiers, null), [node.Parameter], node.Body ?? node.ExpressionBody);
            //base.VisitSimpleLambdaExpression(node);
        }

    }
}
