using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Indexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static NetJs.Translator.CSharpToJavascript.ILLinkerAssembly;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public override void VisitTupleElement(TupleElementSyntax node)
        {
            base.VisitTupleElement(node);
        }

        bool IsSimpleTupleDeclaration(TupleExpressionSyntax node)
        {
            return node.Arguments.All(a => a.Expression.IsKind(SyntaxKind.DeclarationExpression));
        }

        public override void VisitTupleExpression(TupleExpressionSyntax node)
        {
            if (node.Parent is AssignmentExpressionSyntax assignment)
            {
                if (assignment.Right == node)
                {
                    if (assignment.Left.IsKind(SyntaxKind.TupleExpression))
                    {
                        //assigning tuple to tuple in an expression like (T start, T end) = (_start, _endExclusive);
                        //should create a simple object desctructured back to the lhs a simple
                        //no need to instantiate a tuple type
                        CurrentTypeWriter.Write(node, "{ ");
                        int i = 0;
                        var tuple = _global.SystemValueTuple(node.Arguments.Count);
                        var items = tuple.GetMembers().Where(m => m.Name.StartsWith("Item")).OrderBy(e => e.Name).ToArray();
                        foreach (var e in node.Arguments)
                        {
                            if (i > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            WriteMemberName(node, tuple, items[i]);
                            //CurrentTypeWriter.Write(node, "Item");
                            //CurrentTypeWriter.Write(node, (i + 1).ToString());
                            CurrentTypeWriter.Write(node, ": ");
                            Visit(e.Expression);
                            i++;
                        }
                        CurrentTypeWriter.Write(node, " }");
                    }
                    else
                    {
                        WriteCreateTuple(node, node.Arguments);
                    }
                }
                else
                {
                    if (IsSimpleTupleDeclaration(node))
                    {
                        CurrentTypeWriter.Write(node, "let [ ");
                        int i = 0;
                        foreach (var e in node.Arguments)
                        {
                            if (i > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            Visit(((DeclarationExpressionSyntax)e.Expression).Designation);
                            i++;
                        }
                        CurrentTypeWriter.Write(node, " ]");

                        //CurrentTypeWriter.Write(node, "const { ");
                        //int i = 0;
                        //var tuple = _global.SystemValueTuple(node.Arguments.Count);
                        //var items = tuple.GetMembers().Where(m => m.Name.StartsWith("Item")).OrderBy(e => e.Name).ToArray();
                        //foreach (var e in node.Arguments)
                        //{
                        //    if (i > 0)
                        //        CurrentTypeWriter.Write(node, ", ");
                        //    WriteMemberName(node, tuple, items[i]);
                        //    //CurrentTypeWriter.Write(node, "Item");
                        //    //CurrentTypeWriter.Write(node, (i + 1).ToString());
                        //    CurrentTypeWriter.Write(node, ": ");
                        //    if (e.Expression is DeclarationExpressionSyntax de)
                        //    {
                        //        Visit(de.Designation);
                        //    }
                        //    else
                        //    {
                        //        Visit(e.Expression);
                        //    }
                        //    i++;
                        //}
                        //CurrentTypeWriter.Write(node, " }");
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
                            var tuple = _global.SystemValueTuple(node.Arguments.Count);
                            var items = tuple.GetMembers().Where(m => m.Name.StartsWith("Item")).OrderBy(e => e.Name).ToArray();
                            foreach (var arg in node.Arguments)
                            {
                                CurrentTypeWriter.Write(node, "", true);
                                var rhsType = _global.TryGetTypeSymbol(arg.Expression, this);
                                var value = new CodeNode(() =>
                                {
                                    CurrentTypeWriter.Write(node, $"$tp.");
                                    WriteMemberName(node, tuple, items[ix]);
                                    //CurrentTypeWriter.Write(node, $"$tp.Item{(ix + 1)}");
                                });
                                if (arg.Expression.IsKind(SyntaxKind.ElementAccessExpression) && SpanSetItemSyntaxEmitter.TryWrite(node, arg.Expression, value, this, isForceWrite: true))
                                {
                                }
                                else
                                    WriteVariableAssignment(node, arg.Expression is DeclarationExpressionSyntax de ? de.Designation : arg.Expression, null, "=", value, rhs: rhsType);
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
                WriteCreateTuple(node, node.Arguments);
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
