using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    internal class StructCloneOnAssignmentSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (_processing.Value!.TryPeek(out var top) && top == node)
                return false;
            if (node.Parent.IsKind(SyntaxKind.Argument) ||
                node.Parent.IsKind(SyntaxKind.ReturnStatement) ||
                node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression) ||
                node.Parent.IsKind(SyntaxKind.EqualsValueClause))
            {
                CSharpSyntaxNode expression;
                if (node.Parent.IsKind(SyntaxKind.Argument))
                {
                    var parameter = visitor.GetParameterSymbol((ArgumentSyntax)node.Parent);
                    if (parameter != null && parameter.ContainingSymbol.GetTemplateAttribute(visitor.Global, visitor) != null)
                        return false;
                    if (parameter != null && parameter.RefKind != RefKind.None)
                        return false;
                    if (!((ArgumentSyntax)node.Parent).RefKindKeyword.IsKind(SyntaxKind.None))
                        return false;
                    expression = node;
                }
                else if (node.Parent.IsKind(SyntaxKind.ReturnStatement))
                {
                    expression = node;
                    var sym = visitor.Global.GetSymbol(expression, visitor);
                    if (sym.Kind == SymbolKind.Local) //not point in cloning  a local variable
                    {
                        return false;
                    }
                }
                else if (node.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    var ass = (AssignmentExpressionSyntax)node.Parent;
                    if (ass.Left == node)
                        return false;
                    if (ass.Right.IsKind(SyntaxKind.ThisExpression)) //this = value => Handled by ThisStructAssignmentSyntaxEmitter, no point in cloning first what we are cloning this
                        return false;
                    //if we are desctruturing
                    if (ass.Left.IsKind(SyntaxKind.DeclarationExpression) && ass.Left is DeclarationExpressionSyntax decl && decl.Designation.IsKind(SyntaxKind.ParenthesizedVariableDesignation))
                    {
                        return false;
                    }
                    expression = node;
                }
                else
                {
                    expression = node;
                }
                if (expression.IsKind(SyntaxKind.IdentifierName) || expression.IsKind(SyntaxKind.ThisExpression))
                {
                    var symbol = visitor.Global.TryGetSymbol(expression, visitor);
                    var type = symbol != null ? visitor.Global.TryGetTypeSymbol(symbol) : null;
                    if (symbol != null &&
                        type?.Kind == SymbolKind.NamedType &&
                        ((type.IsValueType && !SymbolEqualityComparer.Default.Equals(type, visitor.Global.SystemVoid)) || (type is ITypeParameterSymbol tp && tp.HasValueTypeConstraint)) &&
                        !type.IsJsPrimitive())
                    {
                        if (ImplicitConversionSyntaxEmitter.IsCandidate(expression, visitor))
                        {
                            return false;
                        }
                        //We dont want to clone too much else we have GC issues.
                        //ReadOnly struct like ReadOnlySpan cannot be modified anyway, so we skip cloning those
                        //A struct with readonly fields and init only property is skipped too
                        if (type.IsStructurallyImmutable())
                            return false;
                        var refKind = symbol.GetRefKind();
                        if (refKind == RefKind.None ||
                            (refKind != RefKind.None && RefTypeDereferenceOnAccessSyntaxEmitter.IsRefTypeToDereference(expression, visitor, out _)) ||
                            expression.IsKind(SyntaxKind.ThisExpression))
                        {
                            _processing.Value.Push(node);
                            try
                            {
                                //if (node.Parent.IsKind(SyntaxKind.EqualsValueClause))
                                //{
                                //    visitor.CurrentTypeWriter.Write(node, " = ");
                                //}
                                visitor.CurrentTypeWriter.Write(node, visitor.Global.GlobalName);
                                visitor.CurrentTypeWriter.Write(node, ".");
                                visitor.CurrentTypeWriter.Write(node, Constants.Clone);
                                visitor.CurrentTypeWriter.Write(node, "(");
                                //if (node.IsKind(SyntaxKind.EqualsValueClause))
                                //{
                                //    visitor.Visit(expression);
                                //}
                                //else
                                visitor.Visit(node);
                                visitor.CurrentTypeWriter.Write(node, ")");
                                //visitor.Visit(node);
                                //if (type.IsNullable(out _))
                                //    visitor.CurrentTypeWriter.Write(node, "?");
                                //visitor.CurrentTypeWriter.Write(node, ".");
                                //visitor.CurrentTypeWriter.Write(node, Constants.Clone);
                                //visitor.CurrentTypeWriter.Write(node, "(");
                                //visitor.CurrentTypeWriter.Write(node, ")");
                                //if (type.IsNullable(out _))
                                //    visitor.CurrentTypeWriter.Write(node, " ?? null");
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
