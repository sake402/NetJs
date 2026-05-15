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
        public void WriteIndexOperator(CSharpSyntaxNode node, ExpressionSyntax operand)
        {
            var index = (ITypeSymbol)_global.GetSymbol("System.Index", this/*, out _, out _*/);
            var fromEnd = index.GetMembers("FromEnd").Cast<IMethodSymbol>().FirstOrDefault();
            WriteMethodInvocation(node, fromEnd, null, [operand], null, null, null, false);
        }

        public void WriteRangeOperator(CSharpSyntaxNode node, ExpressionSyntax? leftOperand, ExpressionSyntax? rightOperand)
        {
            var index = (ITypeSymbol)_global.GetSymbol("System.Index", this/*, out _, out _*/);
            var range = (INamedTypeSymbol)_global.GetSymbol("System.Range", this/*, out _, out _*/);
            if (leftOperand == null && rightOperand == null)
            {
                WriteMemberAccess(node, null, range, "All", null);
            }
            else if (leftOperand != null && rightOperand != null)
            {
                var startEndConstructor = range.GetMembers(".ctor").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 2 && e.Parameters.All(p => p.Type.Equals(index, SymbolEqualityComparer.Default)));
                WriteConstructorCall(node, range, startEndConstructor, null, [leftOperand, rightOperand]);
            }
            else if (leftOperand != null)
            {
                var startMethod = range.GetMembers("StartAt").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.All(p => p.Type.Equals(index, SymbolEqualityComparer.Default)));
                WriteMethodInvocation(node, startMethod, null, [leftOperand], null, null, null, false);
            }
            else if (rightOperand != null)
            {
                var endMethod = range.GetMembers("EndAt").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 1 && e.Parameters.All(p => p.Type.Equals(index, SymbolEqualityComparer.Default)));
                WriteMethodInvocation(node, endMethod, null, [rightOperand], null, null, null, false);
            }
        }

        
        public override void VisitRangeExpression(RangeExpressionSyntax node)
        {
            WriteRangeOperator(node, node.LeftOperand, node.RightOperand);
            //base.VisitRangeExpression(node);
        }
    }
}
