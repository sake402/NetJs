using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    internal class ImplicitConversionOfNullableToNullableStructSyntaxEmitter : SyntaxEmitter<ExpressionSyntax>
    {
        public override bool TryEmit(ExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (_processing.Value!.Contains(node))
                return false;
            if (node.Parent.IsKind(SyntaxKind.CoalesceExpression))
            {
                foreach (var sm in visitor.SemanticModels)
                {
                    if (node.SyntaxTree == sm.SyntaxTree)
                    {
                        var type = visitor.Global.GetTypeSymbol(node, visitor);
                        if (!type.IsValueType)
                        {
                            var conversion = sm.GetConversion(node);
                            if (conversion.Exists &&
                                conversion.IsImplicit)
                            {
                                var operation = sm.GetOperation(node);
                                var parentOperation = operation?.Parent as ICoalesceOperation;
                                if (operation != null && parentOperation != null)
                                {
                                    var fromType = type;
                                    var toType = parentOperation.Type!;
                                    if (!SymbolEqualityComparer.Default.Equals(fromType, toType) && toType.IsValueType)
                                    {
                                        IMethodSymbol? converterMethod = visitor.GetConverterMethod(fromType, toType);
                                        if (converterMethod != null)
                                        {
                                            _processing.Value.Push(node);
                                            try
                                            {
                                                var variableName = $"$t{++visitor.CurrentTypeWriter.CurrentClosure.NameManglingSeed}";
                                                visitor.CurrentTypeWriter.InsertInCurrentClosure(node, $"let {variableName};", true);
                                                visitor.CurrentTypeWriter.Write(node, "((");
                                                visitor.CurrentTypeWriter.Write(node, variableName);
                                                visitor.CurrentTypeWriter.Write(node, " = ");
                                                visitor.Visit(node);
                                                visitor.CurrentTypeWriter.Write(node, ") !== null ? ");
                                                visitor.WriteMethodInvocation(node, converterMethod, null, [new CodeNode(() => visitor.CurrentTypeWriter.Write(node, variableName))], null, null);
                                                visitor.CurrentTypeWriter.Write(node, " : null)");
                                                return true;
                                            }
                                            finally { _processing.Value.Pop(); }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
            }
            return false;
        }
    }
}
