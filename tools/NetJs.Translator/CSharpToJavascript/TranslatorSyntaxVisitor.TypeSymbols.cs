using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        void WithSemanticModel(CSharpSyntaxNode expression, Action<SemanticModel> action)
        {
            foreach (var s in _semanticModels)
            {
                if (expression.SyntaxTree == s.SyntaxTree)
                {
                    action(s);
                }
            }
        }

        Dictionary<SyntaxNode, ITypeSymbol> _typeInferenceRegistry = new Dictionary<SyntaxNode, ITypeSymbol>();
        IDisposable RegisterTypeInference(SyntaxNode node, ITypeSymbol type)
        {
            _typeInferenceRegistry[node] = type;
            return new DelegateDispose(() => _typeInferenceRegistry.Remove(node));
        }

        (TypeSyntax? type, ITypeSymbol? typeSymbol) InferType(SyntaxNode node)
        {
            if (node.Parent?.Parent is VariableDeclarationSyntax variableDeclaration)
            {
                return (variableDeclaration.Type, null);
            }
            else if (node.Parent?.Parent?.Parent is VariableDeclarationSyntax variableDeclaration2)
            {
                return (variableDeclaration2.Type, null);
            }
            else if (node.Parent is AssignmentExpressionSyntax assignment)
            {
                var lhsType = _global.TryGetTypeSymbol(assignment.Left, this/*, out _, out _*/) ?? throw new InvalidOperationException($"Cannot Infer the typeof {node}");
                return (null, lhsType);
            }
            else if (node.Parent.IsKind(SyntaxKind.ReturnStatement)/*is ReturnStatementSyntax*/)
            {
                var member = node.FindClosestParent<MemberDeclarationSyntax>() ?? throw new InvalidOperationException($"Cannot find a member containig {node.Parent}");
                var memberSymbol = _global.GetSymbol(member, this/*, out _, out _*/);
                var lhsType = (memberSymbol as IMethodSymbol)?.ReturnType ??
                    (memberSymbol as IPropertySymbol)?.Type ??
                    throw new InvalidOperationException($"Cannot get return type from {memberSymbol}");
                return (null, lhsType);
            }
            else if (node.Parent.IsKind(SyntaxKind.YieldReturnStatement))
            {
                var member = node.FindClosestParent<MemberDeclarationSyntax>() ?? throw new InvalidOperationException($"Cannot find a member containig {node.Parent}");
                var memberSymbol = _global.GetSymbol(member, this/*, out _, out _*/);
                var lhsType = (memberSymbol as IMethodSymbol)?.ReturnType ??
                    (memberSymbol as IPropertySymbol)?.Type ??
                    throw new InvalidOperationException($"Cannot get return type from {memberSymbol}");
                var systemObject = (ITypeSymbol)_global.GetSymbol("System.Object", this);
                if (lhsType.SpecialType == SpecialType.System_Collections_IEnumerable)
                    return (null, systemObject);
                if (lhsType.SpecialType == SpecialType.System_Collections_IEnumerator)
                    return (null, systemObject);
                if (lhsType.SpecialType == SpecialType.System_Collections_Generic_IEnumerator_T)
                    return (null, ((INamedTypeSymbol)lhsType).TypeArguments[0]);
                if (lhsType.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                    return (null, ((INamedTypeSymbol)lhsType).TypeArguments[0]);
                if (lhsType.IsEnumerable(out var ts))
                    return (null, ts);
                if (lhsType.IsEnumerator(out var ts2))
                    return (null, ts2);
                return (null, lhsType);
            }
            else if (node.Parent.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                var property = node.FindClosestParent<PropertyDeclarationSyntax>();
                if (property != null)
                {
                    return (property.Type, null);
                }
                var method = node.FindClosestParent<MethodDeclarationSyntax>();
                if (method != null)
                {
                    return (method.ReturnType, null);
                }
                var conversion = node.FindClosestParent<ConversionOperatorDeclarationSyntax>();
                if (conversion != null)
                {
                    return (conversion.Type, null);
                }
            }
            else if (node.Parent.IsKind(SyntaxKind.Argument) && _typeInferenceRegistry.TryGetValue(node.Parent, out var t))
            {
                return (null, t);
            }
            else
            {
                var equalClause = node.FindClosestParent<EqualsValueClauseSyntax>();
                if (equalClause != null)
                {
                    if (equalClause.Parent is VariableDeclaratorSyntax vd)
                    {
                        var lhsType = (GetIdentifierTypeInScope(vd.Identifier.ValueText).TypeSyntaxOrSymbol as ISymbol)?.GetTypeSymbol();
                        if (lhsType != null)
                            return (null, lhsType);
                    }
                    else if (equalClause.Parent is IdentifierNameSyntax id)
                    {
                        var lhsType = (GetIdentifierTypeInScope(id.Identifier.ValueText).TypeSyntaxOrSymbol as ISymbol)?.GetTypeSymbol();
                        if (lhsType != null)
                            return (null, lhsType);
                    }
                }
                var massignment = node.FindClosestParent<AssignmentExpressionSyntax>();
                if (massignment != null)
                {
                    //if (massignment.Left is VariableDeclaratorSyntax vd)
                    //{
                    //    var lhsType = (CurrentClosure.GetIdentifierType(vd.Identifier.ValueText).TypeSyntaxOrSymbol as ISymbol)?.GetTypeSymbol();
                    //    if (lhsType != null)
                    //        return (null, lhsType);
                    //}
                    //else 
                    if (massignment.Left is IdentifierNameSyntax id)
                    {
                        var lhsType = (GetIdentifierTypeInScope(id.Identifier.ValueText).TypeSyntaxOrSymbol as ISymbol)?.GetTypeSymbol();
                        if (lhsType != null)
                            return (null, lhsType);
                    }
                }
                var fieldAssignment = node.FindClosestParent<BaseFieldDeclarationSyntax>();
                if (fieldAssignment != null)
                {
                    return (fieldAssignment.Declaration.Type, null);
                }
                var propertyAssignment = node.FindClosestParent<BasePropertyDeclarationSyntax>();
                if (propertyAssignment != null)
                {
                    return (propertyAssignment.Type, null);
                }
            }
            throw new InvalidOperationException($"Cannot infer the type of {node}");
        }



        Dictionary<SyntaxNode, SyntaxNode> _factoryAssociationNode = new Dictionary<SyntaxNode, SyntaxNode>();
        IDisposable AssociateSyntaxFactoryNode(CSharpSyntaxNode original, CSharpSyntaxNode newNode)
        {
            if (original != newNode)
            {
                Debug.Assert(original.GetType() == newNode.GetType());
                if (!original.SyntaxTree.HasCompilationUnitRoot)
                {
                    if (_factoryAssociationNode.TryGetValue(original, out var originalOriginal))
                        original = (CSharpSyntaxNode)originalOriginal;
                }
                Debug.Assert(original.SyntaxTree.HasCompilationUnitRoot);
                if (original.GetType() == newNode.GetType())
                {
                    var visitor = new AssociateSyntaxFactoryNewNodeVisitor(_factoryAssociationNode, original, newNode);
                    original.Accept(visitor);
                    return visitor;
                }
                else
                {
                    _factoryAssociationNode.Add(newNode, original);
                    return new DelegateDispose(() => { _factoryAssociationNode.Remove(newNode); });
                }
            }
            return new DelegateDispose(() => { });
        }

        public CodeSymbol GetExpressionBoundTarget(CSharpSyntaxNode expression)
        {
            var syntaxExpression = expression;
            if (_factoryAssociationNode.TryGetValue(expression, out var n))
            {
                syntaxExpression = (CSharpSyntaxNode)n;
            }
            if (syntaxExpression is RefExpressionSyntax rref)
                syntaxExpression = rref.Expression;
            if (syntaxExpression is ArgumentSyntax arg)
                syntaxExpression = arg.Expression;
            if (syntaxExpression is CastExpressionSyntax cast)
                syntaxExpression = cast.Expression;
            //if (expression is PrefixUnaryExpressionSyntax unary)
            //expression = unary.;
            foreach (var s in _semanticModels)
            {
                if (syntaxExpression.SyntaxTree == s.SyntaxTree)
                {
                    var sinfo = s.GetSymbolInfo(syntaxExpression);
                    if (sinfo.Symbol != null)
                    {
                        var symbol = sinfo.Symbol;
                        if (symbol is IMethodSymbol ms && ms.ReducedFrom != null)
                        {
                            if (ms.IsGenericMethod)
                            {
                                symbol = ms.ReducedFrom.Construct(ms.TypeArguments.ToArray());
                            }
                            else
                            {
                                symbol = ms.ReducedFrom;
                            }
                        }
                        return CodeSymbol.From(symbol);
                    }
                }
            }
            //I'd expect the roslyn api to handle this scenario
            //byte[] bb;
            //bb[1] should be bound to the System.Array [] operator, but it isn't. Obviously because the return type (object) isnt what it should be(byte)
            //But our generator needs to know that operator for correctness. We have defined a System.Array<T> stub type to handle this scenario
            if (syntaxExpression is ElementAccessExpressionSyntax ela)
            {
                var leftType = _global.ResolveSymbol(GetExpressionBoundTarget(ela.Expression), this/*, out _, out _*/)?.GetTypeSymbol();
                if (leftType?.IsArray(out var elementType) ?? false)
                {
                    var arrayT = (INamedTypeSymbol)_global.GetSymbol("System.Array<>", this/*, out _, out _*/);
                    arrayT = arrayT.Construct([elementType]);
                    //var argTypes = ela.ArgumentList.Arguments.Select(a => _global.ResolveTypeSymbol(GetExpressionReturnType(a), this, out _, out _));
                    //if (argTypes.All(a => a != null))
                    //{
                    var bestOperator = GetBestOverloadMethod(arrayT, "this[]", null, ela.ArgumentList.Arguments, null, out _);
                    if (bestOperator != null)
                    {
                        return CodeSymbol.From(bestOperator.AssociatedSymbol);
                    }
                    //}
                    return CodeSymbol.From(bestOperator);
                    //find the operator[] in arrayT
                }
            }
            return default;
        }
        public CodeSymbol GetExpressionReturnSymbol(CSharpSyntaxNode expression,
             CodeSymbol? lhs = null,
             ITypeSymbol? lamdaReturnType = null,
             IEnumerable<ITypeSymbol>? lamdaParameterTypes = null,
             Dictionary<string, ITypeSymbol>? lamdaIdentifierType = null)
        {
            var syntaxExpression = expression;
            if (_factoryAssociationNode.TryGetValue(expression, out var n))
            {
                syntaxExpression = (CSharpSyntaxNode)n;
            }
            foreach (var s in _semanticModels)
            {
                if (syntaxExpression.SyntaxTree == s.SyntaxTree)
                {
                    //var sinfo = s.GetSymbolInfo(expression);
                    //if (sinfo.Symbol != null)
                    //{
                    //    return CodeType.From(sinfo.Symbol);
                    //}
                    var target = s.GetSymbolInfo(syntaxExpression).Symbol;
                    if (target != null)
                    {
                        if (target is IMethodSymbol ms && ms.Name.StartsWith("op_")) //operator
                            return CodeSymbol.From(ms.ReturnType);
                        return CodeSymbol.From(target);
                    }
                    var info = s.GetTypeInfo(syntaxExpression);
                    if ((info.Type ?? info.ConvertedType) != null)
                    {
                        return CodeSymbol.From(info.Type ?? info.ConvertedType);
                    }
                    //if (info.ConvertedType != null)
                    //{
                    //    var conversion = s.GetConversion(expression);
                    //    if (conversion.IsImplicit)
                    //    {
                    //        var implicitConverters = info.ConvertedType.GetMembers("op_Implicit");
                    //        var bestConverter = GetBestOverloadMethod(info.ConvertedType, implicitConverters.Cast<IMethodSymbol>(), null, [expression], null, out _);
                    //        if (bestConverter != null)
                    //        {
                    //            return CodeType.From(bestConverter.Parameters.Single().Type);
                    //        }
                    //    }
                    //    var operations = s.GetOperation(expression);
                    //}
                }
            }
            //var sym = semanticModel.GetSymbolInfo(expression).Symbol;
            if (expression is IdentifierNameSyntax id)
            {
                if (lamdaIdentifierType != null)
                {
                    if (lamdaIdentifierType.TryGetValue(id.Identifier.ValueText, out var idV))
                        return CodeSymbol.From(idV);
                }
                var idType = GetIdentifierTypeInScope(id.Identifier.ValueText);
                if (idType.TypeSyntaxOrSymbol != null)
                    return idType;
                var symbol = _global.TryGetSymbol(id, this/*, out _, out _*/);
                return CodeSymbol.From(symbol);
            }
            else if (expression is GenericNameSyntax gnn)
            {
                var idType = GetIdentifierTypeInScope(gnn.Identifier.ValueText + "<" + string.Join(",", Enumerable.Range(1, gnn.Arity).Select(e => "")) + ">");
                if (idType.TypeSyntaxOrSymbol != null)
                    return idType;
                var symbol = _global.GetSymbol(gnn, this/*, out _, out _*/);
                return CodeSymbol.From(symbol);
            }
            else if (expression is TypeSyntax typ)
            {
                var symbol = _global.TryGetSymbol(typ, this/*, out _, out _*/);
                return CodeSymbol.From(symbol);
            }
            else if (expression is RefExpressionSyntax rref)
            {
                return GetExpressionReturnSymbol(rref.Expression, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
            }
            else if (expression is ArgumentSyntax arg)
            {
                return GetExpressionReturnSymbol(arg.Expression, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
            }
            else if (expression is ObjectCreationExpressionSyntax create)
                return CodeSymbol.From(create.Type, SymbolKind.ErrorType);
            else if (expression is ArrayCreationExpressionSyntax arrayCreate)
            {
                var symbol = _global.Compilation.CreateArrayTypeSymbol((ITypeSymbol)_global.GetSymbol(arrayCreate.Type.ElementType, this/*, out _, out _*/));
                return CodeSymbol.From(symbol);
            }
            else if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var memberAccessName = memberAccess.Name;
                var lhsType = GetExpressionReturnSymbol(memberAccess.Expression, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                if (lhsType.TypeSyntaxOrSymbol != null)
                {
                    var lhsSymbol = _global.ResolveSymbol(lhsType, this/*, out _, out _*/);
                    if (lhsSymbol != null)
                    {
                        if (lhsSymbol.Equals(_global.Compilation.DynamicType, SymbolEqualityComparer.Default))
                            return CodeSymbol.From(lhsSymbol);
                        TypeArgumentListSyntax? genericArgs = null;
                        if (memberAccess.Name is GenericNameSyntax gn)
                        {
                            genericArgs = gn.TypeArgumentList;
                        }
                        if (expression.Parent is InvocationExpressionSyntax invoke)
                        {
                            IMethodSymbol? method = GetExpressionBoundTarget(invoke).TypeSyntaxOrSymbol as IMethodSymbol;
                            if (method != null)
                            {
                                //method = (IMethodSymbol)method.SubstituteGenericType(overloadResult.GenericTypeSubstitutions, global);
                                return CodeSymbol.From(method);
                            }
                            ITypeSymbol? type = lhsSymbol.GetTypeSymbol();
                            if (type != null)
                            {
                                method = GetBestOverloadMethod(type, memberAccessName.Identifier.ValueText, genericArgs, invoke.ArgumentList.Arguments, null, out var overloadResult);
                                if (method != null)
                                {
                                    //method = (IMethodSymbol)method.SubstituteGenericType(overloadResult.GenericTypeSubstitutions, global);
                                    return CodeSymbol.From(method);
                                }
                            }
                        }
                        if (lhsSymbol is INamespaceOrTypeSymbol nOrT)
                        {
                            var accessedMember = nOrT.GetMembers(memberAccess.Name.Identifier.ValueText, _global).FirstOrDefault();
                            if (accessedMember is INamespaceOrTypeSymbol name)
                                return CodeSymbol.From(name);
                            else if (accessedMember is IMethodSymbol method)
                                return CodeSymbol.From(method.ReturnType);
                            else if (accessedMember is IPropertySymbol property)
                                return CodeSymbol.From(property.Type);
                            else if (accessedMember is IFieldSymbol field)
                                return CodeSymbol.From(field.Type);
                        }

                    }
                }
            }
            else if (expression is ConditionalAccessExpressionSyntax condition)
            {
                var lhsType = GetExpressionReturnSymbol(condition.Expression, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                if (lhsType.TypeSyntaxOrSymbol != null)
                {
                    return GetExpressionReturnSymbol(condition.WhenNotNull, lhsType, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                }
            }
            else if (expression is InvocationExpressionSyntax invocation)
            {
                var bmethod = GetExpressionBoundTarget(invocation).TypeSyntaxOrSymbol as IMethodSymbol;
                if (bmethod != null)
                {
                    return CodeSymbol.From(bmethod.ReturnType);
                }
                //Weird that there is no dedicated Expression for nameof() operator
                if (invocation.Expression is IdentifierNameSyntax idn && idn.Identifier.ValueText == "nameof")
                {
                    return CodeSymbol.From(_global.GetSymbol("System.String", this/*, out _, out _*/));
                }
                //if (invocation.Expression is MemberAccessExpressionSyntax ma && ma.Name.Identifier.ValueText == "ToString" && invocation.ArgumentList.Arguments.Count == 0)
                //{
                //    return CodeType.From(GetTypeSymbol("System.String"));
                //}
                //if (invocation.Expression is MemberBindingExpressionSyntax mb && mb.Name.Identifier.ValueText == "ToString" && invocation.ArgumentList.Arguments.Count == 0)
                //{
                //    return CodeType.From(GetTypeSymbol("System.String"));
                //}

                if (lhs != null)
                {
                    var lhsSymbol = _global.ResolveSymbol(lhs.Value, this/*, out _, out _*/);
                    if (lhsSymbol != null)
                    {
                        ITypeSymbol? type = lhsSymbol as ITypeSymbol ?? (lhsSymbol as IFieldSymbol)?.Type ?? (lhsSymbol as IPropertySymbol)?.Type ?? (lhsSymbol as IMethodSymbol)?.ReturnType;
                        if (type != null)
                        {
                            if (invocation.Expression is MemberBindingExpressionSyntax mbb)
                            {
                                var mmethod = GetBestOverloadMethod(type, mbb.Name.Identifier.ValueText, null, invocation.ArgumentList.Arguments, null, out var overloadResult);
                                if (mmethod != null)
                                {
                                    //mmethod = (IMethodSymbol)mmethod.SubstituteGenericType(genericTypeSubstitutions, global);
                                    return CodeSymbol.From(mmethod.ReturnType);
                                }
                            }
                        }
                    }
                }
                ISymbol? method = null;
                var rt = GetExpressionReturnSymbol(invocation.Expression, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                if (rt.TypeSyntaxOrSymbol is MemberSymbolOverload overload)
                {
                    method = overload.ResolveMethod(this, (TypeArgumentListSyntax?)invocation.ChildNodes().FirstOrDefault(e => e.IsKind(SyntaxKind.TypeArgumentList)), invocation.ArgumentList, out _);
                }
                else
                {
                    var type = _global.ResolveSymbol(rt, this/*, out var declaringType, out _*/);
                    method = type;
                    //if (method != null && method.ContainingAssembly is ITypeSymbol ts)
                    //{
                    //    var resolvedMethod = GetBestOverloadMethod(ts, [(IMethodSymbol)method], (TypeArgumentListSyntax?)invocation.ChildNodes().FirstOrDefault(e => e.IsKind(SyntaxKind.TypeArgumentList)), invocation.ArgumentList, out _);
                    //}
                }
                if (method is IMethodSymbol m)
                {
                    return CodeSymbol.From(m.ReturnType);
                }
                else if (method is INamedTypeSymbol toDelegate && toDelegate.DelegateInvokeMethod != null)
                {
                    return CodeSymbol.From(toDelegate.DelegateInvokeMethod.ReturnType);
                }
                else if (method is INamedTypeSymbol nt && nt.IsType("NetJs.Union")) // method is overload
                {
                    return CodeSymbol.From(nt);
                }
            }
            else if (expression is LiteralExpressionSyntax literal)
            {
                if (literal.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    return CodeSymbol.From(_global.GetSymbol(literal.Token.ValueText.EndsWith("f") ? "System.Float" : literal.Token.ValueText.Contains(".") ? "System.Double" : "System.Int32", this/*, out _, out _*/));
                }
                else if (literal.IsKind(SyntaxKind.StringLiteralExpression))
                    return CodeSymbol.From(_global.SystemString);
                else if (literal.IsKind(SyntaxKind.TrueLiteralExpression) || literal.IsKind(SyntaxKind.FalseLiteralExpression))
                    return CodeSymbol.From(_global.SystemBoolean);
                else if (literal.IsKind(SyntaxKind.CharacterLiteralExpression))
                    return CodeSymbol.From(_global.SystemChar);
                else if (literal.IsKind(SyntaxKind.NullLiteralExpression))
                    return CodeSymbol.From(_global.GetSymbol("NetJs.Null", this/*, out _, out _*/));
                else if (literal.IsKind(SyntaxKind.DefaultLiteralExpression))
                    return CodeSymbol.From(_global.GetSymbol("NetJs.Default", this/*, out _, out _*/));
            }
            else if (expression is RangeExpressionSyntax range)
            {
                return CodeSymbol.From(_global.GetSymbol("System.Range", this/*, out _, out _*/));
            }
            else if (expression is PrefixUnaryExpressionSyntax un && un.OperatorToken.ValueText == "^")
            {
                return CodeSymbol.From(_global.GetSymbol("System.Index", this/*, out _, out _*/));
            }
            else if (expression is BaseExpressionSyntax _base)
            {
                var type = Utilities.FindClosestParent<BaseTypeDeclarationSyntax>(_base);
                if (type != null)
                {
                    var baseType = type.BaseList?.Types.FirstOrDefault();
                    if (baseType != null)
                    {
                        var symbol = _global.TryGetSymbol(baseType.Type, this/*, out _, out _*/);
                        return CodeSymbol.From(symbol);
                    }
                    if (type is StructDeclarationSyntax)
                    {
                        return CodeSymbol.From(_global.GetSymbol("System.ValueType", this/*, out _, out _*/));
                    }
                    return CodeSymbol.From(_global.GetSymbol("System.Object", this/*, out _, out _*/));
                }
            }
            else if (expression is CastExpressionSyntax cast)
            {
                var t = _global.TryGetTypeSymbol(cast.Type, this);
                if (t != null)
                    return CodeSymbol.From(t);
                return CodeSymbol.From(cast.Type, SymbolKind.ErrorType);
            }
            else if (expression is ParenthesizedExpressionSyntax pa)
            {
                return GetExpressionReturnSymbol(pa.Expression, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
            }
            else if (expression is ImplicitArrayCreationExpressionSyntax arr)
            {
                foreach (var e in arr.Initializer.Expressions)
                {
                    var t = GetExpressionReturnSymbol(e);
                    if (t.TypeSyntaxOrSymbol != null)
                    {
                        return CodeSymbol.From(_global.Compilation.CreateArrayTypeSymbol((ITypeSymbol)_global.ResolveSymbol(t, this/*, out _, out _*/)!.GetTypeSymbol()));
                    }
                }
            }
            else if (expression is CollectionExpressionSyntax coll)
            {
                foreach (var e in coll.Elements)
                {
                    var t = GetExpressionReturnSymbol(e);
                    if (t.TypeSyntaxOrSymbol != null)
                    {
                        //TODO: We probably need to return a union of all types that support CollectionExpression, not just array
                        return CodeSymbol.From(_global.Compilation.CreateArrayTypeSymbol((ITypeSymbol)_global.ResolveSymbol(t, this)!.GetTypeSymbol()));
                    }
                }
            }
            else if (expression is ExpressionElementSyntax ee)
            {
                return GetExpressionReturnSymbol(ee.Expression, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
            }
            else if (expression is ElementAccessExpressionSyntax elementAccess)
            {
                var type = GetExpressionReturnSymbol(elementAccess.Expression, lhs: lhs, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                var symbolType = _global.ResolveSymbol(type, this)?.GetTypeSymbol();
                if (symbolType != null && symbolType.IsArray(out var elementType) && elementAccess.ArgumentList is BracketedArgumentListSyntax)
                {
                    return CodeSymbol.From(elementType);
                }
                else if (symbolType != null && elementAccess.ArgumentList is BracketedArgumentListSyntax br)
                {
                    //TODO: We should be cheking if the getItem can receive the bracketed parameters
                    var getItem = symbolType.GetMembers("get_Item", _global).FirstOrDefault(e => e is IMethodSymbol m && m.Parameters.Count() == br.Arguments.Count);
                    if (getItem != null)
                    {
                        return CodeSymbol.From(((IMethodSymbol)getItem).ReturnType);
                    }
                }
            }
            else if (expression is BinaryExpressionSyntax bin)
            {
                if (bin.IsKind(SyntaxKind.LogicalAndExpression) ||
                    bin.IsKind(SyntaxKind.LogicalOrExpression) ||
                    bin.IsKind(SyntaxKind.LogicalNotExpression) ||
                    bin.IsKind(SyntaxKind.NotEqualsExpression) ||
                    bin.IsKind(SyntaxKind.EqualsExpression) ||
                    bin.IsKind(SyntaxKind.GreaterThanExpression) ||
                    bin.IsKind(SyntaxKind.GreaterThanOrEqualExpression) ||
                    bin.IsKind(SyntaxKind.LessThanExpression) ||
                    bin.IsKind(SyntaxKind.LessThanOrEqualExpression))
                    return CodeSymbol.From(_global.GetSymbol("System.Boolean", this/*, out _, out _*/));
                //TODO: which one shall we return
                var ilhs = GetExpressionReturnSymbol(bin.Left, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                //var irhs = GetExpressionReturnType(bin.Right);
                return ilhs;
            }
            else if (expression is ThisExpressionSyntax _this)
            {
                var type = expression.FindClosestParent<BaseTypeDeclarationSyntax>() ?? CurrentType;
                return CodeSymbol.From(type);
            }
            else if (expression is ConditionalExpressionSyntax cond)
            {
                //TODO: return the lesser type
                var ilhs = GetExpressionReturnSymbol(cond.WhenTrue, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                var irhs = GetExpressionReturnSymbol(cond.WhenFalse, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                return ilhs;
            }
            else if (expression is TypeOfExpressionSyntax)
            {
                return CodeSymbol.From(_global.GetSymbol("System.Type", this/*, out _, out _*/));
            }
            else if (expression is DefaultExpressionSyntax _default)
            {
                return CodeSymbol.From(_default.Type, SymbolKind.ErrorType);
            }
            else if (expression is PostfixUnaryExpressionSyntax postfix)
            {
                return GetExpressionReturnSymbol(postfix.Operand, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
            }
            else if (expression is PrefixUnaryExpressionSyntax prefix)
            {
                return GetExpressionReturnSymbol(prefix.Operand, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
            }
            else if (expression is AnonymousMethodExpressionSyntax al)
            {
                return GetLambdaReturnType(expression, al.ParameterList?.Parameters);
                //var paramsCount = al.ParameterList?.Parameters.Count ?? 0;
                //if (paramsCount == 0)
                //{
                //    if (al.DescendantNodes().Any(r => r.IsKind(SyntaxKind.ReturnStatement)))
                //        return CodeType.From(GetTypeSymbol("dotnetJs.Function<>", out _));
                //    else
                //        return CodeType.From(GetTypeSymbol("dotnetJs.Action", out _));
                //}
                //else
                //{
                //    bool hasReturn = al.DescendantNodes().Any(r => r.IsKind(SyntaxKind.ReturnStatement));
                //    return CodeType.From(GetTypeSymbol($"dotnetJs.Function<{string.Join(",", Enumerable.Range(1, paramsCount + (hasReturn ? 1 : 0)).Select(c => ""))}>", out _));
                //}
            }
            else if (expression is ParenthesizedLambdaExpressionSyntax pl)
            {
                return GetLambdaReturnType(expression, pl.ParameterList.Parameters);
                //var paramsCount = pl.ParameterList?.Parameters.Count ?? 0;
                //if (paramsCount == 0)
                //{
                //    if (pl.DescendantNodes().Any(r => r.IsKind(SyntaxKind.ReturnStatement)))
                //        return CodeType.From(GetTypeSymbol("dotnetJs.Function<>", out _));
                //    else
                //        return CodeType.From(GetTypeSymbol("dotnetJs.Action", out _));
                //}
                //else
                //{
                //    bool hasExplicitReturn = pl.DescendantNodes().Any(r => r.IsKind(SyntaxKind.ReturnStatement));
                //    if (hasExplicitReturn)
                //    {
                //        return CodeType.From(GetTypeSymbol($"dotnetJs.Function<{string.Join(",", Enumerable.Range(1, paramsCount + 1).Select(c => ""))}>", out _));
                //    }
                //    else
                //    {
                //        var asFunc = (ITypeSymbol)GetTypeSymbol($"dotnetJs.Function<{string.Join(",", Enumerable.Range(1, paramsCount + 1).Select(c => ""))}>", out _);
                //        var asAction = (ITypeSymbol)GetTypeSymbol($"dotnetJs.Action<{string.Join(",", Enumerable.Range(1, paramsCount).Select(c => ""))}>", out _);
                //        return CodeType.From(Union([asFunc, asAction]));
                //    }
                //}
            }
            else if (expression is SimpleLambdaExpressionSyntax sl)
            {
                return GetLambdaReturnType(expression, [sl.Parameter]);
            }
            else if (expression is TupleExpressionSyntax tp)
            {
                var items = tp.Arguments.Select(a => _global.ResolveSymbol(GetExpressionReturnSymbol(a, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType), this/*, out _, out _*/)!.GetTypeSymbol()).ToArray();
                var asTuple = (INamedTypeSymbol)_global.GetSymbol($"System.Tuple<{string.Join(",", Enumerable.Range(1, items.Length).Select(c => ""))}>", this/*, out _, out _*/);
                var tuple = asTuple.Construct(items);
                return CodeSymbol.From(tuple);
            }
            else if (expression is AwaitExpressionSyntax aw)
            {
                var result = GetExpressionReturnSymbol(aw.Expression, lamdaReturnType: lamdaReturnType, lamdaParameterTypes: lamdaParameterTypes, lamdaIdentifierType: lamdaIdentifierType);
                if (result.TypeSyntaxOrSymbol != null)
                {
                    var symbol = _global.ResolveSymbol(result, this/*, out _, out _*/)?.GetTypeSymbol();
                    return CodeSymbol.From(((INamedTypeSymbol?)symbol)?.TypeArguments[0]);
                }
            }
            else if (expression is InterpolatedStringExpressionSyntax)
            {
                return CodeSymbol.From(_global.GetSymbol("System.String", this/*, out _, out _*/));
            }
            CodeSymbol GetLambdaReturnType(CSharpSyntaxNode expression, IEnumerable<ParameterSyntax>? parameters)
            {
                var paramsCount = parameters?.Count() ?? 0;
                CSharpSyntaxNode? returnExpression = ((ReturnStatementSyntax?)expression.DescendantNodes().FirstOrDefault(r => r.IsKind(SyntaxKind.ReturnStatement)))?.Expression;
                if (returnExpression == null)
                {
                    returnExpression = (CSharpSyntaxNode)expression.ChildNodes().Last();
                    if (returnExpression is BlockSyntax block)
                    {
                        returnExpression = ((ReturnStatementSyntax?)block.DescendantNodes().FirstOrDefault(r => r.IsKind(SyntaxKind.ReturnStatement)))?.Expression;
                    }
                }
                if (returnExpression != null || lamdaReturnType != null)
                {
                    if (lamdaParameterTypes == null || paramsCount == lamdaParameterTypes.Count())
                    {
                        if (lamdaParameterTypes != null)
                        {
                            lamdaIdentifierType ??= new Dictionary<string, ITypeSymbol>();
                            if (parameters != null)
                            {
                                int ix = 0;
                                foreach (var parameter in parameters)
                                {
                                    lamdaIdentifierType[parameter.Identifier.ValueText] = lamdaParameterTypes.ElementAt(ix);
                                    ix++;
                                }
                            }
                        }
                        var lamdaReturn = returnExpression != null ? GetExpressionReturnSymbol(returnExpression, lamdaIdentifierType: lamdaIdentifierType) : default;
                        if (lamdaReturn.TypeSyntaxOrSymbol == null)
                            lamdaReturn = CodeSymbol.From(lamdaReturnType);
                        var lamdaReturnSymbol = _global.ResolveSymbol(lamdaReturn, this/*, out _, out _*/)?.GetTypeSymbol();
                        if (lamdaReturnSymbol != null) //func
                        {
                            var asFunc = (INamedTypeSymbol)_global.GetSymbol($"NetJs.Function<{string.Join(",", Enumerable.Range(1, paramsCount + 1).Select(c => ""))}>", this/*, out _, out _*/);
                            var func = asFunc.Construct([.. (lamdaParameterTypes ?? asFunc.TypeArguments.Take(asFunc.TypeArguments.Count() - 1)), lamdaReturnSymbol]);
                            return CodeSymbol.From(func);
                        }
                        else //action
                        {
                            var asAction = (INamedTypeSymbol)_global.GetSymbol($"NetJs.Action<{string.Join(",", Enumerable.Range(1, paramsCount).Select(c => ""))}>", this/*, out _, out _*/);
                            var action = asAction.Construct([.. (lamdaParameterTypes ?? asAction.TypeArguments.Take(asAction.TypeArguments.Count()))]);
                            return CodeSymbol.From(action);
                        }
                    }
                }
                bool hasExplicitReturn = returnExpression != null;
                if (hasExplicitReturn)
                {
                    return CodeSymbol.From(_global.GetSymbol($"NetJs.Function<{string.Join(",", Enumerable.Range(1, paramsCount + 1).Select(c => ""))}>", this/*, out _, out _*/));
                }
                else
                {
                    var asFunc = (ITypeSymbol)_global.GetSymbol($"NetJs.Function<{string.Join(",", Enumerable.Range(1, paramsCount + 1).Select(c => ""))}>", this/*, out _, out _*/);
                    var asAction = (ITypeSymbol)_global.GetSymbol($"NetJs.Action<{string.Join(",", Enumerable.Range(1, paramsCount).Select(c => ""))}>", this/*, out _, out _*/);
                    return CodeSymbol.From(_global.Union([asFunc, asAction], this));
                }
            }
            return default;
        }


        RefKind? GetRefKind(ExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.RefExpression))
                return RefKind.Ref;
            if (expression.IsKind(SyntaxKind.ConditionalExpression))
            {
                var refWhenTrue = GetRefKind(((ConditionalExpressionSyntax)expression).WhenTrue);
                if (refWhenTrue != null)
                    return refWhenTrue;
                var refWhenFalse = GetRefKind(((ConditionalExpressionSyntax)expression).WhenFalse);
                if (refWhenFalse != null)
                    return refWhenFalse;
            }
            if (expression is ParenthesizedExpressionSyntax pr)
            {
                return GetRefKind(pr.Expression);
            }
            return null;
        }
    }
}
