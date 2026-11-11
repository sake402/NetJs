using LivingThing.Core.Frameworks.Common.OneOf;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    sealed class ProfileTraceMethodSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        static bool IsCandidate(TranslatorSyntaxVisitor visitor, SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.MethodDeclaration) ||
                node.IsKind(SyntaxKind.ConstructorDeclaration) ||
                node.IsKind(SyntaxKind.PropertyDeclaration) ||
                node.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                node.IsKind(SyntaxKind.GetAccessorDeclaration) ||
                node.IsKind(SyntaxKind.AddAccessorDeclaration) ||
                node.IsKind(SyntaxKind.RemoveAccessorDeclaration) ||
                node.IsKind(SyntaxKind.OperatorDeclaration) ||
                node.IsKind(SyntaxKind.ConversionOperatorDeclaration);
        }
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (!visitor.Global.BuildFlags.HasFlag(NetJsBuildFlags.ProfileTraceEntry))
                return false;
            if (node.IsKind(SyntaxKind.Block) || node.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                if (visitor.HasYield(node))
                    return false;
                if (_processing.Value.Contains(node))
                {
                    return false;
                }
                if (node.Parent != null && IsCandidate(visitor, node.Parent))
                {
                    _processing.Value.Push(node);
                    try
                    {
                        //void EmitWithTrace()
                        //{
                        //    var method = visitor.Global.GetSymbol(node.Parent, visitor);
                        //    var metadata = visitor.Global.GetRequiredMetadata(method);
                        //    if (visitor.Global.BuildFlags.HasFlag(NetJsBuildFlags.ProfileTraceExit))
                        //    {
                        //        visitor.CurrentTypeWriter.WriteLine(node, $"try", true);
                        //        visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                        //    }
                        //    visitor.CurrentTypeWriter.WriteLine(node, $"//--TRIMMABLE-BOUNDARY-START(TRACE)", true);
                        //    visitor.CurrentTypeWriter.WriteLine(node, $"{visitor.Global.GlobalName}.{Constants.TraceEntryName}({metadata.Export.Handle});", true);
                        //    visitor.CurrentTypeWriter.WriteLine(node, $"//--TRIMMABLE-BOUNDARY-END(TRACE)", true);
                        //    visitor.MarkBlockBraceWritten(node);
                        //    visitor.Visit(node);
                        //    if (visitor.Global.BuildFlags.HasFlag(NetJsBuildFlags.ProfileTraceExit))
                        //    {
                        //        visitor.CurrentTypeWriter.WriteLine(node, "}", true);
                        //        visitor.CurrentTypeWriter.WriteLine(node, "finally", true);
                        //        visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                        //        visitor.CurrentTypeWriter.WriteLine(node, $"//--TRIMMABLE-BOUNDARY-START(TRACE)", true);
                        //        visitor.CurrentTypeWriter.WriteLine(node, $"{visitor.Global.GlobalName}.{Constants.TraceExitName}({metadata.Export.Handle});", true);
                        //        visitor.CurrentTypeWriter.WriteLine(node, $"//--TRIMMABLE-BOUNDARY-END(TRACE)", true);
                        //        visitor.CurrentTypeWriter.WriteLine(node, "}", true);
                        //    }
                        //}
                        //if (node.IsKind(SyntaxKind.Block))  //for arrow clause, the brace is already written by method visitor
                        //{
                        visitor.WriteBlock(node, new CodeNode(() =>
                        {
                            var method = visitor.Global.GetSymbol(node.Parent, visitor);
                            var metadata = visitor.Global.GetRequiredMetadata(method);
                            if (visitor.Global.BuildFlags.HasFlag(NetJsBuildFlags.ProfileTraceExit))
                            {
                                visitor.CurrentTypeWriter.WriteLine(node, $"try", true);
                                visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                            }
                            visitor.WriteTraceEntry(node, metadata.Export.Handle);
                            visitor.MarkBlockBraceWritten(node);
                            visitor.Visit(node);
                            if (visitor.Global.BuildFlags.HasFlag(NetJsBuildFlags.ProfileTraceExit))
                            {
                                visitor.CurrentTypeWriter.WriteLine(node, "}", true);
                                visitor.CurrentTypeWriter.WriteLine(node, "finally", true);
                                visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                                visitor.WriteTraceExit(node, metadata.Export.Handle);
                                visitor.CurrentTypeWriter.WriteLine(node, "}", true);
                            }
                        }));
                        //}
                        //else
                        //{
                        //    EmitWithTrace();
                        //}
                        //CodeWriterClosure? blockClosure = null;
                        //if (node.IsKind(SyntaxKind.Block)) //for arrow clause, the brace is already written by method visitor
                        //{
                        //    visitor.CurrentTypeWriter.WriteLine(node, "{", true);
                        //    blockClosure = visitor.CurrentTypeWriter.CurrentClosure;
                        //    visitor.OpenClosure(node);
                        //    visitor.MarkBlockBraceWritten((BlockSyntax)node);
                        //}
                        //Emit();
                        //if (node.IsKind(SyntaxKind.Block))
                        //{
                        //    visitor.CloseClosure(node);
                        //    blockClosure.RaiseOnBlockClosing();
                        //    visitor.CurrentTypeWriter.WriteLine(node, "}", true);
                        //}
                        return true;
                    }
                    finally
                    {
                        _processing.Value.Pop();
                    }
                }
            }
            return false;
        }
    }
}
