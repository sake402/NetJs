using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    //Taking the address of a local method. Bind the method to the available this
    internal class BindLocalFunctionIdentifierToAvailableThisSyntaxEmitter : SyntaxEmitter<IdentifierNameSyntax>
    {
        class ThisKeywordWalker : CSharpSyntaxWalker
        {
            public bool HasThisKeyword { get; private set; }

            public override void VisitThisExpression(ThisExpressionSyntax node)
            {
                HasThisKeyword = true;
                // Optimization: Stop traversing once found
            }
        }

        static bool ReferencesThis(IMethodSymbol localMethodSymbol)
        {
            // 2. Fetch the syntax reference location out of the source tree
            SyntaxReference? syntaxRef = localMethodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
            {
                return false; // Code exists out-of-file or in an external DLL assembly metadata binary
            }

            // 3. Extract the actual local function syntax node layout 
            var localFunctionSyntax = syntaxRef.GetSyntax() as LocalFunctionStatementSyntax;
            if (localFunctionSyntax == null)
            {
                return false;
            }
            var walker = new ThisKeywordWalker();
            walker.Visit(localFunctionSyntax);
            return walker.HasThisKeyword;
        }

        static bool LocalMethodReferencesThis(IMethodSymbol localMethodSymbol, Compilation compilation)
        {
            // 1. Ensure the symbol represents a local function declaration
            if (localMethodSymbol.MethodKind != MethodKind.LocalFunction)
            {
                return false;
            }

            // 2. Fetch the syntax reference location out of the source tree
            SyntaxReference? syntaxRef = localMethodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
            {
                return false; // Code exists out-of-file or in an external DLL assembly metadata binary
            }

            // 3. Extract the actual local function syntax node layout 
            var localFunctionSyntax = syntaxRef.GetSyntax() as LocalFunctionStatementSyntax;
            if (localFunctionSyntax == null)
            {
                return false;
            }

            // Check if there is even a body to inspect
            var bodyToCheck = localFunctionSyntax.Body ?? (SyntaxNode?)localFunctionSyntax.ExpressionBody?.Expression;
            if (bodyToCheck == null)
            {
                return false;
            }

            // 4. Generate a SemanticModel instance targeting the specific syntax location
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxRef.SyntaxTree);

            // 5. Run the Data Flow Analysis targeting the local method block boundary
            DataFlowAnalysis analysis = semanticModel.AnalyzeDataFlow(bodyToCheck);

            // 6. Inspect captured closures looking for the implicit instance parameter ('this')
            foreach (ISymbol capturedSymbol in analysis.Captured)
            {
                // When 'this' or an instance member is captured, Roslyn adds a Parameter symbol named "this"
                if (capturedSymbol.Kind == SymbolKind.Parameter && capturedSymbol.Name == "this")
                {
                    return true; // The instance context 'this' is captured!
                }
            }

            return false;
        }

        public override bool TryEmit(IdentifierNameSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Parent.IsKind(SyntaxKind.InvocationExpression)) //method identifier already invoked, no need to bind as the VisitInvocation will create a .call already
                return false;
            if (_processing.Value.Contains(node))
                return false;
            var identifierSymbol = visitor.Global.TryGetSymbol(node, visitor);
            if (identifierSymbol?.Kind == SymbolKind.Method && !identifierSymbol.IsStatic && ((IMethodSymbol)identifierSymbol).MethodKind == MethodKind.LocalFunction)
            {
                if (identifierSymbol.ContainingSymbol.IsStatic)//local method in as static method, no need to bind as this is not available
                    return false;
                if (!LocalMethodReferencesThis((IMethodSymbol)identifierSymbol, visitor.Global.Compilation)) //no this reference by the local method, no need to bind
                    return false;
                _processing.Value.Push(node);
                try
                {
                    visitor.Visit(node);
                    visitor.CurrentTypeWriter.Write(node, ".bind(this)");
                    return true;
                }
                finally { _processing.Value.Pop(); }
            }
            return false;
        }
    }
}
