using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public override void VisitInterpolation(InterpolationSyntax node)
        {
            var handler = (ITypeSymbol)_global.GetSymbol("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", this);
            var appendFormattedObjectMethod = handler.GetMembers("AppendFormatted")
                .Cast<IMethodSymbol>()
                .Single(e => e.Parameters.Count() == 3 &&
                        e.Parameters[0].Type.Equals(_global.SystemObject, SymbolEqualityComparer.Default) &&
                        e.Parameters[1].Type.Equals(_global.SystemInt32, SymbolEqualityComparer.Default) &&
                        e.Parameters[2].Type.Equals(_global.SystemString, SymbolEqualityComparer.Default));
            CurrentTypeWriter.Write(node, $"$handler.", true);
            WriteMemberName(node, handler, appendFormattedObjectMethod);
            CurrentTypeWriter.Write(node, $"(");
            var expressionType = _global.GetTypeSymbol(node.Expression, this);
            if (!SymbolEqualityComparer.Default.Equals(expressionType, _global.SystemObject))
            {
                CurrentTypeWriter.Write(node, _global.GlobalName);
                CurrentTypeWriter.Write(node, ".");
                CurrentTypeWriter.Write(node, Constants.BoxName);
                CurrentTypeWriter.Write(node, "(");
                Visit(node.Expression);
                CurrentTypeWriter.Write(node, ", ");
                CurrentTypeWriter.Write(node, expressionType.ComputeOutputTypeName(_global));
                CurrentTypeWriter.Write(node, ")");
            }
            else
            {
                Visit(node.Expression);
            }
            CurrentTypeWriter.Write(node, $", ");
            if (node.AlignmentClause != null)
            {
                Visit(node.AlignmentClause);
            }
            else
                CurrentTypeWriter.Write(node, "null");
            CurrentTypeWriter.Write(node, $", ");
            if (node.FormatClause != null)
            {
                CurrentTypeWriter.Write(node, $"\"");
                CurrentTypeWriter.Write(node, node.FormatClause.FormatStringToken.ValueText);
                CurrentTypeWriter.Write(node, $"\"");
            }
            else
                CurrentTypeWriter.Write(node, "null");
            CurrentTypeWriter.WriteLine(node, $");");
            //base.VisitInterpolation(node);
        }

        public override void VisitInterpolatedStringText(InterpolatedStringTextSyntax node)
        {
            var handler = (ITypeSymbol)_global.GetSymbol("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", this);
            CurrentTypeWriter.Write(node, $"$handler.", true);
            var appendLiteralStringMethod = handler.GetMembers("AppendLiteral")
                .Cast<IMethodSymbol>()
                .Single(e => e.Parameters.Count() == 1 &&
                        e.Parameters[0].Type.Equals(_global.SystemString, SymbolEqualityComparer.Default));
            WriteMemberName(node, handler, appendLiteralStringMethod);
            CurrentTypeWriter.Write(node, $"(\"");
            CurrentTypeWriter.Write(node, node.TextToken.ValueText.EscapeString());
            CurrentTypeWriter.WriteLine(node, $"\");");
        }

        public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            //Use native js interpolation for simple interpolation types
            if (node.Contents.All(e => e is InterpolatedStringTextSyntax || (e is InterpolationSyntax token && token.FormatClause == null && token.AlignmentClause == null)))
            {
                CurrentTypeWriter.Write(node, "`");
                foreach (var token in node.Contents)
                {
                    if (token is InterpolatedStringTextSyntax str)
                    {
                        CurrentTypeWriter.Write(node, str/*.ToFullString()*/.TextToken.ValueText.EscapeString().Replace("`", "\\`").Replace("$", "\\$"));
                    }
                    else if (token is InterpolationSyntax format)
                    {
                        CurrentTypeWriter.Write(node, "${");
                        var type = _global.TryGetTypeSymbol(format.Expression, this);
                        string? formatSpecifier = null;
                        //Cant handle char like a regular primitive. THough char is a numeric type, its conversion to string is not numeric
                        if (type != null &&
                            !SymbolEqualityComparer.Default.Equals(type, _global.SystemString) &&
                            (!type.IsJsPrimitive() || SymbolEqualityComparer.Default.Equals(type, _global.SystemChar)))
                        {
                            var toString = type.GetMembers("ToString", _global).Where(e => e is IMethodSymbol m && m.Parameters.Count() == (formatSpecifier == null ? 0 : 1)).Cast<IMethodSymbol>().FirstOrDefault();
                            if (toString != null)
                            {
                                if (IsGenericDispatch(type, toString))
                                {
                                    CurrentTypeWriter.Write(node, _global.GlobalName);
                                    CurrentTypeWriter.Write(node, ".");
                                    CurrentTypeWriter.Write(node, Constants.Dispatch);
                                    CurrentTypeWriter.Write(node, "(");
                                    VisitNode(format.Expression);
                                    CurrentTypeWriter.Write(node, ", ");
                                    CurrentTypeWriter.Write(node, type.Name);
                                    CurrentTypeWriter.Write(node, ", ");
                                    CurrentTypeWriter.Write(node, "($t) => ");
                                    CurrentTypeWriter.Write(node, "$t");
                                    WriteMemberName(node, type, toString);
                                    //WriteMethodInvocation(node, toString, null, null, new CodeNode(() => CurrentTypeWriter.Write(node, "$t")), null, null, false);
                                    CurrentTypeWriter.Write(node, ")");
                                }
                                else
                                {
                                    WriteMethodInvocation(node, toString, null, null, format.Expression, null, null, false);
                                }
                            }
                            else
                            {
                                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.ToStringName}(");
                                Visit(format.Expression);
                                CurrentTypeWriter.Write(node, $", \"\"");
                                CurrentTypeWriter.Write(node, $")");
                            }
                        }
                        else
                        {
                            if (type?.IsJsPrimitive() ?? false)
                            {
                                Visit(format.Expression);
                            }
                            else if (type?.IsType("System.String") ?? false)
                            {
                                Visit(format.Expression);
                            }
                            else
                            {
                                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.ToStringName}(");
                                Visit(format.Expression);
                                CurrentTypeWriter.Write(node, $", \"\"");
                                CurrentTypeWriter.Write(node, $")");
                            }
                        }
                        CurrentTypeWriter.Write(node, "}");
                    }
                }
                CurrentTypeWriter.Write(node, "`");
            }
            else
            {
                var handler = (ITypeSymbol)_global.GetSymbol("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", this);
                var sint = (ITypeSymbol)_global.GetSymbol("System.Int32", this);
                var constructor = handler.GetMembers(".ctor").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 2 && e.Parameters[0].Type.Equals(sint, SymbolEqualityComparer.Default) && e.Parameters[1].Type.Equals(sint, SymbolEqualityComparer.Default));
                int literalLenght = node.Contents.Count(e => !e.IsKind(SyntaxKind.Interpolation));
                int formattedLenght = node.Contents.Count(e => e.IsKind(SyntaxKind.Interpolation));

                WrapStatementsInExpression(node, () =>
                {
                    CurrentTypeWriter.Write(node, $"var $handler = ", true);
                    WriteConstructorCall(node, handler, constructor, null, [SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(literalLenght)), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(formattedLenght))]);
                    CurrentTypeWriter.WriteLine(node, $";");
                    foreach (var token in node.Contents)
                    {
                        Visit(token);
                    }
                    CurrentTypeWriter.Write(node, "return $handler.", true);
                    WriteMemberName(node, handler, "ToStringAndClear");
                    CurrentTypeWriter.WriteLine(node, "();", true);
                });
                //CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.Expression}(function()");
                //CurrentTypeWriter.WriteLine(node, $"{{", true);
                //CurrentTypeWriter.Write(node, $"var $handler = ", true);
                //WriteConstructorCall(node, handler, constructor, null, [SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(literalLenght)), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(formattedLenght))]);
                //CurrentTypeWriter.WriteLine(node, $";");
                //foreach (var token in node.Contents)
                //{
                //    Visit(token);
                //}
                //CurrentTypeWriter.WriteLine(node, "return $handler.ToStringAndClear();", true);
                //CurrentTypeWriter.Write(node, $"}})", true);
            }
            //base.VisitInterpolatedStringExpression(node);
        }
    }
}
