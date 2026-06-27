using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        Stack<CodeBlockClosure> closures = new Stack<CodeBlockClosure>();
        public IEnumerable<CodeBlockClosure> Closures => closures;
        public CodeBlockClosure CurrentClosure
        {
            get
            {
                closures.TryPeek(out var closure);
                return closure;
            }
        }

        public ISymbol OpenClosure(CSharpSyntaxNode node)
        {
            var symbol = _global.TryGetSymbol(node, this/*, out _, out _*/);
            closures.Push(new CodeBlockClosure(_global, this, node, symbol, CurrentClosure));
            return symbol!;
        }

        public void CloseClosure(CSharpSyntaxNode node)
        {
            var closure = closures.Pop();
            Debug.Assert(node == closure.Syntax);
            closure.Dispose();
        }

        CodeBlockClosure? TryGetClosureOf(CSharpSyntaxNode node)
        {
            foreach (var c in closures)
                if (c.Syntax == node)
                    return c;
            return null;
        }

        CodeBlockClosure GetClosureOf(CSharpSyntaxNode node)
        {
            foreach (var c in closures)
                if (c.Syntax == node)
                    return c;
            throw new InvalidOperationException($"Cannot find closure of node requested");
        }

        void DefineParametersInClosure(IEnumerable<ParameterSyntax> parameters, ISymbol? source)
        {
            int ix = 0;
            foreach (var parameter in parameters)
            {
                if (source is IMethodSymbol method)
                {
                    CurrentClosure.DefineIdentifierType(parameter.Identifier.ValueText, CodeSymbol.From(method.Parameters.ElementAt(ix)));
                }
                else
                {
                    if (parameter.Type != null)
                        CurrentClosure.DefineIdentifierType(parameter.Identifier.ValueText, CodeSymbol.From(parameter.Type, SymbolKind.Parameter));
                }
                ix++;
            }
        }

        internal CodeSymbol GetIdentifierTypeInScope(string identifierName)
        {
            foreach (var closure in closures)
            {
                var type = closure.GetIdentifierType(identifierName);
                if (type.TypeSyntaxOrSymbol != null)
                    return type;
            }
            return default;
        }
    }
}
