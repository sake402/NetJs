using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    sealed class ImplicitConversionSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        Stack<CSharpSyntaxNode> _processing = new Stack<CSharpSyntaxNode>();
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (_processing.TryPeek(out var top) && top == node)
                return false;
            foreach (var sm in visitor.SemanticModels)
            {
                if (node.SyntaxTree == sm.SyntaxTree)
                {
                    if (node.ToFullString().Contains("span = stackAllocatedMatches"))
                    {

                    }
                    var conversion = sm.GetConversion(node);
                    if (conversion.Exists &&
                        conversion.IsImplicit &&
                        conversion.IsUserDefined &&
                        conversion.MethodSymbol != null &&
                        visitor.Global.ShouldExportType(conversion.MethodSymbol.ContainingType, visitor))
                    {
                        _processing.Push(node);
                        try
                        {
                            visitor.WriteMethodInvocation(node, conversion.MethodSymbol, null, [node], null, null, null, false);
                            return true;
                        }
                        finally
                        {
                            _processing.Pop();
                        }
                        //visitor.TryInvokeMethodOperator(node, "op_Implicit", (ITypeSymbol?)lhsType, null, [rhsAsExpression]));
                    }
                    else if (conversion.Exists &&
                        conversion.IsImplicit &&
                        conversion.IsSpan)
                    {
                        var operation = sm.GetOperation(node)?.Parent as IConversionOperation;
                        if (operation != null)
                        {
                            _processing.Push(node);
                            try
                            {
                                var sourceType = operation.Operand.Type!;
                                var spanType = operation.Type!;
                                var implicitConverter = spanType.GetMembers("op_Implicit", visitor.Global)
                                    .Cast<IMethodSymbol>()
                                    .FirstOrDefault(e => e.Parameters.Length == 1 && sourceType.CanConvertTo(e.Parameters[0].Type, visitor.Global, null, out _) > 0 && e.ReturnType.Equals(spanType, SymbolEqualityComparer.Default))
                                    ??
                                    sourceType.GetMembers("op_Implicit", visitor.Global)
                                    .Cast<IMethodSymbol>()
                                    .First(e => e.Parameters.Length == 1 && sourceType.CanConvertTo(e.Parameters[0].Type, visitor.Global, null, out _) > 0 && spanType.Equals(e.ReturnType, SymbolEqualityComparer.Default))
                                    ;
                                visitor.WriteMethodInvocation(node, implicitConverter, null, [node], null, null, null, false);
                                return true;
                            }
                            finally
                            {
                                _processing.Pop();
                            }
                        }
                    }
                    else if (conversion.Exists &&
                        conversion.IsImplicit &&
                        conversion.IsInlineArray)
                    {
                        var operation = sm.GetOperation(node)?.Parent as IConversionOperation;
                        if (operation != null)
                        {
                            _processing.Push(node);
                            try
                            {
                                var sourceType = operation.Operand.Type!;
                                var spanType = operation.Type!;
                                var implicitConverter = spanType.GetMembers("op_Implicit", visitor.Global)
                                    .Cast<IMethodSymbol>()
                                    .FirstOrDefault(e => e.Parameters.Length == 1 && e.Parameters[0].Type.IsArray(out var t) && e.ReturnType.Equals(spanType, SymbolEqualityComparer.Default));
                                visitor.WriteMethodInvocation(node, implicitConverter, null, [node], null, null, null, false);
                                return true;
                            }
                            finally
                            {
                                _processing.Pop();
                            }
                        }
                    }
                    else if (conversion.Exists &&
                        conversion.IsImplicit &&
                        conversion.IsCollectionExpression)
                    {
                        var operation = sm.GetOperation(node)?.Parent as IConversionOperation;
                        if (operation != null)
                        {
                            bool IsCollectionExpressionTargetCandidateParameter(IParameterSymbol parameter)
                            {
                                if (parameter.Type.IsArray(out var ta))
                                    return true;
                                if (parameter.Type.IsEnumerable(out var te))
                                    return true;
                                return false;
                            }
                            var targetType = operation.Type!;
                            var implicitConverter = targetType.GetMembers("op_Implicit", visitor.Global)
                                .Cast<IMethodSymbol>()
                                .FirstOrDefault(e => e.Parameters.Length == 1 && IsCollectionExpressionTargetCandidateParameter(e.Parameters[0]) && e.ReturnType.Equals(targetType, SymbolEqualityComparer.Default));
                            if (implicitConverter != null)
                            {
                                _processing.Push(node);
                                try
                                {
                                    visitor.WriteMethodInvocation(node, implicitConverter, null, [new CodeNode(() =>
                                    {
                                        visitor.CurrentTypeWriter.Write(node, "[");
                                        int ix = 0;
                                        foreach(var e in ((CollectionExpressionSyntax)node).Elements)
                                        {
                                            if (ix > 0)
                                                visitor.CurrentTypeWriter.Write(node, ", ");
                                            visitor.Visit(e);
                                            ix++;
                                        }
                                        visitor.CurrentTypeWriter.Write(node, "]");
                                    })], null, null, null, false);
                                    return true;
                                }
                                finally
                                {
                                    _processing.Pop();
                                }
                            }
                        }
                    }
                    else if (node.IsKind(SyntaxKind.NumericLiteralExpression) &&
                        node is LiteralExpressionSyntax lt &&
                        !lt.Token.Text.EndsWith("UL") &&
                        !lt.Token.Text.EndsWith("L") &&
                        conversion.Exists &&
                        conversion.IsImplicit /*&& conversion.IsConstantExpression*/)
                    {
                        var literalOperation = sm.GetOperation(node) as ILiteralOperation;
                        var convertOperation = sm.GetOperation(node)?.Parent as IConversionOperation;
                        if ((literalOperation != null &&
                            (SymbolEqualityComparer.Default.Equals(literalOperation.Type, visitor.Global.SystemUInt64) || SymbolEqualityComparer.Default.Equals(literalOperation.Type, visitor.Global.SystemInt64)))
                            ||
                            (convertOperation != null &&
                            (SymbolEqualityComparer.Default.Equals(convertOperation.Type, visitor.Global.SystemUInt64) || SymbolEqualityComparer.Default.Equals(convertOperation.Type, visitor.Global.SystemInt64)))
                            )
                        {
                            _processing.Push(node);
                            try
                            {
                                visitor.Visit(node);
                                visitor.CurrentTypeWriter.Write(node, "n");
                                return true;
                            }
                            finally { _processing.Pop(); }
                        }
                    }
                    else if (conversion.Exists &&
                        conversion.IsImplicit &&
                        conversion.IsNumeric)
                    {
                        var from = sm.GetOperation(node)?.Type;
                        var to = (sm.GetOperation(node)?.Parent as IConversionOperation)?.Type;
                        if (from != null && to != null)
                        {
                            bool skip = false;
                            ElementAccessExpressionSyntax? el;
                            if ((el = node.FindClosestParent<ElementAccessExpressionSyntax>()) != null)
                            {
                                var ex = visitor.Global.GetTypeSymbol(el.Expression, visitor).GetTypeSymbol();
                                if (ex.IsPointer(out _) || ex.IsArray(out _)) //dont convert pointer index to BigInt
                                    skip = true;
                            }
                            if (!skip && from.IsIntegerNumericType() && to.IsLongNumericType())
                            {
                                _processing.Push(node);
                                try
                                {
                                    if (node.IsKind(SyntaxKind.NumericLiteralExpression))
                                    {
                                        visitor.Visit(node);
                                        visitor.CurrentTypeWriter.Write(node, "n");
                                    }
                                    else
                                    {
                                        visitor.CurrentTypeWriter.Write(node, "BigInt(");
                                        visitor.Visit(node);
                                        visitor.CurrentTypeWriter.Write(node, ")");
                                    }
                                    return true;
                                }
                                finally { _processing.Pop(); }
                            }
                        }
                    }

                }
            }
            return false;
        }
    }

    //sealed class RefAssignmentSyntaxEmitter : SyntaxEmitter<ExpressionSyntax>
    //{
    //    Stack<ExpressionSyntax> _processing = new Stack<ExpressionSyntax>();
    //    public override bool TryEmit(ExpressionSyntax node, TranslatorSyntaxVisitor visitor)
    //    {
    //        if (_processing.TryPeek(out var top) && top == node)
    //            return false;
    //        foreach (var sm in visitor.SemanticModels)
    //        {
    //            if (node.SyntaxTree == sm.SyntaxTree)
    //            {
    //                var conversion = sm.Get(node);
    //                if (conversion.Exists && conversion.IsImplicit && conversion.IsUserDefined && conversion.MethodSymbol != null)
    //                {
    //                    _processing.Push(node);
    //                    try
    //                    {
    //                        visitor.WriteMethodInvocation(node, conversion.MethodSymbol, null, [node], null, null, null, false);
    //                    }
    //                    finally
    //                    {
    //                        _processing.Pop();
    //                    }
    //                    //visitor.TryInvokeMethodOperator(node, "op_Implicit", (ITypeSymbol?)lhsType, null, [rhsAsExpression]));
    //                    return true;
    //                }
    //            }
    //        }
    //        return false;
    //    }
    //}
}
