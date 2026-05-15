using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public override void VisitTryStatement(TryStatementSyntax node)
        {
            CurrentTypeWriter.WriteLine(node, "try", true);
            Visit(node.Block);
            var catches = node.ChildNodes().Where(e => e.IsKind(SyntaxKind.CatchClause)/* is CatchClauseSyntax*/).Cast<CatchClauseSyntax>();
            if (catches.Count() > 1)
            {
                CurrentTypeWriter.WriteLine(node, "catch($e)", true);
                CurrentTypeWriter.WriteLine(node, "{", true);
                int iDeclaration = 0;
                foreach (var _catch in catches.Where(e => e.Declaration != null))
                {
                    CurrentTypeWriter.Write(node, $"{(iDeclaration > 0 ? "else " : "")}if ($e instanceof ", true);
                    Visit(_catch.Declaration!.Type);
                    CurrentTypeWriter.WriteLine(node, $")");
                    if (!string.IsNullOrEmpty(_catch.Declaration!.Identifier.ValueText))
                    {
                        CurrentTypeWriter.WriteLine(node, "{", true);
                        CurrentTypeWriter.WriteLine(node, $"let {_catch.Declaration!.Identifier.ValueText} = $e;", true);
                        VisitChildren(_catch.Block.ChildNodes());
                        CurrentTypeWriter.WriteLine(node, "}", true);
                    }
                    else
                    {
                        Visit(_catch.Block);
                    }
                    iDeclaration++;
                }
                int iNoDeclaration = 0;
                foreach (var _catch in catches.Where(e => e.Declaration == null))
                {
                    Visit(_catch.Block);
                    iNoDeclaration++;
                }
                if (iNoDeclaration == 0)
                {
                    CurrentTypeWriter.WriteLine(node, $"else", true);
                    CurrentTypeWriter.WriteLine(node, "{", true);
                    CurrentTypeWriter.WriteLine(node, "throw $e;", true);
                    CurrentTypeWriter.WriteLine(node, "}", true);
                }
                CurrentTypeWriter.WriteLine(node, "}", true);
                VisitChildren(node.ChildNodes().Except([node.Block, .. catches]));
            }
            else
            {
                VisitChildren(node.ChildNodes().Except([node.Block]));
            }
            //base.VisitTryStatement(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            IDisposable? dispose = null;
            CurrentTypeWriter.Write(node, "catch(", true);
            if (node.Declaration != null && !string.IsNullOrEmpty(node.Declaration.Identifier.ValueText))
            {
                var localField = _global.TryGetSymbol(node.Declaration, this/*, out _, out _*/);
                if (localField != null)
                    dispose = CurrentClosure.DefineIdentifierType(node.Declaration.Identifier.ValueText, CodeSymbol.From(localField));
                else
                    dispose = CurrentClosure.DefineIdentifierType(node.Declaration.Identifier.ValueText, CodeSymbol.From(node.Declaration.Type, SymbolKind.Local));
                CurrentTypeWriter.Write(node, node.Declaration.Identifier.ValueText);
            }
            else
                CurrentTypeWriter.Write(node, "$e");
            //Visit(node.Declaration);
            CurrentTypeWriter.WriteLine(node, ")");
            Visit(node.Block);
            dispose?.Dispose();
            //base.VisitCatchClause(node);
        }

        public override void VisitFinallyClause(FinallyClauseSyntax node)
        {
            CurrentTypeWriter.WriteLine(node, "finally", true);
            CurrentTypeWriter.WriteLine(node, "{", true);
            Visit(node.Block);
            CurrentTypeWriter.WriteLine(node, "}", true);
            //base.VisitFinallyClause(node);
        }

    }
}
