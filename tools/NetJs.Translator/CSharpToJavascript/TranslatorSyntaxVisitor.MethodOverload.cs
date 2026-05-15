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
    public struct MethodParameterResult
    {
        public ISymbol? SelectedUnionItem { get; set; }
        public CodeSymbol OutType { get; set; }
        public string? OutName { get; set; }
        public List<CSharpSyntaxNode> Arguments { get; set; }
        public ITypeSymbol? ArgumentType { get; set; }
        public override string ToString()
        {
            return $"({string.Join(", ", Arguments.Select(a => a.ToString()))}){(SelectedUnionItem != null ? "<=>" : "")}{SelectedUnionItem}";
        }
    }

    public struct MethodOverloadResult
    {
        public Dictionary<ITypeParameterSymbol, ISymbol>? GenericTypeSubstitutions { get; set; }
        public Dictionary<IParameterSymbol, MethodParameterResult>? ParameterValueSubstitutions { get; set; }
    }

    public partial class TranslatorSyntaxVisitor
    {
        int MapMethodParameters(
            ITypeSymbol targetThis,
            IMethodSymbol method,
            TypeArgumentListSyntax? explicitGenericArgs,
            IEnumerable<CSharpSyntaxNode> parameterArgs,
            ExpressionSyntax? suffixParameter,
            bool validate,
            out IMethodSymbol updatedMethod,
            out MethodOverloadResult result)
        {
            if (explicitGenericArgs != null)
            {
                var ts = explicitGenericArgs.Arguments.Select(a => _global.GetTypeSymbol(a, this)).ToArray();
                if (ts.Any(t => t is not ITypeParameterSymbol))
                    method = method.Construct(ts);
            }
            updatedMethod = method;
            Dictionary<IParameterSymbol, MethodParameterResult> _parameterSubstitutions = new Dictionary<IParameterSymbol, MethodParameterResult>(SymbolEqualityComparer.Default);
            Dictionary<ITypeParameterSymbol, ISymbol>? _genericTypeSubstitutions = new Dictionary<ITypeParameterSymbol, ISymbol>(SymbolEqualityComparer.Default);
            int i = 0;
            int weight = 0;
            bool calledAsExtensionMethod = method.IsStatic && !targetThis.OriginalDefinition.Equals(method.ContainingSymbol.OriginalDefinition, SymbolEqualityComparer.Default);
            foreach (var parameter in method.Parameters)
            {
                ITypeSymbol? unionItemSelected = null;
                int MapSingleArgument(CSharpSyntaxNode? arg)
                {
                    int w = -1;
                    CodeSymbol outType = default;
                    ITypeSymbol? argType = null;
                    string? outName = null;
                    var argument = arg as ArgumentSyntax;
                    ExpressionSyntax? argExpression = argument?.Expression ?? (ExpressionSyntax?)arg;
                    if (arg == null && suffixParameter != null && ReferenceEquals(parameter, method.Parameters.Last()))
                    {
                        argExpression = suffixParameter;
                    }
                    if (argExpression == null)
                    {
                        if (parameter.HasExplicitDefaultValue)
                        {
                            w = 1;
                        }
                        else
                        {
                            w = -1;
                        }
                    }
                    else if (validate)
                    {
                        if (argument != null && argument.RefKindKeyword.ValueText == "out" && argument.Expression is DeclarationExpressionSyntax declaration)
                        {
                            if (declaration.Type is IdentifierNameSyntax id)
                            {
                                if (id.IsVar)
                                {
                                    outType = CodeSymbol.From(parameter);
                                }
                            }
                            outName = (declaration.Designation as SingleVariableDesignationSyntax)?.Identifier.ValueText;
                            w = 1;
                        }
                        else
                        {
                            bool isDelegate = parameter.Type.IsDelegate(out var delegateReturnType, out var delegateParameterTypes);
                            argType = _global.GetTypeSymbol(argExpression, this);
                            //argType = _global.ResolveSymbol(GetExpressionReturnSymbol(argExpression!, lamdaReturnType: delegateReturnType, lamdaParameterTypes: delegateParameterTypes), this/*, out _, out _*/)?.GetTypeSymbol();
                            w = argType?.CanConvertTo(parameter.Type, _global, _genericTypeSubstitutions, out unionItemSelected, fromExpressionHint: argExpression, visitor: this) ?? -1;
                        }
                    }
                    _parameterSubstitutions[parameter] = new MethodParameterResult
                    {
                        SelectedUnionItem = unionItemSelected,
                        Arguments = arg!= null ? [arg] : [],
                        ArgumentType = argType,
                        OutType = outType,
                        OutName = outName
                    };
                    return w;
                }
                if (parameter.IsParams)
                {
                    var remaining = parameterArgs.Skip(i - (method.IsExtensionMethod ? 1 : 0));
                    if (validate)
                    {
                        int ix = 0;
                        foreach (var arg in remaining)
                        {
                            if (ix == 0 && remaining.Count() == 1) //if remaining argument is 1 and is is an array that matched the param
                            {
                                var ww = MapSingleArgument(arg);
                                if (ww >= 0)
                                {
                                    weight += ww;
                                    break;
                                }
                            }
                            //var w = MapSingleArgument(arg);
                            var type = _global.ResolveSymbol(GetExpressionReturnSymbol(arg), this/*, out _, out _*/);
                            var w = type?.CanConvertTo(((IArrayTypeSymbol)parameter.Type).ElementType, _global, _genericTypeSubstitutions, out unionItemSelected) ?? -1;
                            if (w <= 0)
                            {
                                result = default;
                                return -1;
                            }
                            weight += w;
                            ix++;
                        }
                    }
                    _parameterSubstitutions[parameter] = new MethodParameterResult
                    {
                        SelectedUnionItem = unionItemSelected,
                        Arguments = [.. remaining]
                    };
                    break;
                }
                else
                {
                    var useArg = parameterArgs.SingleOrDefault(e => e is ArgumentSyntax ar && ar.NameColon?.Name.Identifier.ValueText == parameter.Name);
                    if (useArg != null)
                    {
                        _parameterSubstitutions[parameter] = new MethodParameterResult
                        {
                            Arguments = [useArg]
                        };
                    }
                    else
                    {
                        if (method.IsExtensionMethod && calledAsExtensionMethod)
                        {
                            if (i == 0)
                            {
                                if (validate)
                                {
                                    var w = targetThis.CanConvertTo(parameter.Type, _global, _genericTypeSubstitutions, out unionItemSelected);
                                    if (w <= 0)
                                    {
                                        result = default;
                                        return -1;
                                    }
                                    weight += w;
                                }
                                _parameterSubstitutions[parameter] = new MethodParameterResult
                                {
                                    SelectedUnionItem = unionItemSelected,
                                    Arguments = [] //first extension parameter lef tas a hole
                                };
                            }
                            else
                            {
                                var arg = parameterArgs.ElementAtOrDefault(i - 1);
                                var w = MapSingleArgument(arg);
                                if (w < 0)
                                {
                                    result = default;
                                    return -1;
                                }
                                weight += w;
                            }
                        }
                        else
                        {
                            var arg = parameterArgs.ElementAtOrDefault(i);
                            var w = MapSingleArgument(arg);
                            if (w < 0)
                            {
                                result = default;
                                return -1;
                            }
                            weight += w;
                        }
                    }
                }
                i++;
                if (method.Arity > 0 && _genericTypeSubstitutions.Count == method.Arity && updatedMethod.Equals(method, SymbolEqualityComparer.Default))
                {
                    var ts = _genericTypeSubstitutions.OrderBy(o => method.TypeParameters.IndexOf(o.Key)).Select(e => e.Value).Cast<ITypeSymbol>().ToArray();
                    //var alreadySubstituted = !method.OriginalDefinition.Equals(method, SymbolEqualityComparer.Default);
                    updatedMethod = method.Construct(ts);
                    int ix = 0;
                    //make sure the new parameter types for the updated method exist in the dictionary too
                    foreach (var p in updatedMethod.Parameters)
                    {
                        var initialP = method.Parameters.ElementAt(ix);
                        if (_parameterSubstitutions.TryGetValue(initialP, out var v))
                            _parameterSubstitutions[p] = v;
                        ix++;
                    }
                    //once we know the infered generic arguments of a method, start mapping all over
                    //return MapMethodParameters(targetThis, updatedMethod, parameterArgs, validate, out _, out result);
                }
            }
            result = new MethodOverloadResult
            {
                GenericTypeSubstitutions = _genericTypeSubstitutions,
                ParameterValueSubstitutions = _parameterSubstitutions
            };
            return weight;
        }

        internal IMethodSymbol? GetBestOverloadMethod(
            ITypeSymbol targetType,
            IEnumerable<IMethodSymbol> candidates,
            TypeArgumentListSyntax? explicitGenericArgs,
            IEnumerable<CSharpSyntaxNode>? parameterArgs,
            ExpressionSyntax? suffixParameter,
            out MethodOverloadResult result)
        {
            //bool callAsMemberMethod = true;
            //if (targetType is INamedTypeSymbol nt)
            //{
            //    if (nt.IsStatic)
            //    {
            //        callAsMemberMethod = false;
            //    }
            //}
            result = default;
            if (!candidates.Any())
            {
                return null;
            }
            IMethodSymbol? candidate = null;
            IMethodSymbol? updatedCandidate = null;
            //Save time if there is only one call target
            //We skip the check for it as Visual Studio is doing checks anyway
            //Since we also need to copy the genericType subsstitution, we dont skip if there is a generic argument
            if (candidates.Count() == 1 && explicitGenericArgs == null && (parameterArgs?.Count() ?? 0) == 0 && candidates.Single().Arity == 0)
            {
                candidate = candidates.Single();
                updatedCandidate = candidate;
                //if (genericTypeSubstitutions != null)
                //{
                //    foreach (var arg in argTypes)
                //    {
                //        var symbol = GetTypeSymbol(arg.Value);
                //        if (symbol is ITypeParameterSymbol tp)
                //            genericTypeSubstitutions[tp.Name] = symbol;
                //    }
                //}
            }
            else
            {
                //var typeArguments = explicitGenericArgs?.Arguments ?? Enumerable.Empty<TypeSyntax>();
                var arguments = parameterArgs ?? Enumerable.Empty<ArgumentSyntax>();
                //var argTypes = arguments.ToDictionary(a => a, a => GetTypeSymbol(GetExpressionReturnType(a.Expression)));
                //var explicitTypeArgTypes = typeArguments.ToDictionary(a => a, a => GetExpressionReturnType(a));
                //var parameterArgTypes = arguments.ToDictionary(a => a, a =>
                //{
                //    var v = GetExpressionReturnType(a.Expression);
                //    //if (v.TypeSyntaxOrSymbol == null)
                //    //throw new InvalidOperationException($"Cannot compute return type for {a}. Consider declaring it into a typed(not var) local variable first.");
                //    return v;
                //});
                Dictionary<IMethodSymbol, MethodOverloadResult> results = new(SymbolEqualityComparer.Default);
                //Dictionary<IMethodSymbol,MethodOverloadResult> _parameterValueSubstitutions = new(SymbolEqualityComparer.Default);
                var max = candidates.Select(candidate =>
                {
                    var w = MapMethodParameters(targetType, candidate, explicitGenericArgs, arguments, suffixParameter, true, out var updatedMethod, out var _result);
                    if (w >= 0)
                    {
                        results[candidate] = _result;
                    }
                    return (candidate, updatedMethod, w);
                }).Where(c => c.w >= 0).DefaultIfEmpty().MaxBy(c => c.w);
                candidate = max.candidate;
                updatedCandidate = max.updatedMethod;
                if (candidate != null)
                {
                    if (results.TryGetValue(candidate, out var mresult))
                    {
                        result = mresult;
                    }
                }
            }
            if (candidate != null && candidate.Arity > 0 && explicitGenericArgs != null)
            {
                var args = explicitGenericArgs.Arguments.Select(a => (ITypeSymbol?)_global.TryGetSymbol(a, this/*, out _, out _*/)).ToArray();
                if (args.All(a => a != null))
                {
                    candidate = candidate.Construct(args!);
                    if (result.GenericTypeSubstitutions != null)
                    {
                        int i = 0;
                        foreach (var tp in candidate.TypeParameters)
                        {
                            result.GenericTypeSubstitutions[tp] = candidate.TypeArguments.ElementAt(i);
                            i++;
                        }
                    }
                }
            }
            else if (candidate != null)
            {
                if (result.GenericTypeSubstitutions != null && targetType is INamedTypeSymbol ntt && ntt.Arity > 0 && ntt.TypeArguments.Count() > 0)
                {
                    int i = 0;
                    foreach (var tp in ntt.TypeParameters)
                    {
                        result.GenericTypeSubstitutions[tp] = ntt.TypeArguments.ElementAt(i);
                        i++;
                    }
                }
            }
            return updatedCandidate;
        }

        IMethodSymbol? GetBestOverloadMethod(
            ITypeSymbol type,
            string methodName,
            TypeArgumentListSyntax? explicitGenericArgs,
            IEnumerable<CSharpSyntaxNode>? parameterArgs,
            ExpressionSyntax? suffixParameter,
            out MethodOverloadResult result)
        {
            var candidates = type.GetMembers(methodName, _global).Select(e =>
            {
                if (e is IMethodSymbol)
                    return e;
                if (methodName == "this[]")
                {
                    return (e as IPropertySymbol)?.GetMethod;
                }
                return null;
            }).Where(e =>
            {
                var method = e as IMethodSymbol;
                if (method != null)
                {
                    if (explicitGenericArgs != null)
                    {
                        if (method.Arity != explicitGenericArgs.Arguments.Count())
                            return false;
                    }
                    //method doesnt have default parameter ad no params, make sure all parameters are passed
                    if (method.Parameters.All(e => !e.HasExplicitDefaultValue && !e.IsParams))
                    {
                        if ((parameterArgs?.Count() ?? 0) != method.Parameters.Count())
                            return false;
                    }
                    return true;
                }
                return false;
            }).Cast<IMethodSymbol>().ToList();
            MethodOverloadResult _result = default;
            var method = GetBestOverloadMethod(type, candidates, explicitGenericArgs, parameterArgs, suffixParameter, out _result);
            if (method != null)
            {
                result = _result;
                return method;
            }
            if (!type.IsStatic) //try extension methods
            {
                candidates = _global.ExtensionMethods.GetValueOrDefault(methodName)?.Where(method =>
                {
                    if (explicitGenericArgs != null)
                    {
                        if (method.Arity != explicitGenericArgs.Arguments.Count())
                            return false;
                    }
                    var thisType = method.Parameters.First().Type;
                    return type.GetOriginalRootDefinition().CanConvertTo(thisType.GetOriginalRootDefinition(), _global, null, out _) > 0;
                }).Cast<IMethodSymbol>().ToList();
                if (candidates != null)
                {
                    method = GetBestOverloadMethod(type, candidates, explicitGenericArgs, parameterArgs, suffixParameter, out _result);
                    if (method != null)
                    {
                        result = _result;
                        return method;
                    }
                }
            }
            //As a last resort, if the type is a System.Array, use it
            if (type is IArrayTypeSymbol)
            {
                var systemArray = _global.GetSymbol("System.Array", this/*, out _, out _*/);
                return GetBestOverloadMethod((ITypeSymbol)systemArray, methodName, explicitGenericArgs, parameterArgs, suffixParameter, out result);
            }
            result = default;
            return null;
        }
    }
}
