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
        public void WriteCreateIndexFromStart(CSharpSyntaxNode node, CodeNode numericIndex)
        {
            //var index = (ITypeSymbol)_global.GetSymbol("System.Index", this/*, out _, out _*/);
            var fromEnd = _global.SystemIndex.GetMembers("FromStart").Cast<IMethodSymbol>().FirstOrDefault();
            WriteMethodInvocation(node, fromEnd, null, [numericIndex], null, null, null, false);
        }

        public void WriteCreateIndexFromEnd(CSharpSyntaxNode node, CodeNode numericIndex)
        {
            //var index = (ITypeSymbol)_global.GetSymbol("System.Index", this/*, out _, out _*/);
            var fromEnd = _global.SystemIndex.GetMembers("FromEnd").Cast<IMethodSymbol>().FirstOrDefault();
            WriteMethodInvocation(node, fromEnd, null, [numericIndex], null, null, null, false);
        }

        public void WriteCreateRange(CSharpSyntaxNode node, CodeNode? startIndex, CodeNode? endIndex)
        {
            //var index = (ITypeSymbol)_global.GetSymbol("System.Index", this/*, out _, out _*/);
            //var range = (INamedTypeSymbol)_global.GetSymbol("System.Range", this/*, out _, out _*/);
            if (startIndex == null && endIndex == null)
            {
                WriteMemberAccess(node, null, _global.SystemRange, "All", null);
            }
            else if (startIndex != null && endIndex != null)
            {
                var startEndConstructor = _global.SystemRange.GetMembers(".ctor")
                    .Cast<IMethodSymbol>()
                    .Single(e => e.Parameters.Count() == 2 && e.Parameters.All(p => p.Type.Equals(_global.SystemIndex, SymbolEqualityComparer.Default)));
                WriteConstructorCall(node, _global.SystemRange, startEndConstructor, null, [startIndex, endIndex]);
            }
            else if (startIndex != null)
            {
                var startMethod = _global.SystemRange.GetMembers("StartAt").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.All(p => p.Type.Equals(_global.SystemIndex, SymbolEqualityComparer.Default)));
                WriteMethodInvocation(node, startMethod, null, [startIndex], null, null, null, false);
            }
            else if (endIndex != null)
            {
                var endMethod = _global.SystemRange.GetMembers("EndAt").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.All(p => p.Type.Equals(_global.SystemIndex, SymbolEqualityComparer.Default)));
                WriteMethodInvocation(node, endMethod, null, [endIndex], null, null, null, false);
            }
        }

        
        public override void VisitRangeExpression(RangeExpressionSyntax node)
        {
            var left = node.LeftOperand;
            while (left.IsKind(SyntaxKind.ParenthesizedExpression) && left is ParenthesizedExpressionSyntax pl)
            {
                left = pl.Expression;
            }
            var right = node.RightOperand;
            while (right.IsKind(SyntaxKind.ParenthesizedExpression) && right is ParenthesizedExpressionSyntax pr)
            {
                right = pr.Expression;
            }
            WriteCreateRange(node, left, right);
            //base.VisitRangeExpression(node);
        }
    }
}
