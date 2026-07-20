using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    internal class StructCloneOnAssignmentSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (_processing.Value.TryPeek(out var top) && top == node)
                return false;
            if (node.IsKind(SyntaxKind.Argument) || node.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                CSharpSyntaxNode expression;
                if (node.IsKind(SyntaxKind.Argument))
                {
                    var parameter = visitor.GetParameterSymbol((ArgumentSyntax)node);
                    if (parameter != null && parameter.RefKind != RefKind.None)
                        return false;
                    if (!((ArgumentSyntax)node).RefKindKeyword.IsKind(SyntaxKind.None))
                        return false;
                    expression = ((ArgumentSyntax)node).Expression;
                }
                else
                {
                    var ass = (AssignmentExpressionSyntax)node;
                    //if we are desctruturing
                    if (ass.Left.IsKind(SyntaxKind.DeclarationExpression) && ass.Left is DeclarationExpressionSyntax decl && decl.Designation.IsKind(SyntaxKind.ParenthesizedVariableDesignation))
                    {
                        return false;
                    }
                    expression = ass.Right;
                }
                if (expression.IsKind(SyntaxKind.IdentifierName))
                {
                    var symbol = visitor.Global.TryGetSymbol(expression, visitor);
                    var type = symbol != null ? visitor.Global.TryGetTypeSymbol(symbol) : null;
                    if (symbol != null &&
                        type?.Kind == SymbolKind.NamedType &&
                        ((type.IsValueType && !SymbolEqualityComparer.Default.Equals(type, visitor.Global.SystemVoid)) || (type is ITypeParameterSymbol tp && tp.HasValueTypeConstraint)) &&
                        !type.IsJsPrimitive())
                    {
                        //We dont want to clone too much else we have GC issues.
                        //ReadOnly struct like ReadOnlySpan cannot be modified anyway, so we skip cloning those
                        //A struct with readonly fields and init only property is skipped too
                        if (type.IsStructurallyImmutable())
                            return false;
                        var refKind = symbol.GetRefKind();
                        if (refKind == RefKind.None)
                        {
                            _processing.Value.Push(node);
                            try
                            {
                                visitor.Visit(node);
                                if (type.IsNullable(out _))
                                    visitor.CurrentTypeWriter.Write(node, "?");
                                visitor.CurrentTypeWriter.Write(node, ".");
                                visitor.CurrentTypeWriter.Write(node, Constants.Clone);
                                visitor.CurrentTypeWriter.Write(node, "(");
                                visitor.CurrentTypeWriter.Write(node, ")");
                            }
                            finally { _processing.Value.Pop(); }
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
