using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Threading;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    sealed class ImplicitConversionSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        public static bool NumberImplicitlyConvertsToLong(CSharpSyntaxNode node, SemanticModel semanticModel, TranslatorSyntaxVisitor visitor, Conversion? conversion = null)
        {
            conversion ??= semanticModel.GetConversion(node);
            if (conversion.Value.Exists &&
                        conversion.Value.IsImplicit &&
                        (conversion.Value.IsNumeric || conversion.Value.IsConstantExpression))
            {
                var fromOperation = semanticModel.GetOperation(node);
                var from = fromOperation?.Type;
                var toOperation = fromOperation?.Parent as IConversionOperation;
                var to = toOperation?.Type;
                if (from != null && to != null)
                {
                    bool skip = false;
                    ElementAccessExpressionSyntax? el = null;
                    //node.Parent.IsKind(SyntaxKind.ElementAccessExpression) ? (ElementAccessExpressionSyntax)node.Parent :
                    //    node.Parent.IsKind(SyntaxKind.Argument) && node.Parent.Parent.IsKind(SyntaxKind.BracketedArgumentList) && node.Parent.Parent.Parent.IsKind(SyntaxKind.ElementAccessExpression) ? (ElementAccessExpressionSyntax)node.Parent.Parent.Parent :
                    //    null;
                    if (/*el != null*/(el = node.FindClosestParent<ElementAccessExpressionSyntax>()) != null && el.ArgumentList.DescendantNodes().Contains(node))
                    {
                        var ex = visitor.Global.GetTypeSymbol(el.Expression, visitor);
                        if (ex.IsPointer(out _) || ex.IsArray(out _)) //dont convert pointer index to BigInt
                            skip = true;
                    }
                    if (!skip && from.IsIntegerNumericType() && to.IsLongNumericType())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool LongImplicitlyConvertsToNumber(CSharpSyntaxNode node, SemanticModel semanticModel, TranslatorSyntaxVisitor visitor, Conversion? conversion = null)
        {
            conversion ??= semanticModel.GetConversion(node);
            if (conversion.Value.Exists &&
                        conversion.Value.IsImplicit &&
                        (conversion.Value.IsNumeric || conversion.Value.IsConstantExpression))
            {
                var fromOperation = semanticModel.GetOperation(node);
                var from = fromOperation?.Type;
                var toOperation = fromOperation?.Parent as IConversionOperation;
                var to = toOperation?.Type;
                if (from != null && to != null)
                {
                    bool skip = false;
                    ElementAccessExpressionSyntax? el = null;
                    //node.Parent.IsKind(SyntaxKind.ElementAccessExpression) ? (ElementAccessExpressionSyntax)node.Parent :
                    //    node.Parent.IsKind(SyntaxKind.Argument) && node.Parent.Parent.IsKind(SyntaxKind.BracketedArgumentList) && node.Parent.Parent.Parent.IsKind(SyntaxKind.ElementAccessExpression) ? (ElementAccessExpressionSyntax)node.Parent.Parent.Parent :
                    //    null;
                    if (/*el != null*/(el = node.FindClosestParent<ElementAccessExpressionSyntax>()) != null && el.ArgumentList.DescendantNodes().Contains(node))
                    {
                        var ex = visitor.Global.GetTypeSymbol(el.Expression, visitor);
                        if (ex.IsPointer(out _) || ex.IsArray(out _)) //dont convert pointer index to BigInt
                            skip = true;
                    }
                    if (!skip && from.IsLongNumericType() && to.IsFloatingNumericType())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static ThreadLocal<List<SyntaxNode?>> _disabled = new(() => new List<SyntaxNode?>());
        public static IDisposable Disable(CSharpSyntaxNode? node)
        {
            if (node.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                SyntaxNode? snode = node;
                List<SyntaxNode> nodes = new();
                while (snode != null && snode.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    nodes.Add(snode);
                    snode = ((ParenthesizedExpressionSyntax)snode).Expression;
                }
                if (snode != null)
                    nodes.Add(snode);
                _disabled.Value.AddRange(nodes);
                return new DelegateDispose(() => nodes.ForEach(node => _disabled.Value.Remove(node)));
            }
            else
            {
                _disabled.Value.Add(node);
                return new DelegateDispose(() => _disabled.Value.Remove(node));
            }
        }
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (_disabled.Value.Contains(node))
                return false;
            if (_processing.Value.TryPeek(out var top) && top == node)
                return false;
            foreach (var sm in visitor.SemanticModels)
            {
                if (node.SyntaxTree == sm.SyntaxTree)
                {
                    var conversion = sm.GetConversion(node);
                    if (conversion.Exists &&
                        conversion.IsImplicit &&
                        conversion.IsUserDefined &&
                        conversion.MethodSymbol != null &&
                        visitor.Global.ShouldExportType(conversion.MethodSymbol.ContainingType, visitor))
                    {
                        _processing.Value.Push(node);
                        try
                        {
                            visitor.WriteMethodInvocation(node, conversion.MethodSymbol, null, [node], null, null, null, false);
                            return true;
                        }
                        finally
                        {
                            _processing.Value.Pop();
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
                            _processing.Value.Push(node);
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
                                _processing.Value.Pop();
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
                            _processing.Value.Push(node);
                            try
                            {
                                var sourceType = operation.Operand.Type!;
                                var spanType = operation.Type!;
                                var implicitConverter = spanType.GetMembers("op_Implicit", visitor.Global)
                                    .Cast<IMethodSymbol>()
                                    .FirstOrDefault(e => e.Parameters.Length == 1 && e.Parameters[0].Type.IsArray(out var t) && e.ReturnType.Equals(spanType, SymbolEqualityComparer.Default));
                                visitor.WriteMethodInvocation(node, implicitConverter, null, [new CodeNode(() => {
                                    visitor.Visit(node);
                                    visitor.CurrentTypeWriter.Write(node, ".");
                                    visitor.CurrentTypeWriter.Write(node, Constants.StructFieldsLayoutName);
                                })], null, null, null, false);
                                return true;
                            }
                            finally
                            {
                                _processing.Value.Pop();
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
                            bool IsCollectionExpressionTargetCandidateParameter(IParameterSymbol parameter, out ITypeSymbol? elementType)
                            {
                                if (parameter.Type.IsArray(out elementType))
                                    return true;
                                if (parameter.Type.IsEnumerable(out elementType))
                                    return true;
                                return false;
                            }
                            var targetType = operation.Type!;
                            ITypeSymbol? elementType = null;
                            var implicitConverter = targetType.GetMembers("op_Implicit", visitor.Global)
                                .Cast<IMethodSymbol>()
                                .FirstOrDefault(e => e.Parameters.Length == 1 && IsCollectionExpressionTargetCandidateParameter(e.Parameters[0], out elementType) && e.ReturnType.Equals(targetType, SymbolEqualityComparer.Default));
                            if (implicitConverter != null)
                            {
                                _processing.Value.Push(node);
                                try
                                {
                                    visitor.WriteMethodInvocation(node, implicitConverter, null, [new CodeNode(() =>
                                    {
                                        bool isBootCode = visitor.Global.HasAttribute(visitor.CurrentTypeSymbol, typeof(BootAttribute).FullName, visitor, false, out _);
                                        if (!isBootCode)
                                        {
                                            visitor.WriteCreateArray(node, elementType!, null, null, new CodeNode(() => {
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
                                            }));
                                        }
                                        else
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
                                        }
                                    })], null, null, null, false);
                                    return true;
                                }
                                finally
                                {
                                    _processing.Value.Pop();
                                }
                            }
                        }
                    }
                    else if ((node.IsKind(SyntaxKind.NumericLiteralExpression) || node.IsKind(SyntaxKind.CharacterLiteralExpression)) &&
                        node is LiteralExpressionSyntax lt &&
                        !lt.Token.Text.EndsWith("UL") &&
                        !lt.Token.Text.EndsWith("L") &&
                        conversion.Exists &&
                        conversion.IsImplicit /*&& conversion.IsConstantExpression*/)
                    {
                        var literalOperation = sm.GetOperation(node) as ILiteralOperation;
                        var convertOperation = literalOperation?.Parent as IConversionOperation;
                        var assignOperation = literalOperation?.Parent as ICompoundAssignmentOperation;
                        var convertType = convertOperation?.Type;
                        if (convertType?.TypeKind == TypeKind.Enum)
                        {
                            convertType = ((INamedTypeSymbol)convertType).EnumUnderlyingType;
                        }
                        if ((literalOperation != null &&
                            (SymbolEqualityComparer.Default.Equals(literalOperation.Type, visitor.Global.SystemUInt64) || SymbolEqualityComparer.Default.Equals(literalOperation.Type, visitor.Global.SystemInt64)))
                            ||
                            (convertOperation != null &&
                            (SymbolEqualityComparer.Default.Equals(convertType, visitor.Global.SystemUInt64) || SymbolEqualityComparer.Default.Equals(convertType, visitor.Global.SystemInt64)))
                            ||
                            (assignOperation != null &&
                            (SymbolEqualityComparer.Default.Equals(assignOperation.Type, visitor.Global.SystemUInt64) || SymbolEqualityComparer.Default.Equals(assignOperation.Type, visitor.Global.SystemInt64)))
                            )
                        {
                            _processing.Value.Push(node);
                            try
                            {
                                visitor.Visit(node);
                                visitor.CurrentTypeWriter.Write(node, "n");
                                return true;
                            }
                            finally { _processing.Value.Pop(); }
                        }
                    }
                    //else if (conversion.Exists &&
                    //    conversion.IsImplicit &&
                    //   (conversion.IsNumeric || conversion.IsConstantExpression))
                    //{
                    if (NumberImplicitlyConvertsToLong(node, sm, visitor, conversion))
                    {
                        _processing.Value.Push(node);
                        try
                        {
                            if (node.IsKind(SyntaxKind.NumericLiteralExpression))
                            {
                                visitor.Visit(node);
                                visitor.CurrentTypeWriter.Write(node, "n");
                            }
                            else
                            {
                                //If we are inside the class BigInt(could be user defined), make sure to generate window.BigInt
                                if (visitor.CurrentTypes.Any(t => t.Identifier.ValueText == "BigInt"))
                                {
                                    visitor.CurrentTypeWriter.Write(node, "window.");
                                }
                                visitor.CurrentTypeWriter.Write(node, "BigInt(");
                                visitor.Visit(node);
                                visitor.CurrentTypeWriter.Write(node, ")");
                            }
                            return true;
                        }
                        finally { _processing.Value.Pop(); }
                    }
                    else if (LongImplicitlyConvertsToNumber(node, sm, visitor, conversion))
                    {
                        _processing.Value.Push(node);
                        try
                        {
                            //If we are inside the class Number, make sure to generate window.Number
                            if (visitor.CurrentTypes.Any(t => t.Identifier.ValueText == "Number"))
                            {
                                visitor.CurrentTypeWriter.Write(node, "window.");
                            }
                            visitor.CurrentTypeWriter.Write(node, "Number(");
                            visitor.Visit(node);
                            visitor.CurrentTypeWriter.Write(node, ")");
                            return true;
                        }
                        finally { _processing.Value.Pop(); }
                    }
                    else if (conversion.Exists &&
                        conversion.IsImplicit &&
                        conversion.IsNumeric)
                    {
                        var fromOperation = sm.GetOperation(node);
                        var from = fromOperation?.Type;
                        var toOperation = fromOperation?.Parent as IConversionOperation;
                        var to = toOperation?.Type;
                        if (from != null && to != null)
                        {
                            if ((!from.IsNumberNumericType() && !from.IsLongNumericType()) || (!to.IsNumberNumericType() && !to.IsLongNumericType()))
                            {
                                _processing.Value.Push(node);
                                try
                                {
                                    if (visitor.TryInvokeMethodOperator(node, "op_Implicit", to, null, from, [node]))
                                        return true;
                                }
                                finally { _processing.Value.Pop(); }
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
