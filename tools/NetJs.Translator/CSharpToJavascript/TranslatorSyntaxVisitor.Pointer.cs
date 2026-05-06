using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public void WritePointerAdvance(CSharpSyntaxNode node, ExpressionSyntax pointer, CodeNode advance, bool subtract = false)
        {
            Visit(pointer);
            CurrentTypeWriter.Write(node, ".");
            var refOrPointer = (ITypeSymbol)_global.GetTypeSymbol("System.Pointer<>", this);
            var add = (IMethodSymbol)refOrPointer.GetMembers("Add", _global).Single();
            WriteMemberName(node, refOrPointer, add);
            CurrentTypeWriter.Write(node, $"(");
            if (subtract)
            {
                CurrentTypeWriter.Write(node, $"-");
            }
            VisitNode(advance);
            CurrentTypeWriter.Write(node, $")");
        }

        public void WritePointerSelfAdvance(CSharpSyntaxNode node, ExpressionSyntax pointer, CodeNode advance, bool subtract = false)
        {
            //Writer.Write(node, "", true);
            Visit(pointer);
            CurrentTypeWriter.Write(node, " = ");
            WritePointerAdvance(node, pointer, advance, subtract: subtract);
        }

        public void WritePointerSubtration(CSharpSyntaxNode node, ExpressionSyntax left, ExpressionSyntax right)
        {
            //var returnType = _global.GetTypeSymbol(node, this).GetTypeSymbol();
            ////Pointer sub typically return long, If the parent is a cast though(more likely to int), ignore this cast to long, from
            //bool ignoreCast = node.Parent.IsKind(SyntaxKind.ParenthesizedExpression) && node.Parent.Parent.IsKind(SyntaxKind.CastExpression);
            //if (!ignoreCast && SymbolEqualityComparer.Default.Equals(returnType, _global.SystemInt64))
            //{
            //    CurrentTypeWriter.Write(node, _global.GlobalName);
            //    CurrentTypeWriter.Write(node, ".");
            //    CurrentTypeWriter.Write(node, Constants.CastName);
            //    CurrentTypeWriter.Write(node, "(");
            //}
            Visit(left);
            CurrentTypeWriter.Write(node, ".");
            var refOrPointer = (ITypeSymbol)_global.GetTypeSymbol("System.Pointer<>", this);
            var subtract = (IMethodSymbol)refOrPointer.GetMembers("Subtract", _global).Single();
            WriteMemberName(node, refOrPointer, subtract);
            CurrentTypeWriter.Write(node, $"(");
            VisitNode(right);
            CurrentTypeWriter.Write(node, $")");
            //if (!ignoreCast && SymbolEqualityComparer.Default.Equals(returnType, _global.SystemInt64))
            //{
            //    var metadata = _global.GetRequiredMetadata(_global.SystemInt64);
            //    CurrentTypeWriter.Write(node, ", ");
            //    CurrentTypeWriter.Write(node, metadata.InvocationName ?? _global.SystemInt64.ToString());
            //    CurrentTypeWriter.Write(node, ")");
            //}
        }

        public override void VisitPointerType(PointerTypeSyntax node)
        {
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.TypePointer}(");
            Visit(node.ElementType);
            CurrentTypeWriter.Write(node, ")");
            //base.VisitPointerType(node);
        }
    }
}
