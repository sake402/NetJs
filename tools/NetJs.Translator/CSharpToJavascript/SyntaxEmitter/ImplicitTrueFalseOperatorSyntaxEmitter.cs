using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    internal class ImplicitTrueFalseOperatorSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (_processing.Value.TryPeek(out var top) && top == node)
                return false;
            foreach (var sm in visitor.SemanticModels)
            {
                if (node.SyntaxTree == sm.SyntaxTree)
                {
                    var conversion = sm.GetConversion(node);
                    if (conversion.Exists &&
                        conversion.IsImplicit)
                    {
                        var literalOperation = sm.GetOperation(node);
                        var convertOperation = literalOperation?.Parent as IUnaryOperation;
                        if (convertOperation?.OperatorMethod != null && SymbolEqualityComparer.Default.Equals(convertOperation.Type, visitor.Global.SystemBoolean))
                        {
                            _processing.Value.Push(node);
                            try
                            {
                                if (node is BinaryExpressionSyntax binary)
                                {
                                    visitor.WriteMethodInvocation(node, convertOperation.OperatorMethod, null, [binary.Left], null, null);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.CurrentTypeWriter.Write(node, binary.OperatorToken.ValueText);
                                    visitor.CurrentTypeWriter.Write(node, " ");
                                    visitor.WriteMethodInvocation(node, convertOperation.OperatorMethod, null, [binary.Right], null, null);
                                }
                                else
                                {
                                    visitor.WriteMethodInvocation(node, convertOperation.OperatorMethod, null, [node], null, null);
                                }
                            }
                            finally { _processing.Value.Pop(); }
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
