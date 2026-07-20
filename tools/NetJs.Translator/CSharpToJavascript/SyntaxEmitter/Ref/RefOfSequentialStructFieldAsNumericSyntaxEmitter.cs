using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Ref
{
    /// <summary>
    /// ref of field of a struct should reference the backing field array. eg "ref Unsafe.AsRef<T, int>(ref struct._field)". Unsafe.AsRef is a NOP in this port though
    /// </summary>
    sealed internal class RefOfSequentialStructFieldAsNumericSyntaxEmitter : SyntaxEmitter<RefExpressionSyntax>
    {
        public override bool TryEmit(RefExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            ExpressionSyntax? targetSource = null;
            if (node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                targetSource = ((MemberAccessExpressionSyntax)node.Expression).Expression;
            }
            else if (node.Expression.IsKind(SyntaxKind.IdentifierName))
            {
                targetSource = SyntaxFactory.ThisExpression();
            }
            if (targetSource != null)
            {
                var targetSymbol = visitor.Global.TryGetSymbol(node.Expression, visitor);
                if (targetSymbol != null &&
                    targetSymbol.Kind == SymbolKind.Field &&
                    targetSymbol.GetRefKind() == RefKind.None &&
                    targetSymbol.ContainingType.IsValueType/* && visitor.Global.IsPureStructType(targetSymbol.ContainingType)*/)
                {
                    var field = (IFieldSymbol)targetSymbol;
                    if (visitor.IsFieldStructLayout(null, field, out int fieldOffset, out int fieldSize))
                    {
                        var methodInvoke = node.FindClosestParent<InvocationExpressionSyntax>();
                        if (methodInvoke != null)
                        {
                            var methodSymbol = (IMethodSymbol)visitor.Global.GetSymbol(methodInvoke, visitor);
                            if (methodSymbol.ContainingType.Name == "Unsafe" && methodSymbol.Name == "AsRef" && methodSymbol.TypeArguments[0].IsNumericType())
                            {
                                if (visitor.Global.IsPureStructType(field.ContainingType))
                                {
                                    var getFieldsAsPointerMethod = (IMethodSymbol)visitor.Global.SystemObject.GetMembers("GetFieldRefOrPointer").Single();
                                    getFieldsAsPointerMethod = getFieldsAsPointerMethod.Construct(methodSymbol.TypeArguments[0]);
                                    visitor.WriteMethodInvocation(node, getFieldsAsPointerMethod, null, [
                                        new CodeNode(() => visitor.CurrentTypeWriter.Write(node, fieldOffset.ToString())),
                                        new CodeNode(() => visitor.CurrentTypeWriter.Write(node, "false")) //create reference type
                                    ], targetSource, null);
                                }
                                else
                                    visitor.WriteCreateObjectRefOrPointer(node, methodSymbol.TypeArguments[0], targetSource, byteOffset: new CodeNode(() => visitor.CurrentTypeWriter.Write(node, fieldOffset.ToString())));
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// ref of field of a struct should reference the backing field array. eg "ref Unsafe.AsRef<T, int>(in struct._field)"
    /// </summary>
    sealed internal class InOfSequentialStructFieldAsNumericSyntaxEmitter : SyntaxEmitter<ArgumentSyntax>
    {
        public override bool TryEmit(ArgumentSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.RefKindKeyword.IsKind(SyntaxKind.InKeyword))
            {
                ExpressionSyntax? targetSource = null;
                if (node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    targetSource = ((MemberAccessExpressionSyntax)node.Expression).Expression;
                }
                else if (node.Expression.IsKind(SyntaxKind.IdentifierName))
                {
                    targetSource = SyntaxFactory.ThisExpression();
                }
                if (targetSource != null)
                {
                    var targetSymbol = visitor.Global.TryGetSymbol(node.Expression, visitor);
                    if (targetSymbol != null &&
                        targetSymbol.Kind == SymbolKind.Field &&
                        targetSymbol.GetRefKind() == RefKind.None &&
                        targetSymbol.ContainingType.IsValueType/* && visitor.Global.IsPureStructType(targetSymbol.ContainingType)*/)
                    {
                        var field = (IFieldSymbol)targetSymbol;
                        if (visitor.IsFieldStructLayout(null, field, out int fieldOffset, out int fieldSIze))
                        {
                            var methodInvoke = node.FindClosestParent<InvocationExpressionSyntax>();
                            if (methodInvoke != null)
                            {
                                var methodSymbol = (IMethodSymbol)visitor.Global.GetSymbol(methodInvoke, visitor);
                                if (methodSymbol.ContainingType.Name == "Unsafe" && methodSymbol.Name == "AsRef" && methodSymbol.TypeArguments[0].IsNumericType())
                                {
                                    ////TODO: maybe we can just generate this.dataView.set(...)
                                    //if (visitor.Global.IsPureStructType(field.ContainingType))
                                    //{

                                    //}
                                    //else
                                    if (visitor.Global.IsPureStructType(field.ContainingType))
                                    {
                                        var getFieldsAsPointerMethod = (IMethodSymbol)visitor.Global.SystemObject.GetMembers("GetFieldRefOrPointer").Single();
                                        getFieldsAsPointerMethod = getFieldsAsPointerMethod.Construct(methodSymbol.TypeArguments[0]);
                                        visitor.WriteMethodInvocation(node, getFieldsAsPointerMethod, null, [
                                            new CodeNode(() => visitor.CurrentTypeWriter.Write(node, fieldOffset.ToString())), 
                                            new CodeNode(() => visitor.CurrentTypeWriter.Write(node, "false")) //create reference type
                                        ], targetSource, null);
                                    }
                                    else
                                        visitor.WriteCreateObjectRefOrPointer(node, methodSymbol.TypeArguments[0], targetSource, byteOffset: new CodeNode(() => visitor.CurrentTypeWriter.Write(node, fieldOffset.ToString())));
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
