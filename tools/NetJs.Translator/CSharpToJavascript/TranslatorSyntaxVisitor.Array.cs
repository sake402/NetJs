using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        /// <summary>
        /// Calls Array.CreateInstance(Type elementType, int[] lengths, int[] lowerBounds)
        /// </summary>
        /// <param name="node"></param>
        /// <param name="elementType"></param>
        /// <param name="lengths"></param>
        /// <param name="bounds"></param>
        public void WriteCreateArray(CSharpSyntaxNode node, TypeSyntax elementType, CodeNode? lengths, CodeNode? bounds, CodeNode? values)
        {
            //if (values != null)
            //{
            WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateArray", arguments: [new CodeNode(() => {
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.TypeOf}(");
                    Visit(elementType);
                    CurrentTypeWriter.Write(node, $")");
                }), values?? new CodeNode(()=>{
                    CurrentTypeWriter.Write(node, $"null");
                }), lengths??new CodeNode(()=>{
                    CurrentTypeWriter.Write(node, $"null");
                }), bounds??new CodeNode(()=>{
                    CurrentTypeWriter.Write(node, $"null");
                })]);
            //}
            //else
            //{
            //    var array = (ITypeSymbol)_global.GetTypeSymbol("System.Array", this);
            //    var sType = (ITypeSymbol)_global.GetTypeSymbol("System.Type", this);
            //    var sint = (ITypeSymbol)_global.GetTypeSymbol("System.Int32", this);
            //    if (bounds == null)
            //    {
            //        bool FilterMethod(IMethodSymbol e)
            //        {
            //            return e.Parameters.Count() == 2 &&
            //                e.Parameters[0].Type.Equals(sType, SymbolEqualityComparer.Default) &&
            //                e.Parameters[1].Type.IsArray(out var lType) &&
            //                lType.Equals(sint, SymbolEqualityComparer.Default);
            //        }
            //        WriteMethodInvocation(node, "System.Array.CreateInstance", methodFilter: FilterMethod, arguments: [new CodeNode(() =>
            //        {
            //            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.TypeOf}(");
            //            Visit(elementType);
            //            CurrentTypeWriter.Write(node, $")");
            //        }), lengths]);
            //        //var createInstanceWithLength = (IMethodSymbol)array.GetMembers("CreateInstance").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 2 && e.Parameters[0].Type.Equals(sType, SymbolEqualityComparer.Default) && e.Parameters[1].Type.IsArray(out var lType) && lType.Equals(sint, SymbolEqualityComparer.Default));
            //        //WriteMethodInvocation(node, createInstanceWithLength, null, null, [elementType, ranks], null, array, false);
            //    }
            //    else
            //    {
            //        bool FilterMethod(IMethodSymbol e)
            //        {
            //            return e.Parameters.Count() == 3 &&
            //                e.Parameters[0].Type.Equals(sType, SymbolEqualityComparer.Default) &&
            //                e.Parameters[1].Type.IsArray(out var lType) &&
            //                lType.Equals(sint, SymbolEqualityComparer.Default) &&
            //                e.Parameters[2].Type.IsArray(out var boundType) &&
            //                boundType.Equals(sint, SymbolEqualityComparer.Default);
            //        }
            //        WriteMethodInvocation(node, "System.Array.CreateInstance", methodFilter: FilterMethod, arguments: [elementType, lengths, bounds]);
            //        //var createInstanceWithLenghtAndBound = (IMethodSymbol)array.GetMembers("CreateInstance").Cast<IMethodSymbol>().Single(e => e.Parameters.Count() == 3 && e.Parameters[0].Type.Equals(sType, SymbolEqualityComparer.Default) && e.Parameters[1].Type.IsArray(out var lType) && lType.Equals(sint, SymbolEqualityComparer.Default) && e.Parameters[2].Type.IsArray(out var boundType) && boundType.Equals(sint, SymbolEqualityComparer.Default));
            //        //WriteMethodInvocation(node, createInstanceWithLenghtAndBound, null, null, [elementType, ranks, bounds], null, array, false);
            //    }
            //}
        }
        public void WriteCreateArray(CSharpSyntaxNode node, ITypeSymbol elementType, CodeNode? lengths, CodeNode? bounds, CodeNode? values)
        {
            //if (values != null)
            //{
            var typeMetadata = _global.GetMetadata(elementType!);
            var typeName = typeMetadata?.InvocationName ?? elementType!.Name;
            WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateArray", arguments: [new CodeNode(() => {
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.TypeOf}(");
                    CurrentTypeWriter.Write(node, typeName);
                    CurrentTypeWriter.Write(node, $")");
                }), values?? new CodeNode(()=>{
                    CurrentTypeWriter.Write(node, $"null");
                }), lengths??new CodeNode(()=>{
                    CurrentTypeWriter.Write(node, $"null");
                }), bounds??new CodeNode(()=>{
                    CurrentTypeWriter.Write(node, $"null");
                })]);
        }
        public void WriteCreateArray(CSharpSyntaxNode node, TypeSyntax elementType, IEnumerable<int> ranks, IEnumerable<int>? bounds, CodeNode? values)
        {
            //var rankLiterals = ranks.Select(l => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(l)));
            ////var rank = SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList<ExpressionSyntax>(rankLiterals));
            //var rank = SyntaxFactory.CollectionExpression(SyntaxFactory.SeparatedList(rankLiterals.Select(l => (CollectionElementSyntax)SyntaxFactory.ExpressionElement(l))));
            //var boundLiterals = bounds?.Select(l => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(l)));
            //var boundsNode = boundLiterals != null ? SyntaxFactory.CollectionExpression(SyntaxFactory.SeparatedList(boundLiterals.Select(l => (CollectionElementSyntax)SyntaxFactory.ExpressionElement(l)))) : null;
            WriteCreateArray(node, elementType, (Action)(() =>
            {
                CurrentTypeWriter.Write(node, "[");
                int ix = 0;
                foreach (var r in ranks)
                {
                    if (ix > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    CurrentTypeWriter.Write(node, r.ToString());
                    ix++;
                }
                CurrentTypeWriter.Write(node, "]");
            }), bounds != null ? (Action)(() =>
            {
                CurrentTypeWriter.Write(node, "[");
                int ix = 0;
                foreach (var r in bounds)
                {
                    if (ix > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    CurrentTypeWriter.Write(node, r.ToString());
                    ix++;
                }
                CurrentTypeWriter.Write(node, "]");
            }) : null, values);
        }

        public override void VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node)
        {
            CurrentTypeWriter.Write(node, "-1");
            //base.VisitOmittedArraySizeExpression(node);
        }

        public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            EnsureImported(node.Type);
            var rankLiterals = node.Type.RankSpecifiers.First().Sizes.ToArray();//.Select(l => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(l)));
            //var rank = SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList<ExpressionSyntax>(rankLiterals));
            //var rank = SyntaxFactory.CollectionExpression(SyntaxFactory.SeparatedList(rankLiterals.Select(l => (CollectionElementSyntax)SyntaxFactory.ExpressionElement(l))));
            WriteCreateArray(node, node.Type.ElementType, lengths: (Action)(() =>
            {
                CurrentTypeWriter.Write(node, "[");
                int ix = 0;
                foreach (var r in rankLiterals)
                {
                    if (ix > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    Visit(r);
                    ix++;
                }
                if (rankLiterals.Length == 0)
                {
                    if (node.Initializer != null)
                        CurrentTypeWriter.Write(node, node.Initializer.Expressions.Count().ToString());
                    else
                        CurrentTypeWriter.Write(node, "0");
                }
                CurrentTypeWriter.Write(node, "]");
            }), bounds: null, values: node.Initializer != null ? new CodeNode(() =>
            {
                CurrentTypeWriter.Write(node, "[");
                int ix = 0;
                foreach (var i in node.Initializer.Expressions)
                {
                    if (ix > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    Visit(i);
                    ix++;
                }
                CurrentTypeWriter.Write(node, "]");
            }) : null);
            //var symbol = (IArrayTypeSymbol)_global.GetTypeSymbol(node.Type, this/*, out _, out _*/);
            //var originalType = symbol.ElementType.OriginalDefinition;
            //var typeMetadata = _global.GetMetadata(originalType);
            //var typeName = typeMetadata?.InvocationName ?? originalType.Name;
            //if (node.Type.RankSpecifiers.Count == 1)
            //{
            //    EnsureImported("System.Array");
            //    var singleRank = node.Type.RankSpecifiers.First();
            //    if (node.Initializer == null && singleRank.Sizes.Count == 1)
            //    {
            //        var size = singleRank.Sizes.First();
            //        Writer.Write(node, $"{_global.GlobalName}.System.Array.Create({typeName})([");
            //        Visit(size);
            //        Writer.Write(node, $"])");
            //    }
            //    else if (node.Initializer == null && singleRank.Sizes.Count > 1)
            //    {
            //        Writer.Write(node, $"{_global.GlobalName}.System.Array.Create({typeName})([");
            //        int ix = 0;
            //        foreach (var rs in singleRank.Sizes)
            //        {
            //            if (ix > 0)
            //                Writer.Write(node, ", ");
            //            Visit(rs);
            //            ix++;
            //        }
            //        Writer.Write(node, $"])");
            //    }
            //    else
            //    {
            //        Writer.Write(node, $"{_global.GlobalName}.System.Array.Create({typeName})([{node.Initializer?.Expressions.Count ?? 0}], null, ");
            //        Visit(node.Initializer);
            //        Writer.Write(node, $")");
            //    }
            //}
            //else
            //{
            //    if (node.Initializer != null)
            //    {
            //        Visit(node.Initializer);
            //    }
            //    else
            //    {
            //        Writer.Write(node, "[ ");
            //        Writer.Write(node, " ]");
            //    }
            //    //throw new NotImplementedException();
            //}
        }

        public override void VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node)
        {
            ITypeSymbol elementType = (ITypeSymbol)_global.GetSymbol(((ArrayTypeSyntax)node.Type).ElementType, this);
            var type = InferType(node);
            var typeSymbol = type.typeSymbol?.GetOriginalRootDefinition() ??
                (type.type != null ? _global.TryGetTypeSymbol(type.type, this) : null)?.GetOriginalRootDefinition();
            //var spanType = _global.GetTypeSymbol("System.Span<>", this)!.GetTypeSymbol();
            //var readOnlySpanType = _global.GetTypeSymbol("System.ReadOnlySpan<>", this)!.GetTypeSymbol();
            //if ((typeSymbol?.Equals(spanType, SymbolEqualityComparer.Default) ?? false) ||
            //    (typeSymbol?.Equals(readOnlySpanType, SymbolEqualityComparer.Default) ?? false))
            //{
            //    var arrayConstructor = spanType.GetMembers(".ctor").Cast<IMethodSymbol>().Single(t => t.Parameters.Count() == 1 && t.Parameters[0].Type.IsArray(out _));
            //    var typeMetadata = _global.GetMetadata(elementType);
            //    var typeName = typeMetadata?.InvocationName ?? elementType.Name;
            //    WriteConstructorCall(node, spanType, arrayConstructor, null, null, suffixArguments: (Action)(() =>
            //    {
            //        Writer.Write(node, $"{_global.GlobalName}.System.Array.Create({typeName})([");
            //        VisitChildren(((ArrayTypeSyntax)node.Type).RankSpecifiers);
            //        Writer.Write(node, $"]");
            //        //if (node.Initializer != null)
            //        //{
            //        //    Writer.Write(node, $", null, ");
            //        //    Visit(node.Initializer);
            //        //}
            //        Writer.Write(node, $")");
            //    }));
            //}
            string methodName;
            if ((typeSymbol?.Equals(_global.SystemSpan, SymbolEqualityComparer.Default) ?? false))
            {
                methodName = "StackAllocSpan";
            }
            else if ((typeSymbol?.Equals(_global.SystemReadOnlySpan, SymbolEqualityComparer.Default) ?? false))
            {
                methodName = "StackAllocReadOnlySpan";
            }
            else //PointerType
            {
                methodName = "StackAllocPointer";
            }
            //var runtimeHelpers = _global.GetTypeSymbol("System.Runtime.CompilerServices.RuntimeHelpers", this)!.GetTypeSymbol();
            //var stackAlloc = (IMethodSymbol)runtimeHelpers.GetMembers(methodName).Single();
            //stackAlloc = stackAlloc.Construct(elementType);
            var arraySize = ((ArrayTypeSyntax)node.Type).RankSpecifiers.FirstOrDefault()?.Sizes.FirstOrDefault();
            if (arraySize?.IsKind(SyntaxKind.OmittedArraySizeExpression) ?? false)
                arraySize = null;
            WriteMethodInvocation(node,
                $"System.Runtime.CompilerServices.RuntimeHelpers.{methodName}", methodGenericTypes: [elementType],
                arguments: [arraySize ?? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression), (CSharpSyntaxNode?)node.Initializer ?? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)]);
            //WriteMethodInvocation(node, stackAlloc, null, null, [arraySize ?? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression), (CSharpSyntaxNode?)node.Initializer ?? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)], null, runtimeHelpers, false);
            //base.VisitStackAllocArrayCreationExpression(node);
        }

        public override void VisitSpreadElement(SpreadElementSyntax node)
        {
            CurrentTypeWriter.Write(node, "...");
            base.VisitSpreadElement(node);
        }

        ITypeSymbol? InferLeftHandSideType(CollectionExpressionSyntax node)
        {
            if (node.Parent is EqualsValueClauseSyntax eq && eq.Parent is VariableDeclaratorSyntax vr)
            {
                var eqLeft = vr.Identifier.ValueText;
                var variableType = GetIdentifierTypeInScope(eqLeft);
                return _global.ResolveSymbol(variableType, this/*, out _, out _*/)?.GetTypeSymbol();
            }
            else if (node.Parent is ArrowExpressionClauseSyntax ar && ar.Parent is MemberDeclarationSyntax member)
            {
                if (member is PropertyDeclarationSyntax p)
                {
                    return _global.GetTypeSymbol(p.Type, this);
                }
                else if (member is MethodDeclarationSyntax m)
                {
                    return _global.GetTypeSymbol(m.ReturnType, this);
                }
            }
            return null;
        }

        public void WriteCollectionElementsAsArray(CollectionExpressionSyntax node)
        {
            bool hasSpreadElement = node.Elements.Any(e => e.IsKind(SyntaxKind.SpreadElement));
            if (hasSpreadElement)
            {
                CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.ToArray}(");
            }
            CurrentTypeWriter.Write(node, $"[ ");
            int i = 0;
            foreach (var e in node.Elements)
            {
                if (i > 0)
                    CurrentTypeWriter.Write(node, ", ");
                Visit(e);
                i++;
            }
            CurrentTypeWriter.Write(node, " ]");
            if (hasSpreadElement)
            {
                CurrentTypeWriter.Write(node, ")");
            }
        }

        public override void VisitCollectionExpression(CollectionExpressionSyntax node)
        {
            var @class = node.FindClosestParent<BaseTypeDeclarationSyntax>();
            var symbol = _global.GetSymbol(@class!, this/*, out _, out _*/);
            bool isBootCode = _global.HasAttribute(symbol, typeof(BootAttribute).FullName, this, false, out _);

            //Disable collection expression in boot code as other classes are not available
            var lhsType = isBootCode ? null : (InferLeftHandSideType(node) ?? _global.TryGetTypeSymbol(node, this));
            //bool isArrayLHS = false;
            ITypeSymbol? elementType = null;
            if ((lhsType?.IsArray(out elementType) ?? false) || (lhsType?.IsEnumerable(out elementType) ?? false))
            {
                //isArrayLHS = true;
                var typeMetadata = _global.GetMetadata(elementType!);
                var typeName = typeMetadata?.InvocationName ?? elementType!.Name;
                WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateArray", arguments: [new CodeNode(() => {
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.TypeOf}({typeName})");
                }), new CodeNode(()=>{
                    WriteCollectionElementsAsArray(node);
                })]);
            }
            else if (lhsType != null)
            {
                if (_global.HasAttribute(lhsType, "System.Runtime.CompilerServices.CollectionBuilderAttribute"/*typeof(CollectionBuilderAttribute).FullName*/, this, false, out var args))
                {
                    var builderTypeArg = (ITypeSymbol)args[0];
                    var builderMethodName = (string)args[1];
                    var method = builderTypeArg.GetMembers(builderMethodName).FirstOrDefault() as IMethodSymbol;
                    if (method == null)
                    {
                        throw new InvalidOperationException("No method \"" + builderMethodName + "\" was found in type \"" + builderTypeArg.ToDisplayString() + "\"");
                    }
                    WriteMethodInvocation(node, method, null, node.Elements.Select(e => new CodeNode(e)), null, null, null, false);
                }
                else
                {
                    //if (TryInvokeMethodOperator(node, ImplicitOperatorName, lhsType, null, node.Elements))
                    //return;
                    WrapStatementsInExpression(node, () =>
                    {
                        var ix = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                        var instanceName = $"$t{ix}";
                        CurrentTypeWriter.Write(node, "let ");
                        CurrentTypeWriter.Write(node, instanceName);
                        CurrentTypeWriter.Write(node, " = ");
                        WriteConstructorCall(node, (INamedTypeSymbol)lhsType, lhsType.GetMembers(".ctor").Cast<IMethodSymbol>().Where(e => e.Parameters.Count() == 0).First());
                        CurrentTypeWriter.WriteLine(node, ";");
                        WriteInitializer(node, instanceName, lhsType, node.Elements);
                        CurrentTypeWriter.WriteLine(node, $"return {instanceName};", true);
                    });
                    //CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.Expression}(function()");
                    //CurrentTypeWriter.WriteLine(node, $"{{", true);
                    //var ix = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                    //var instanceName = $"$t{ix}";
                    //CurrentTypeWriter.Write(node, instanceName, true);
                    //CurrentTypeWriter.Write(node, " = ");
                    //WriteConstructorCall(node, (INamedTypeSymbol)lhsType, lhsType.GetMembers(".ctor").Cast<IMethodSymbol>().Where(e => e.Parameters.Count() == 0).First());
                    //CurrentTypeWriter.WriteLine(node, ";");
                    //WriteInitializer(node, instanceName, lhsType, node.Elements);
                    //CurrentTypeWriter.WriteLine(node, $"return {instanceName};", true);
                    //CurrentTypeWriter.Write(node, $"}}.bind(this))", true);
                }
            }
            else
            {
                //var sstring = _global.GetTypeSymbol("System.String", this);
                //CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{_global.GetAssemblyGlobalSlug(sstring.ContainingAssembly)}.System.Array.CreateInstance({_global.GlobalName}.{Constants.TypeOf}({typeName}), [-1], null, "); //the runtime will handle the -1 length based on the final lenght of the array
                WriteCollectionElementsAsArray(node);
                //CurrentTypeWriter.Write(node, $")");
            }
            //base.VisitCollectionExpression(node);
        }

        public override void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            var arrayType = (IArrayTypeSymbol)_global.GetTypeSymbol(node, this);
            WriteCreateArray(node, arrayType.ElementType, lengths: null, bounds: null, values: new CodeNode(() =>
            {
                CurrentTypeWriter.Write(node, "[");
                int ix = 0;
                foreach (var i in node.Initializer.Expressions)
                {
                    if (ix > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    Visit(i);
                    ix++;
                }
                CurrentTypeWriter.Write(node, "]");
            }));
            //base.VisitImplicitArrayCreationExpression(node);
        }

        public override void VisitArrayType(ArrayTypeSyntax node)
        {
            CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.TypeArray}(");
            Visit(node.ElementType);
            CurrentTypeWriter.Write(node, ")");
            //base.VisitArrayType(node);
        }
    }
}
