using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public override void VisitTupleElement(TupleElementSyntax node)
        {
            base.VisitTupleElement(node);
        }

        public override void VisitTupleExpression(TupleExpressionSyntax node)
        {
            if (node.Parent is AssignmentExpressionSyntax assignment)
            {
                if (assignment.Right == node)
                {
                    //assigning tuple to tuple in an expression like (T start, T end) = (_start, _endExclusive);
                    //should create a simple object desctructured back to the lhs a simple
                    //no need to instantiate a tuple type
                    CurrentTypeWriter.Write(node, "{ ");
                    int i = 0;
                    foreach (var e in node.Arguments)
                    {
                        if (i > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        CurrentTypeWriter.Write(node, "Item");
                        CurrentTypeWriter.Write(node, (i + 1).ToString());
                        CurrentTypeWriter.Write(node, ": ");
                        Visit(e.Expression);
                        i++;
                    }
                    CurrentTypeWriter.Write(node, " }");
                }
                else
                {
                    if (node.Arguments.All(a => a.Expression.IsKind(SyntaxKind.DeclarationExpression)))
                    {
                        CurrentTypeWriter.Write(node, "const { ");
                        int i = 0;
                        foreach (var e in node.Arguments)
                        {
                            if (i > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            CurrentTypeWriter.Write(node, "Item");
                            CurrentTypeWriter.Write(node, (i + 1).ToString());
                            CurrentTypeWriter.Write(node, ": ");
                            if (e.Expression is DeclarationExpressionSyntax de)
                            {
                                Visit(de.Designation);
                            }
                            else
                            {
                                Visit(e.Expression);
                            }
                            i++;
                        }
                        CurrentTypeWriter.Write(node, " }");
                    }
                    else
                    {
                        foreach (var e in node.Arguments)
                        {
                            if (e.Expression is DeclarationExpressionSyntax de)
                            {
                                Visit(de);
                                CurrentTypeWriter.WriteLine(node, ";");
                            }
                        }
                        if (false)
                        {
                            CurrentTypeWriter.Write(node, "{ ");
                            int i = 0;
                            foreach (var e in node.Arguments)
                            {
                                if (i > 0)
                                    CurrentTypeWriter.Write(node, ", ");
                                CurrentTypeWriter.Write(node, "Item");
                                CurrentTypeWriter.Write(node, (i + 1).ToString());
                                CurrentTypeWriter.Write(node, ": ");
                                if (e.Expression is DeclarationExpressionSyntax de)
                                {
                                    Visit(de.Designation);
                                }
                                else
                                {
                                    Visit(e.Expression);
                                }
                                i++;
                            }
                            CurrentTypeWriter.Write(node, " }");
                        }
                        else
                        {
                            CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.TupleUnPack}(($tp) =>");
                            CurrentTypeWriter.WriteLine(node, "{", true);
                            int ix = 0;
                            foreach (var arg in node.Arguments)
                            {
                                CurrentTypeWriter.Write(node, "", true);
                                WriteVariableAssignment(node, arg.Expression is DeclarationExpressionSyntax de ? de.Designation : arg.Expression, null, "=", new CodeNode(() =>
                                {
                                    CurrentTypeWriter.Write(node, $"$tp.Item{(ix + 1)}");
                                }), rhs: _global.TryGetTypeSymbol(arg.Expression, this));
                                //if (arg.Expression is DeclarationExpressionSyntax de)
                                //{
                                //    Visit(de.Designation);
                                //}
                                //else
                                //{
                                //    Visit(arg.Expression);
                                //}
                                //Writer.Write(node, " = ");
                                //Writer.Write(node, "$tp.Item");
                                //Writer.Write(node, ix.ToString());
                                CurrentTypeWriter.WriteLine(node, ";");
                                ix++;
                            }
                            CurrentTypeWriter.Write(node, "}).$v", true);
                        }
                    }
                }
            }
            else
            {
                var tupleStruct = (INamedTypeSymbol)_global.GetSymbol($"System.ValueTuple<{string.Join(",", Enumerable.Range(1, node.Arguments.Count).Select(s => ""))}>", this/*, out _, out _*/);
                var argTypes = node.Arguments.Select(a => _global.TryGetTypeSymbol(a, this) ?? throw new InvalidOperationException("Cannot result tuple generic argument")).ToArray();
                tupleStruct = tupleStruct.Construct(argTypes);
                var tupleConstructor = (IMethodSymbol)tupleStruct.GetMembers(".ctor").Where(e => ((IMethodSymbol)e).Parameters.Count() == node.Arguments.Count).Single();
                WriteConstructorCall(node, tupleStruct, tupleConstructor, null, node.Arguments.Select(e => new CodeNode(e)), default);
                //Writer.Write(node, "{ ");
                //int i = 0;
                //foreach (var e in node.Arguments)
                //{
                //    if (i > 0)
                //        Writer.Write(node, ", ");
                //    //if (e.NameColon == null)
                //    //{
                //    Writer.Write(node, $"Item{i + 1}: ");
                //    //}
                //    //The namecolon are syntatic sugar, we still reference them in runtime as ItemX
                //    Visit(e.Expression);
                //    i++;
                //}
                //Writer.Write(node, " }");
            }
            //base.VisitTupleExpression(node);
        }

        public override void VisitTupleType(TupleTypeSyntax node)
        {
            CurrentTypeWriter.Write(node, $"System.ValueTuple(");
            int i = 0;
            foreach (var e in node.Elements)
            {
                if (i > 0)
                    CurrentTypeWriter.Write(node, ", ");
                Visit(e);
                i++;
            }
            CurrentTypeWriter.Write(node, $")");
            //base.VisitTupleType(node);
        }
    }
}
