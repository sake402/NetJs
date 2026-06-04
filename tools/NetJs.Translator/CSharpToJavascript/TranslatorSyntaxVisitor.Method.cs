using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        string? GetMethodModifier(CSharpSyntaxNode methodNode, SyntaxTokenList modifiers, TypeSyntax? methodReturnType, bool commentAll = false)
        {
            string? modifier = null;
            if (commentAll)
                modifier = "/*";
            if (modifiers.Any(e => e.ValueText == "static"))
            {
                modifier += "static ";
            }
            bool isGeneric = false;
            if (methodNode is MethodDeclarationSyntax mt && mt.Arity > 0)
                isGeneric = true;
            if (methodReturnType != null)
            {
                var returnType = methodReturnType.ToString().Trim();
                if (!commentAll)
                    modifier += $"/*";
                modifier += $"{returnType.Replace("/*", "/ *").Replace("*/", "* /")}";
                if (!commentAll)
                    modifier += $"*/ ";
            }
            modifier = modifier?.Trim();
            if (commentAll)
                modifier += "*/";
            //A generic method is implemeted as a factory of function. It cant be async itself, but the factory result can be async
            if (!isGeneric && modifiers.IsAsync())
            {
                modifier += "async ";
            }
            return modifier;
        }

        string GetMethodParameterModifier(ParameterSyntax parameter)
        {
            return $"/*{string.Join(" ", parameter.Modifiers.Select(m => m.ValueText))}{(parameter.Modifiers.Count > 0 ? " " : "")}{parameter.Type?.ToString().Trim()}*/";
        }

        void WriteMethodGenericArgument(CSharpSyntaxNode node, IMethodSymbol method, Dictionary<ITypeParameterSymbol, ISymbol>? genericTypeSubstitutions = null)
        {
            if (_global.HasAttribute(method, typeof(IgnoreGenericAttribute).FullName, this, false, out _))
                return;
            var parameters = method.TypeParameters;
            if (method.Arity == 0 && method.Name == ".ctor")
            {
                parameters = method.ContainingType.TypeParameters;
            }
            if (parameters.Any())
            {
                CurrentTypeWriter.Write(node, "(");
                int ix = 0;
                foreach (var t in parameters)
                {
                    if (ix > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    if (t is INamedTypeSymbol nt)
                    {
                        CurrentTypeWriter.Write(node, nt.ComputeOutputTypeName(_global));
                    }
                    else if (t is IArrayTypeSymbol arr)
                    {
                        CurrentTypeWriter.Write(node, arr.ComputeOutputTypeName(_global));
                    }
                    else if (t.Equals(_global.Compilation.DynamicType, SymbolEqualityComparer.Default))
                    {
                        var systemObject = _global.GetSymbol("System.Object", this/*, out _, out _*/);
                        var meta = _global.GetRequiredMetadata(systemObject);
                        CurrentTypeWriter.Write(node, meta.OverloadName ?? "System.Object");
                    }
                    else
                    {
                        var subst = genericTypeSubstitutions?.FirstOrDefault(e => e.Key.Name == t.Name).Value;
                        if (subst != null && subst is not ITypeParameterSymbol)
                        {
                            var substSymbol = _global.GetMetadata(subst);
                            CurrentTypeWriter.Write(node, substSymbol?.OverloadName ?? t.Name);
                        }
                        else
                        {
                            CurrentTypeWriter.Write(node, t.Name);
                        }
                    }
                    ix++;
                }
                CurrentTypeWriter.Write(node, ")");
            }
        }

        void WriteSingleMethodInvocationArgument(
            CSharpSyntaxNode node,
            int index,
            CodeNode? arg,
            ITypeSymbol? argType,
            IParameterSymbol parameter,
            MethodOverloadResult overloadResult,
            bool? enableBoxing = null)
        {
            var substitution = overloadResult.ParameterValueSubstitutions?.GetValueOrDefault(parameter);
            if (substitution != null && substitution.Value.SelectedUnionItem != null && arg != null && arg.IsT0)
            {
                var options = GetExpressionReturnSymbol(arg.AsT0);
                if (options.TypeSyntaxOrSymbol is MemberSymbolOverload overloads)
                {
                    var matched = overloads.Overloads.Where(e => e is IMethodSymbol).FirstOrDefault(v => v.CanConvertTo(substitution.Value.SelectedUnionItem, _global, null, out _) > 0);
                    if (matched != null)
                    {
                        var meta = _global.GetRequiredMetadata(matched);
                        if ((meta.InvocationName ?? meta.OverloadName) != null)
                        {
                            CurrentTypeWriter.Write(node, meta.InvocationName ?? meta.OverloadName!);
                            if (!matched.IsStatic)
                                CurrentTypeWriter.Write(node, ".bind(this)");
                            return;
                        }
                    }
                }
            }
            if (arg == null && parameter.HasExplicitDefaultValue)
            {
                string? defaultValue = null;
                var vDefaultValue = parameter.ExplicitDefaultValue;
                if (vDefaultValue is bool b)
                {
                    defaultValue = b ? "true" : "false";
                }
                else if (vDefaultValue is string s)
                {
                    defaultValue = "\"" + s + "\"";
                }
                else if (vDefaultValue is char c)
                {
                    defaultValue = ((int)c).ToString();
                }
                else
                {
                    defaultValue = vDefaultValue?.ToString();
                }
                if (defaultValue == null && !parameter.Type.IsNullable(out _) && parameter.Type.IsValueType)
                {
                    defaultValue = $"{parameter.Type.ComputeOutputTypeName(_global)}.default()";
                }
                CurrentTypeWriter.Write(node, defaultValue ?? "null");
            }
            else if (arg == null)
            {
                CurrentTypeWriter.Write(node, "{");
                CurrentTypeWriter.Write(node, index.ToString());
                CurrentTypeWriter.Write(node, "}");
            }
            else
            {
                IDisposable? disposeAnonymousTypeDefinition = null;
                if (parameter.Type.IsDelegate(out var delegateReturnType, out var delegateParameterTypes))
                {
                    disposeAnonymousTypeDefinition = CurrentClosure.DefineAnonymousMethodParameterTypes(delegateParameterTypes!);
                }
                if ((parameter.RefKind == RefKind.Out || parameter.RefKind == RefKind.Ref) && arg.IsT0 && arg.AsT0 is ArgumentSyntax aarg && aarg.Expression is DeclarationExpressionSyntax dec && dec.Designation is SingleVariableDesignationSyntax sv)
                {
                    var localSymbol = _global.TryGetSymbol(sv, this/*, out _, out _*/);
                    if (localSymbol != null)
                        CurrentClosure.DefineIdentifierType(sv.Identifier.ValueText, CodeSymbol.From(localSymbol));
                    else
                        CurrentClosure.DefineIdentifierType(sv.Identifier.ValueText, CodeSymbol.From(parameter.Type));
                }
                if (arg.IsT0)
                {
                    var disposable = RegisterTypeInference(arg.AsT0, parameter.Type);
                    var method = (IMethodSymbol)parameter.ContainingSymbol;
                    var box = enableBoxing ?? true;
                    if (_global.HasAttribute(parameter, typeof(BoxAttribute).FullName, this, false, out var barg))
                    {
                        box = (bool)barg[0]!;
                    }
                    else if (method.IsExtern)
                    {
                        box = false;
                    }
                    WriteVariableAssignment(node, null, parameter, null, arg.AsT0, argType, enableBoxing: box);
                    disposable.Dispose();
                    //Visit(arg);
                    disposeAnonymousTypeDefinition?.Dispose();
                }
                else if (arg.IsT1)
                {
                    arg.AsT1();
                }
            }
        }

        void WriteMethodDeclarationParameters(CSharpSyntaxNode node, SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            int i = 0;
            if (node.Parent.IsKind(SyntaxKind.ExtensionBlockDeclaration))
            {
                var extensionBlock = (ExtensionBlockDeclarationSyntax)node.Parent;
                var extensionParameter = extensionBlock.ParameterList!.Parameters.Single();
                CurrentTypeWriter.Write(node, $"/*this {extensionParameter.Type}*/{extensionParameter.Identifier.ValueText}", true);
                i++;
            }
            var symbol = _global.TryGetSymbol(node, this/*, out _, out _*/);
            foreach (var p in parameters/*.Where(e => e.Default == null)*/)
            {
                EnsureImported(p.Type);
                if (symbol is IMethodSymbol method)
                {
                    var parameter = method.Parameters.ElementAt(i);
                    CurrentClosure.DefineIdentifierType(p.Identifier.ValueText, CodeSymbol.From(parameter));
                }
                else if (symbol is IPropertySymbol property)
                {
                    var parameter = property.Parameters.ElementAt(i);
                    CurrentClosure.DefineIdentifierType(p.Identifier.ValueText, CodeSymbol.From(parameter));
                }
                else if (p.Type != null)
                    CurrentClosure.DefineIdentifierType(p.Identifier.ValueText, p.Type, SymbolKind.Parameter);
                if (i > 0)
                    CurrentTypeWriter.Write(node, ", ");
                CurrentTypeWriter.Write(node, $"{GetMethodParameterModifier(p)} {p.Identifier.ResolveIdentifierName()}");
                i++;
            }
            //if (parameters.Where(e => e.Default != null).Any())
            //{
            //    if (i > 0)
            //        Writer.Write(node, ", ");
            //    int ix = 0;
            //    Writer.Write(node, "{ ");
            //    foreach (var p in parameters.Where(e => e.Default != null))
            //    {
            //        EnsureImported(p.Type);
            //        if (p.Type != null)
            //            CurrentClosure.DefineIdentifierType(p.Identifier.ValueText, p.Type);
            //        if (ix > 0)
            //            Writer.Write(node, ", ");
            //        Writer.Write(node, $"/*{p.Type?.ToFullString().Trim()}*/ {p.Identifier.Text}");
            //        Visit(p.Default);
            //        i++;
            //        ix++;
            //    }
            //    Writer.Write(node, "}");
            //}
        }

        void WriteAdditionalArgument(CSharpSyntaxNode node, CodeNode arg, ref int ix)
        {
            if (arg.IsT0)
            {
                if (ix > 0)
                    CurrentTypeWriter.Write(node, ", ");
                Visit(arg.AsT0);
                ix++;
            }
            else if (arg.IsT1)
            {
                if (ix > 0)
                    CurrentTypeWriter.Write(node, ", ");
                arg.AsT1();
                ix++;
            }
            //else if (arg.IsT2)
            //{
            //    if (ix > 0)
            //        Writer.Write(node, ", ");
            //    Writer.Write(node, arg.AsT2);
            //    ix++;
            //}
        }

        void WriteMethodInvocationParameter(
            CSharpSyntaxNode node,
            IMethodSymbol? targetMethod,
            TypeArgumentListSyntax? genericArgs,
            IEnumerable<CodeNode>? arguments,
            CodeNode? prefixArguments = null,
            CodeNode? suffixArguments = null,
            MethodOverloadResult overloadResult = default,
            bool calledAsExtensionMethod = false,
            bool writeBrackets = true)
        {
            if (writeBrackets)
                CurrentTypeWriter.Write(node, "(");
            //if (node is InvocationExpressionSyntax)
            //{
            //    IEnumerable<GenericNameSyntax> genericNames =
            //    [
            //        .. node.ChildNodes().Where(m => m.IsKind(SyntaxKind.GenericName)/* m is GenericNameSyntax*/).Cast<GenericNameSyntax>(),
            //        .. node.ChildNodes().Where(e=> e is MemberAccessExpressionSyntax).Cast<MemberAccessExpressionSyntax>().Select(m => m.Name).Where(m => m is GenericNameSyntax).Cast<GenericNameSyntax>()
            //    ];
            //    if (genericNames.Any())
            //    {
            //        var genericArguments = string.Join(",", genericNames.Select(a =>
            //        {
            //            return string.Join(", ", a.TypeArgumentList.Arguments.Select(a => Utilities.ResolveTypeName(a)));
            //        }));
            //        Writer.Write(node, genericArguments);
            //        if (arguments?.Arguments.Any() ?? false)
            //            Writer.Write(node, ", ");
            //    }
            //}
            int ix = 0;
            if (prefixArguments != null)
            {
                WriteAdditionalArgument(node, prefixArguments, ref ix);
            }
            if (arguments != null)
            {
                if (targetMethod != null)
                {
                    IEnumerable<IParameterSymbol> parameters = targetMethod.Parameters;
                    if (targetMethod.IsExtensionMethod && calledAsExtensionMethod && prefixArguments != null)
                    {
                        parameters = parameters.Skip(1).ToList();
                    }
                    //The given arguments is ordered the way we pass it to the method, if the method has default argument that is not passed, we need to inser it here
                    if (parameters.Any(p => p.HasExplicitDefaultValue))
                    {
                        Dictionary<IParameterSymbol, CodeNode> argumentsMapping = new Dictionary<IParameterSymbol, CodeNode>(SymbolEqualityComparer.Default);
                        var firstDefaultParameter = parameters.Select((t, i) => (t, i)).FirstOrDefault(p => p.t.HasExplicitDefaultValue);
                        var firstDefaultParametrerIndex = firstDefaultParameter.i;
                        for (int i = 0; i < firstDefaultParametrerIndex; i++)
                        {
                            var parameter = parameters.ElementAt(i);
                            argumentsMapping.Add(parameter, arguments.ElementAt(i));
                        }
                        bool hasNamedArgument = arguments.Any(e => e.IsT0 && e.AsT0 is ArgumentSyntax ar && ar.NameColon != null);
                        for (int i = firstDefaultParametrerIndex; i < parameters.Count(); i++)
                        {
                            var parameterName = parameters.ElementAt(i).Name;
                            var arg = arguments.Where(e => e.IsT0 && e.AsT0 is ArgumentSyntax).SingleOrDefault(e => ((ArgumentSyntax)e.AsT0).NameColon?.Name.Identifier.ValueText == parameterName);
                            if (arg == null && !hasNamedArgument)
                            {
                                arg = arguments.ElementAtOrDefault(i);
                            }
                            if (arg != null)
                            {
                                argumentsMapping.Add(parameters.ElementAt(i), arg);
                            }
                            else
                            {
                                var parameter = parameters.ElementAt(i);
                                argumentsMapping.Add(parameter, new CodeNode(() =>
                                {
                                    if (_global.HasAttribute(parameter, "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute", this, false, out var args))
                                    {
                                        var name = (string)args[0]!;
                                        var marg = argumentsMapping.Single(e => e.Key.Name == name).Value;
                                        if (marg.IsT0)
                                        {
                                            var exp = marg.AsT0.ToString().Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
                                            CurrentTypeWriter.Write(node, $"\"{exp}\"");
                                            return;
                                        }
                                    }
                                    if (_global.HasAttribute(parameter, typeof(CallerFilePathAttribute).FullName, this, false, out _))
                                    {
                                        var exp = node.SyntaxTree.FilePath;
                                        CurrentTypeWriter.Write(node, $"\"{exp}\"");
                                        return;
                                    }
                                    if (_global.HasAttribute(parameter, typeof(CallerMemberNameAttribute).FullName, this, false, out _))
                                    {
                                        var member = node.FindClosestParent<MemberDeclarationSyntax>();
                                        var exp = (member as MethodDeclarationSyntax)?.Identifier.ValueText ??
                                        //(member as LocalFunctionStatementSyntax)?.Identifier.ValueText ??
                                        (member as PropertyDeclarationSyntax)?.Identifier.ValueText ??
                                        (member as ConstructorDeclarationSyntax)?.Identifier.ValueText ?? "";
                                        CurrentTypeWriter.Write(node, $"\"{exp}\"");
                                        return;
                                    }
                                    if (_global.HasAttribute(parameter, typeof(CallerLineNumberAttribute).FullName, this, false, out _))
                                    {
                                        var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
                                        // Note: Line numbers are 0-based in the Roslyn API
                                        int lineNumber = lineSpan.StartLinePosition.Line + 1;
                                        // You can also get the character (column) position
                                        int columnNumber = lineSpan.StartLinePosition.Character + 1;
                                        CurrentTypeWriter.Write(node, $"{lineNumber}");
                                        return;
                                    }
                                    if (!TryWriteConstant(node, parameter.Type, null, new Optional<object?>(parameter.ExplicitDefaultValue)))
                                    {
                                        throw new InvalidOperationException($"Don't know how to write constant {parameter.ExplicitDefaultValue}");
                                    }
                                }));
                            }
                        }
                        if (argumentsMapping.Count > 0)
                            arguments = argumentsMapping.Values;
                    }
                    int arg_i = 0;
                    List<CodeNode> remainingArguments = new(arguments);
                    foreach (var parameter in parameters)
                    {
                        var arg = remainingArguments.FirstOrDefault(e => e.IsT0 && e.AsT0 is ArgumentSyntax ar && ar.NameColon != null && ar.NameColon.Name.ToString() == parameter.Name) ?? remainingArguments.FirstOrDefault();
                        if (arg == null && !parameter.IsParams && !parameter.HasExplicitDefaultValue)
                        {
                            if (targetMethod.Parameters.Count() == arguments.Count() + 1 && parameter.Name == "value" && suffixArguments != null) //if we are writing an indexer, break and write the last parameter supplied as the value
                            {
                                break;
                            }
                            throw new InvalidOperationException($"No argument was supplied for {parameter.Name} in {targetMethod}");
                        }
                        var argSubstitution = overloadResult.ParameterValueSubstitutions?.GetValueOrDefault(parameter);
                        var argType = argSubstitution?.ArgumentType;
                        //parameter.Type.IsDelegate(out var delegateReturnType, out var delegateParameterTypes);
                        //var argType = arg != null ? (ITypeSymbol?)GetTypeSymbol(GetExpressionReturnType(arg.Expression, lamdaParameterTypes: delegateParameterTypes), out _) : null;
                        if (parameter.IsParams && (argType == null || argType.CanConvertTo(parameter.Type, _global, null, out _) <= 0))
                        {
                            if (remainingArguments.Count() == 1) //if the last parameter passed is an array than can convert directly to the target type. dont create another array to wrap it again
                            {
                                var singleParam = remainingArguments.Single();
                                var singleParamType = singleParam.IsT0 ? _global.TryGetTypeSymbol(singleParam.AsT0, this) : null;
                                if (singleParamType?.CanConvertTo(parameter.Type, _global, null, out _) >= 0)
                                {
                                    if (ix > 0)
                                        CurrentTypeWriter.Write(node, ", ");
                                    WriteSingleMethodInvocationArgument(node, arg_i, arg, argType, parameter, overloadResult, null);
                                    //Visit(arg.Expression);
                                    if (arg != null)
                                        remainingArguments.Remove(arg);
                                    break;
                                }
                            }
                            if (ix > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            int iip = 0;
                            bool isReadOnlySpan = SymbolEqualityComparer.Default.Equals(parameter.Type.OriginalDefinition, _global.SystemReadOnlySpan);
                            bool isIEnumerable = SymbolEqualityComparer.Default.Equals(parameter.Type.OriginalDefinition, _global.SystemIEnumerableT);

                            void WriteRemainingArgsAsArray(ITypeSymbol? argType = null)
                            {
                                bool spread = parameter.HasAttribute(typeof(SpreadAttribute).FullName, false, out _);
                                if (!spread)
                                    CurrentTypeWriter.Write(node, "[ ", false);
                                foreach (var argument in remainingArguments)
                                {
                                    if (iip > 0)
                                        CurrentTypeWriter.Write(node, ", ");
                                    WriteSingleMethodInvocationArgument(node, arg_i, argument, argType, parameter, overloadResult, null);
                                    //Visit(argument.Expression);
                                    iip++;
                                }
                                if (!spread)
                                    CurrentTypeWriter.Write(node, " ]", false);
                            }
                            if (isReadOnlySpan)
                            {
                                ITypeSymbol? largType = null;
                                var implicitConverter = parameter.Type.GetMembers(ImplicitOperatorName)
                                    .Cast<IMethodSymbol>()
                                    .Single(e => e.Parameters.Length == 1 && e.Parameters[0].Type.IsArray(out largType));
                                WriteMethodInvocation(node, implicitConverter, null, [new CodeNode(()=>{
                                    WriteRemainingArgsAsArray(largType);
                                })], null, null);
                            }
                            else if (isIEnumerable)
                            {
                                WriteCreateArray(node, ((INamedTypeSymbol)parameter.Type).TypeArguments[0], null, null, new CodeNode(() =>
                                {
                                    WriteRemainingArgsAsArray(((INamedTypeSymbol)parameter.Type).TypeArguments[0]);
                                }));
                            }
                            else
                            {
                                WriteRemainingArgsAsArray();
                            }
                            break;
                        }
                        else
                        {
                            if (ix > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            WriteSingleMethodInvocationArgument(node, arg_i, arg, argType, parameter, overloadResult, null);
                            //Visit(arg.Expression);
                            if (arg != null)
                                remainingArguments.Remove(arg);
                        }
                        ix++;
                        arg_i++;
                    }
                }
                else
                {
                    int arg_i = 0;
                    foreach (var arg in arguments.Where(e => (e.IsT0 ? e.AsT0 as ArgumentSyntax : null)?.NameColon == null))
                    {
                        if (ix > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        if (arg.IsT0)
                            WriteVariableAssignment(node, null, null, null, arg.AsT0);
                        else
                            arg.AsT1();
                        //Visit(arg);
                        //WriteSingleMethodInvocationArgument(node, arg_i,arg,null);
                        ix++;
                        arg_i++;
                    }
                    if (arguments.Any(e => e.IsT0 && e.AsT0 is ArgumentSyntax ar && ar.NameColon != null))
                    {
                        if (ix > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        CurrentTypeWriter.Write(node, "{ ");
                        int ix2 = 0;
                        foreach (var arg in arguments.Where(e => e.IsT0 && e.AsT0 is ArgumentSyntax ar && ar.NameColon != null))
                        {
                            if (ix2 > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            WriteVariableAssignment(node, null, null, null, arg.AsT0);
                            //Visit(arg);
                            ix2++;
                            ix++;
                        }
                        CurrentTypeWriter.Write(node, " }");
                    }
                }
            }
            if (suffixArguments != null)
            {
                WriteAdditionalArgument(node, suffixArguments, ref ix);
            }
            if (writeBrackets)
                CurrentTypeWriter.Write(node, ")");
        }

        ///// <summary>
        ///// if a method is not an explicit interface implementation, but this method implements an interface method though,
        ///// we need to create an alias to this method with the interface name
        ///// </summary>
        ///// <param name="node"></param>
        ///// <param name="explicitInterface"></param>
        ///// <param name="parameters"></param>
        ///// <param name="methodSymbol"></param>
        //void TryWriteImplementedMethod(CSharpSyntaxNode node, ExplicitInterfaceSpecifierSyntax? explicitInterface, ParameterListSyntax? parameters, IMethodSymbol? methodSymbol)
        //{
        //    if (explicitInterface == null && methodSymbol != null && methodSymbol.ContainingType.Interfaces.Any())
        //    {
        //        if (!methodSymbol.IsExtern && !_global.HasAttribute(methodSymbol, typeof(ExternalAttribute).FullName, this, false, out _) && !_global.HasAttribute(methodSymbol.ContainingSymbol, typeof(ExternalAttribute).FullName, this, false, out _))
        //        {
        //            //find the interfaces that this method implements
        //            var implementedMethods = methodSymbol.ContainingType.AllInterfaces
        //                .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
        //                .Where(im => methodSymbol.Equals(methodSymbol.ContainingType.FindImplementationForInterfaceMember(im), SymbolEqualityComparer.Default));
        //            var metadata = _global.GetMetadata(methodSymbol);
        //            var methodInvocationName = metadata?.InvocationName ?? throw new InvalidOperationException();// Utilities.ResolveMethodName(node);
        //            foreach (var implementedMethod in implementedMethods)
        //            {
        //                if (discriminatedInterfaceMethodImplemented.Contains(implementedMethod, SymbolEqualityComparer.Default))
        //                    continue;
        //                if (!implementedMethod.IsExtern && !_global.HasAttribute(implementedMethod, typeof(ExternalAttribute).FullName, this, false, out _) && !_global.HasAttribute(implementedMethod.ContainingSymbol, typeof(ExternalAttribute).FullName, this, false, out _))
        //                {
        //                    OpenClosure(node);
        //                    CurrentTypeWriter.WriteLine(node, $"//Generated explicit method implemetation for {implementedMethod}", true);
        //                    var symbol = _global.GetRequiredMetadata(implementedMethod);
        //                    CurrentTypeWriter.Write(node, $"{(implementedMethod.IsStatic || methodSymbol.IsStaticCallConvention(_global) ? "static " : "")}{(methodSymbol.IsStaticCallConvention(_global) ? "/*conventional*/ " : "")}{symbol.OverloadName}", true);
        //                    CurrentTypeWriter.Write(node, $"(", false);
        //                    //No need to write method argument explicitly since we use sptread operator on arguments
        //                    //WriteMethodDeclarationParameters(node, parameters?.Parameters ?? default);
        //                    CurrentTypeWriter.WriteLine(node, $")", false);
        //                    CurrentTypeWriter.WriteLine(node, $"{{", true);

        //                    //For implemented interface members call that may conflict in name:
        //                    //eg if a class implement both IComparer<string> and IComparer<char>
        //                    //the compare implementation methods for both implementation will conflictly be named System$Collections$Generic$IComparer$$$Compare
        //                    //even though one will receive a string and the other a char
        //                    //We need to discriminate the one we intend to call by checking the type Ts passed as a last argument to this
        //                    var conflictingInterfaces = methodSymbol.ContainingType.AllInterfaces.GroupBy(e => e.OriginalDefinition, SymbolEqualityComparer.Default).Where(a => a.Count() > 1);
        //                    if (conflictingInterfaces.Any())
        //                    {
        //                        var conflictingMethods = conflictingInterfaces.SelectMany(g => g.SelectMany(i => i.GetMembers().Where(m => m.OriginalDefinition.Equals(implementedMethod.OriginalDefinition, SymbolEqualityComparer.Default))))
        //                            .Cast<IMethodSymbol>();
        //                        var conflctingTs = conflictingMethods.Select(m => (m.ContainingType, m.ContainingType.TypeArguments)).ToList();
        //                        if (implementedMethod.ContainingType.TypeKind == TypeKind.Interface && implementedMethod.ContainingType.TypeArguments.Any() && implementedMethod.ContainingType.TypeArguments.All(a => a is INamedTypeSymbol))
        //                        {
        //                            CurrentTypeWriter.WriteLine(node, $"let $ts = arguments[arguments.length-1];", true);
        //                            foreach (var method in conflictingMethods.Except([implementedMethod]))
        //                            {
        //                                discriminatedInterfaceMethodImplemented.Add(method);
        //                                var ifs = string.Join(" && ", method.ContainingType.TypeArguments.Select((t, i) => $"$ts[{i}] === {t.ComputeOutputTypeName(_global)}"));
        //                                CurrentTypeWriter.WriteLine(node, $"//Merged and discriminated with method implemetation for {method}", true);
        //                                CurrentTypeWriter.WriteLine(node, $"if ({ifs})", true);
        //                                var implementation = methodSymbol.ContainingType.FindImplementationForInterfaceMember(method)!;
        //                                var implementationMetadata = _global.GetRequiredMetadata(implementation);
        //                                if (implementation.IsStaticCallConvention(_global))
        //                                {
        //                                    CurrentTypeWriter.WriteLine(node, $"    return {implementationMetadata.InvocationName}.apply(this, arguments);", true);
        //                                }
        //                                else
        //                                {
        //                                    CurrentTypeWriter.WriteLine(node, $"    return {(!methodSymbol.IsStatic ? "this." : "")}{implementationMetadata.InvocationName}(...arguments);", true);
        //                                }
        //                            }
        //                        }
        //                    }
        //                    if (methodSymbol.IsStaticCallConvention(_global))
        //                    {
        //                        CurrentTypeWriter.WriteLine(node, $"{(methodSymbol.ReceiverType != null ? "return " : "")}{methodInvocationName}.apply(this, arguments);", true);
        //                    }
        //                    else
        //                    {
        //                        CurrentTypeWriter.WriteLine(node, $"{(methodSymbol.ReceiverType != null ? "return " : "")}{(!methodSymbol.IsStatic ? "this." : "")}{methodInvocationName}(...arguments);", true);
        //                    }
        //                    CurrentTypeWriter.WriteLine(node, $"}}", true);
        //                    if (methodSymbol.IsStaticCallConvention(_global) && !methodSymbol.IsStatic)
        //                    {
        //                        CurrentTypeWriter.WriteLine(node, $"//Static convention instance redirect", true);
        //                        CurrentTypeWriter.Write(node, $"{symbol.OverloadName}", true);
        //                        CurrentTypeWriter.Write(node, $"(", false);
        //                        //No need to write method argument explicitly since we use spread operator on arguments
        //                        //WriteMethodDeclarationParameters(node, parameters?.Parameters ?? default);
        //                        CurrentTypeWriter.WriteLine(node, $")", false);
        //                        CurrentTypeWriter.WriteLine(node, $"{{", true);
        //                        CurrentTypeWriter.WriteLine(node, $"{(methodSymbol.ReceiverType != null ? "return " : "")}{methodInvocationName}.apply(this, arguments);", true);
        //                        //CurrentTypeWriter.Write(node, $"return {symbol.InvocationName}.apply(this, ...arguments);", true);
        //                        CurrentTypeWriter.WriteLine(node, $"}}", true);
        //                    }
        //                    CloseClosure();
        //                }
        //            }
        //        }
        //    }
        //}
        void WriteMethodBody(BaseMethodDeclarationSyntax node, TypeSyntax? returnType, SeparatedSyntaxList<TypeParameterSyntax>? typeParameters, IEnumerable<ParameterSyntax> parameters, Action? writePrologue = null, Action? writeEpilogue = null)
        {
            if (node.ExpressionBody == null && node.Body == null)
                throw new InvalidOperationException("Cannot write method without a body");
            //TODO: operator methods are not getting their symbols
            var methodSymbol = _global.TryGetSymbol(node, this/*, out _, out _*/);
            // If this method has a covariant parameter,
            // eg
            // private interface IConsumer<in T> { void Consume(T item); }
            // void Consume(object item){} 
            // private class ObjectConsumer : IConsumer<object> { public void Consume(object item){} }
            // Variance: IConsumer<object> can be used as IConsumer<string>
            // IConsumer<string> contravariant = new ObjectConsumer();
            // contravariant.Consume("world");
            // Calling consume with js string will break things,
            // unless the string is boxed as an object,
            // so we need to box the parameter in this case.
            // We can detect this by checking if the parameter is contravariant and the argument type is a value type,
            // then we know we need to box it.
            // We can also check if the parameter has a custom attribute [Box] to force boxing even for reference types, or disable boxing for value types.
            //if (methodSymbol is IMethodSymbol method1)
            //{
            //    foreach (var p in method1.Parameters)
            //    {
            //        if (p.Var)
            //    }
            //}
            if (node.ExpressionBody != null || writePrologue != null)
            {
                CurrentTypeWriter.WriteLine(node, "{", true);
            }
            writePrologue?.Invoke();
            bool needClosurePop = false;
            if (CurrentClosure.Syntax != node)
            {
                OpenClosure(node);
                needClosurePop = true;
            }
            DefineParametersInClosure(parameters, methodSymbol);
            if (typeParameters != null)
            {
                int ixx = 0;
                foreach (var tp in typeParameters)
                {
                    if (methodSymbol is IMethodSymbol method)
                    {
                        CurrentClosure.DefineIdentifierType(tp.Identifier.ValueText, CodeSymbol.From(method.TypeParameters.ElementAt(ixx)));
                    }
                    else
                    {
                        CurrentClosure.DefineIdentifierType(tp.Identifier.ValueText, CodeSymbol.From(tp, SymbolKind.Parameter));
                    }
                    ixx++;
                }
            }
            TryWrapInYieldingGetEnumerable(
                node,
                (returnType as GenericNameSyntax)?.TypeArgumentList.Arguments,
                [node.Body ?? (SyntaxNode)node.ExpressionBody!],
                isAsync: node.Modifiers.IsAsync()
            //node.ChildNodes().Where(e => !e.IsKind(SyntaxKind.ThisConstructorInitializer) && !e.IsKind(SyntaxKind.ThisConstructorInitializer) && !e.IsKind(SyntaxKind.BaseConstructorInitializer) && !e.IsKind(SyntaxKind.IdentifierName)/* is not IdentifierNameSyntax*/ && !e.IsKind(SyntaxKind.ParameterList) /*is not ParameterListSyntax */&& !e.IsKind(SyntaxKind.TypeParameterConstraintClause)/* is not TypeParameterConstraintClauseSyntax*/ && !e.IsKind(SyntaxKind.ExplicitInterfaceSpecifier) /*is not ExplicitInterfaceSpecifierSyntax*/).Except(returnType != null ? [returnType] : [])
            );
            if (needClosurePop)
            {
                CloseClosure();
            }
            writeEpilogue?.Invoke();
            if (node.ExpressionBody != null || writePrologue != null)
            {
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
        }

        string ConditionalInvokeStart(CSharpSyntaxNode node, CodeNode? lhsExpression)
        {
            var i = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
            string label = $"$loc{i}";

            CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.$exp(function()");
            CurrentTypeWriter.WriteLine(node, $"{{", true);
            CurrentTypeWriter.Write(node, $"const {label} = ", true);
            VisitNode(lhsExpression);
            CurrentTypeWriter.WriteLine(node, $";");
            CurrentTypeWriter.Write(node, $"return ", true);

            //Writer.Write(node, $"(const {label} = ");
            //Visit(lhsExpression);
            //Writer.Write(node, $", ");
            return label;
        }

        void ConditionalInvokeEnd(CSharpSyntaxNode node)
        {
            CurrentTypeWriter.WriteLine(node, $";");
            //Writer.WriteLine(node, $"return $ret;", true);
            CurrentTypeWriter.Write(node, $"}})", true);
            //Writer.Write(node, " || null)");
        }

        /// <summary>
        /// Check for an instace call where lhs is a TypeParameter and rhs is an instance member on an interface/base class implemented by lhs
        /// </summary>
        /// <param name="lhsSymbol"></param>
        /// <param name="rhsSymbol"></param>
        /// <returns></returns>
        bool IsGenericDispatch(ITypeSymbol lhsSymbol, ISymbol rhsSymbol)
        {
            return lhsSymbol.TypeKind == TypeKind.TypeParameter &&
                !rhsSymbol.IsStatic;//&&
                                    //rhsSymbol.ContainingType.TypeKind == TypeKind.Interface;
        }

        public void WriteMethodInvocation(
            CSharpSyntaxNode node,
            IMethodSymbol? method,
            ExpressionSyntax? rhsExpression,
            IEnumerable<CodeNode>? parameterArgs,
            CodeNode? lhsExpression,
            ISymbol? lhsSymbol,
            TypeArgumentListSyntax? explicitGenericArgs = null,
            bool conditionallyInvoke = false,
            CodeNode? suffixArguments = null,
            MethodOverloadResult overloadResult = default)
        {
            Dictionary<object, object?>? conditonalConstructorArgs = null;
            var hasConditional = method != null && _global.HasAttribute(method, typeof(ConditionalAttribute).FullName!, this, false, out conditonalConstructorArgs);
            if (hasConditional)
            {
                var prop = conditonalConstructorArgs![0]!.ToString()!;
                if (!prop.Equals(_global.Evaluate("Configuration"), StringComparison.InvariantCultureIgnoreCase))
                {
                    return /*true*/;
                }
            }
            //if (method?.Name == "TrimFallback")
            //{

            //}
            bool isLocalMethod = method?.MethodKind == MethodKind.LocalFunction;
            bool isStaticCallConvention = method?.IsStaticCallConvention(_global) ?? false;
            bool prefixHasGenericArguments = !Constants.GenericMethodAsFactory &&
                method != null &&
                method.IsGenericMethod &&
                !_global.HasAttribute(method, typeof(IgnoreGenericAttribute).FullName, this, false, out _);
            var lhsType = lhsSymbol != null ? _global.GetTypeSymbol(lhsSymbol) : null;
            bool calledAsExtensionMethod = lhsType != null && method != null && !lhsType.Equals(method.ContainingType, SymbolEqualityComparer.Default);
            CodeNode? prefixArgument = prefixHasGenericArguments || (method != null && !method.IsStatic) ? new CodeNode(() =>
            {
                int ix = 0;
                if (prefixHasGenericArguments)
                {
                    foreach (var type in method!.TypeArguments)
                    {
                        if (ix > 0)
                            CurrentTypeWriter.Write(node, ", ", false);
                        var typeName = type.ComputeOutputTypeName(_global);
                        CurrentTypeWriter.Write(node, typeName);
                        ix++;
                    }
                }
                if (method != null)
                {
                    if (!method.IsStatic)
                    {
                        ConditionalAccessExpressionSyntax? ce = null;
                        if ((ce = node.FindClosestParent<ConditionalAccessExpressionSyntax>(isCandidate: (e) => rhsExpression.IsKind(SyntaxKind.MemberBindingExpression) || e.Expression == node || e.WhenNotNull == node)) != null &&
                            ConditionalAccessUseIfNotNull(ce, out _) &&
                            (ce.WhenNotNull == node || rhsExpression.IsKind(SyntaxKind.MemberBindingExpression)))
                        {
                            if (ix > 0)
                                CurrentTypeWriter.Write(node, ", ", false);
                            CurrentTypeWriter.Write(node, Constants.IfNotNullParameterName);
                        }
                        /*static convention call*/
                        else if (lhsExpression == null)
                        {
                            if (SymbolIsThisTypeMember(method, out _))
                            {
                                if (ix > 0)
                                    CurrentTypeWriter.Write(node, ", ", false);
                                CurrentTypeWriter.Write(node, "this");
                            }
                        }
                        else
                        {
                            if (isStaticCallConvention && lhsExpression.IsT0 && lhsExpression.AsT0.IsKind(SyntaxKind.BaseExpression))
                            {
                                if (ix > 0)
                                    CurrentTypeWriter.Write(node, ", ", false);
                                CurrentTypeWriter.Write(node, "this"); //cannot pass super to method call
                            }
                            else if (isStaticCallConvention)
                            {
                                if (ix > 0)
                                    CurrentTypeWriter.Write(node, ", ", false);
                                VisitNode(lhsExpression);
                            }
                        }
                    }
                    else if (method.IsExtensionMethod && lhsExpression != null && calledAsExtensionMethod)
                    {
                        if (ix > 0)
                            CurrentTypeWriter.Write(node, ", ", false);
                        VisitNode(lhsExpression);
                    }
                }
            })
            : null;
            if (method != null)
            {
                //var parameterNames = method.Parameters.Select(p => p.Name).ToList();
                var templateAttribute = method.OriginalDefinition.GetTemplateAttribute(_global);
                if (templateAttribute != null)
                {
                    WriteMethodTemplate(node, lhsExpression, lhsSymbol, conditionallyInvoke, method, parameterArgs, templateAttribute, overloadResult, suffixArguments);
                    return /*true*/;
                }
                if (method.IsExtensionMethod)
                {
                    if (calledAsExtensionMethod)
                    {
                        EnsureImported(method.ContainingType);
                        var mlhsLabel = conditionallyInvoke ? ConditionalInvokeStart(node, lhsExpression) : "";
                        var meta = _global.GetRequiredMetadata(method);
                        //Writer.Write(node, meta.InvocationName ?? method.ContainingType.Name);
                        //Writer.Write(node, ".");
                        CurrentTypeWriter.Write(node, meta.InvocationName ?? method.Name);
                        //if (method.Arity > 0)
                        //{
                        //    WriteMethodGenericArgument(node, method, overloadResult.GenericTypeSubstitutions);
                        //}
                        if (prefixArgument == null)
                            prefixArgument = lhsExpression;
                        WriteMethodInvocationParameter(
                            node,
                            method,
                            explicitGenericArgs,
                            parameterArgs,
                            prefixArguments: prefixArgument,
                            suffixArguments: suffixArguments,
                            overloadResult: overloadResult,
                            calledAsExtensionMethod: true);
                        if (conditionallyInvoke)
                            ConditionalInvokeEnd(node);
                        return /*true*/;
                    }
                }
            }
            var methodMetadata = method != null &&
                method.ContainingSymbol is not IMethodSymbol/*Currently unable to read local method symbol when collecting symbols*/ &&
                !_global.IsAnonymousFunction(method.ContainingSymbol) ?
                _global.GetRequiredMetadata(method) : null;
            //var typeSymbol = (INamedTypeSymbol)_global.GetTypeSymbol(CurrentType, this);
            //var isBootClass = _global.IsBootClass(typeSymbol);
            if ((method?.IsStatic ?? false) ||
                isStaticCallConvention)// ||
                                       //'C'.GetHashCode() => if the lhs is static convention, even if the method isnt, we use static convention
                                       //(lhsSymbol?.IsStaticCallConvention() ?? false))
            {
                if (lhsSymbol is ITypeParameterSymbol ttp)
                {
                    CurrentTypeWriter.Write(node, ttp.Name);
                    CurrentTypeWriter.Write(node, ".");
                    CurrentTypeWriter.Write(node, methodMetadata?.OverloadName ?? method!.Name);
                }
                else
                {
                    CurrentTypeWriter.Write(node, methodMetadata?.InvocationName /*?? methodMetadata?.SimpleName*/ ?? method!.Name);
                    if (!method!.IsStatic) //static convention call
                    {
                        CurrentTypeWriter.Write(node, ".call");
                    }
                }
                void WriteGenericArguments()
                {
                    if (method?.Arity > 0)
                    {
                        WriteMethodGenericArgument(node, method, overloadResult.GenericTypeSubstitutions);
                    }
                    //the generic method is called without providing generic argument, use the infered arguments in the genericTypesSubstitutions
                    else if (method?.IsGenericMethod == true && !node.ChildNodes().Any(e => e is GenericNameSyntax))
                    {
                        CurrentTypeWriter.Write(node, "(", false);
                        int ix = 0;
                        foreach (var tp in method.TypeParameters)
                        {
                            if (ix > 0)
                                CurrentTypeWriter.Write(node, ", ", false);
                            var type = overloadResult.GenericTypeSubstitutions?.GetValueOrDefault(tp);
                            var typeName = type != null ? _global.GetMetadata(type)?.FullName : null;
                            CurrentTypeWriter.Write(node, typeName ?? tp.Name);
                            ix++;
                        }
                        CurrentTypeWriter.Write(node, ")", false);
                    }
                }
                if (Constants.GenericMethodAsFactory && methodMetadata == null)
                {
                    WriteGenericArguments();
                }
                WriteMethodInvocationParameter(
                    node,
                    method,
                    explicitGenericArgs,
                    parameterArgs,
                    prefixArguments: prefixArgument,
                    suffixArguments: suffixArguments,
                    overloadResult: overloadResult);
                return /*true*/;
            }
            else
            {
                bool writeParameterBrackets = true;
                //If we are calling a method on an interface/class through a generic type, we really cant be sure the method exist directly on the target
                //For example calling IEquatable.Equals on a T=char will fail as char is a js primitive type
                //We can actually dispatch the call statically, through the generic type T we have
                var dlhsExpression = lhsExpression != null ? (lhsExpression.IsT0 ? lhsExpression.AsT0 : null) : null;
                var dlhsSymbol = lhsSymbol;
                if (dlhsExpression == null)
                {
                    if (node.Parent.IsKind(SyntaxKind.ConditionalAccessExpression))
                    {
                        dlhsExpression = ((ConditionalAccessExpressionSyntax)node.Parent).Expression;
                        dlhsSymbol = _global.GetSymbol(dlhsExpression, this);
                    }
                }
                if (dlhsExpression != null &&
                    rhsExpression != null &&
                    dlhsSymbol != null &&
                    method != null &&
                    IsGenericDispatch(_global.GetTypeSymbol(dlhsSymbol), method))
                {
                    //$.$dsp(lhs, T, (t)=>t.Method)()
                    CurrentTypeWriter.Write(node, _global.GlobalName);
                    CurrentTypeWriter.Write(node, ".");
                    CurrentTypeWriter.Write(node, Constants.Dispatch);
                    CurrentTypeWriter.Write(node, "(");
                    VisitNode(dlhsExpression);
                    CurrentTypeWriter.Write(node, ", ");
                    CurrentTypeWriter.Write(node, _global.GetTypeSymbol(dlhsSymbol).Name);
                    CurrentTypeWriter.Write(node, ", ");
                    CurrentTypeWriter.Write(node, "($t) => ");
                    CurrentTypeWriter.Write(node, "$t.");
                    if (method?.MethodKind != MethodKind.DelegateInvoke && methodMetadata?.InvocationName != null)
                    {
                        CurrentTypeWriter.Write(node, methodMetadata.InvocationName);
                    }
                    else
                    {
                        var exp = rhsExpression;
                        if (exp.IsKind(SyntaxKind.MemberBindingExpression))
                            exp = ((MemberBindingExpressionSyntax)exp).Name;
                        Visit(exp);
                    }
                    CurrentTypeWriter.Write(node, ", ");
                    //CurrentTypeWriter.Write(node, ")");
                    writeParameterBrackets = false;
                }
                else
                {
                    if (lhsExpression != null)
                    {
                        //calling object method on js primitive type,
                        //We need to box it first to be able to call the method
                        //Or we call the method statically
                        if (lhsSymbol != null && _global.GetTypeSymbol(lhsSymbol).IsJsPrimitive())
                        {
                            if (method != null)
                            {
                                var typeMetadata = _global.GetRequiredMetadata(method.ContainingType);
                                CurrentTypeWriter.Write(node, typeMetadata.InvocationName!);
                                CurrentTypeWriter.Write(node, ".");
                                CurrentTypeWriter.Write(node, methodMetadata?.InvocationName ?? method.Name);
                                CurrentTypeWriter.Write(node, ".call(");
                                VisitNode(lhsExpression);
                                CurrentTypeWriter.Write(node, ", ");

                                //CurrentTypeWriter.Write(node, ")");
                                writeParameterBrackets = false;
                            }
                            else
                            {
                                CurrentTypeWriter.Write(node, _global.GlobalName);
                                CurrentTypeWriter.Write(node, ".");
                                CurrentTypeWriter.Write(node, Constants.BoxName);
                                CurrentTypeWriter.Write(node, "(");
                                VisitNode(lhsExpression);
                                CurrentTypeWriter.Write(node, ", ");
                                CurrentTypeWriter.Write(node, _global.GetTypeSymbol(lhsSymbol).ComputeOutputTypeName(_global));
                                CurrentTypeWriter.Write(node, ")");
                                CurrentTypeWriter.Write(node, ".");
                            }
                        }
                        else
                        {
                            VisitNode(lhsExpression);
                            CurrentTypeWriter.Write(node, ".");
                        }
                    }
                    else
                    {
                        if (SymbolIsThisTypeMember(method, out _))
                        {
                            ConditionalAccessExpressionSyntax? ce = null;
                            if ((ce = node.FindClosestParent<ConditionalAccessExpressionSyntax>(isCandidate: (e) => e.Expression == node || e.WhenNotNull == node)) == null)
                                CurrentTypeWriter.Write(node, "this.");
                            else if (ce.Expression == node)
                                CurrentTypeWriter.Write(node, "this.");
                            else if (ce.WhenNotNull == node)
                                CurrentTypeWriter.Write(node, ".");
                        }
                        else if (method != null &&
                            !method.IsStatic &&
                            method.MethodKind != MethodKind.LocalFunction &&
                            method.MethodKind != MethodKind.DelegateInvoke)
                        {
                            //ConditionalAccessExpressionSyntax? ce = null;
                            //if ((ce = node.FindClosestParent<ConditionalAccessExpressionSyntax>(isCandidate: (e) => e.Expression == node || e.WhenNotNull == node)) == null)
                            //    CurrentTypeWriter.Write(node, "this.");
                            //else
                            CurrentTypeWriter.Write(node, ".");
                        }
                    }
                    if (method?.MethodKind != MethodKind.DelegateInvoke && methodMetadata?.InvocationName != null)
                    {
                        CurrentTypeWriter.Write(node, methodMetadata.InvocationName);
                    }
                    else
                    {
                        Visit(rhsExpression);
                    }
                    if (/*!isBootClass && */method?.MethodKind == MethodKind.DelegateInvoke && lhsExpression == null)
                    {
                        var nodeSymbol = node is InvocationExpressionSyntax iv ? _global.GetSymbol(iv.Expression, this) : null;
                        if ((nodeSymbol != null && _global.IsNativeFunction(nodeSymbol)) || _global.IsNativeFunction(method.ContainingType))
                        {
                        }
                        else
                        {
                            bool isExplicitInvoke = rhsExpression != null &&
                                rhsExpression.IsKind(SyntaxKind.MemberBindingExpression) &&
                                ((MemberBindingExpressionSyntax)rhsExpression).Name.Identifier.ValueText == "Invoke";
                            if (rhsExpression != null &&
                                rhsExpression.IsKind(SyntaxKind.SimpleMemberAccessExpression) &&
                                ((MemberAccessExpressionSyntax)rhsExpression).Name.Identifier.ValueText == "Invoke")
                            {
                                isExplicitInvoke = true;
                            }
                            if (!isExplicitInvoke)
                            {
                                CurrentTypeWriter.Write(node, ".Invoke");
                            }
                        }
                    }
                    else if (isLocalMethod)
                    {
                        CurrentTypeWriter.Write(node, ".call");
                    }
                }
                //Handle implemented interface members call that may conflict in name:
                //eg if a class implement both IComparer<string> and IComparer<char>
                //the compare implementation methods for both implementation will conflictly be named System$Collections$Generic$IComparer$$$Compare
                //even though one will receive a string and the other a char. These two methods are merged into one.
                //We need to discriminate the one we intend to call by passing the type Ts as a last argument to the merged implementation
                //If the called class doesnt implement more than one of these discriminated interface, it simply ignore the T parameter
                if (method != null && method.ContainingType.TypeKind == TypeKind.Interface && method.ContainingType.TypeArguments.Any()/* && method.ContainingType.TypeArguments.All(a => a is INamedTypeSymbol)*/)
                {
                    var existingSuffix = suffixArguments;
                    suffixArguments = (Action)(() =>
                    {
                        int ix = 0;
                        if (existingSuffix != null)
                        {
                            WriteAdditionalArgument(node, existingSuffix, ref ix);
                            CurrentTypeWriter.Write(node, ", ");
                        }
                        ix = 0;
                        CurrentTypeWriter.Write(node, "[");
                        foreach (var t in method.ContainingType.TypeArguments)
                        {
                            if (ix > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            CurrentTypeWriter.Write(node, t.ComputeOutputTypeName(_global));
                            ix++;
                        }
                        CurrentTypeWriter.Write(node, "]");
                    });
                }
                prefixArgument = isLocalMethod || prefixHasGenericArguments ? new CodeNode(() =>
                {
                    int ix = 0;
                    if (isLocalMethod)
                    {
                        CurrentTypeWriter.Write(node, "this", false);
                        ix++;
                    }
                    foreach (var type in method!.TypeArguments)
                    {
                        if (ix > 0)
                            CurrentTypeWriter.Write(node, ", ", false);
                        var typeName = type.ComputeOutputTypeName(_global);
                        CurrentTypeWriter.Write(node, typeName);
                        ix++;
                    }
                }) : null;
                WriteMethodInvocationParameter(node, method, explicitGenericArgs, parameterArgs, prefixArguments: prefixArgument, suffixArguments: suffixArguments, overloadResult: overloadResult, writeBrackets: writeParameterBrackets);
                if (!writeParameterBrackets)
                {
                    CurrentTypeWriter.Write(node, ")");
                }
                return /*true*/;
            }
        }

        public void WriteMethodInvocation(
              CSharpSyntaxNode node,
              string methodFullName,
              Func<IMethodSymbol, bool>? methodFilter = null,
              ITypeSymbol[]? classGenericTypes = null,
              ITypeSymbol[]? methodGenericTypes = null,
              CodeNode? lhsExpression = null,
              IEnumerable<CodeNode>? arguments = null)
        {
            var i = methodFullName.LastIndexOf('.');
            var className = methodFullName.Substring(0, i);
            var methodName = methodFullName.Substring(i + 1);
            var @class = (ITypeSymbol)_global.GetSymbol(className, this);
            if (classGenericTypes != null)
            {
                @class = ((INamedTypeSymbol)@class).Construct(classGenericTypes);
            }
            var method = (IMethodSymbol)@class.GetMembers(methodName).Where(e => e is IMethodSymbol m && (!(methodGenericTypes?.Any() ?? false) || methodGenericTypes.Length == m.TypeArguments.Length) && (methodFilter?.Invoke(m) ?? true)).Single();
            if (methodGenericTypes != null)
            {
                method = method.Construct(methodGenericTypes);
            }
            WriteMethodInvocation(node,
                method,
                null,
                arguments,
                lhsExpression, @class, null, false);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var parameterArgs = (ArgumentListSyntax?)node.ChildNodes().FirstOrDefault(e => e.IsKind(SyntaxKind.ArgumentList)/* is ArgumentListSyntax*/);
            if (node.Expression is IdentifierNameSyntax nid)
            {
                if (nid.Identifier.ValueText == "nameof")
                {
                    var argName = node.ArgumentList.Arguments.FirstOrDefault();
                    if (argName == null)
                        throw new InvalidOperationException("nameof expects one argument");
                    CurrentTypeWriter.Write(node, "\"");
                    CurrentTypeWriter.Write(node, argName.ToString().Split('.').Last().Split('<').First());
                    CurrentTypeWriter.Write(node, "\"");
                    return;
                }
                //else
                //{
                //    var target = GetIdentifierTypeInScope(nid.Identifier.ValueText);
                //    if (target.TypeSyntaxOrSymbol != null)
                //    {
                //        IMethodSymbol? mmethod = null;
                //        if (target.TypeSyntaxOrSymbol is IMethodSymbol meth)
                //            mmethod = meth;
                //        else if (target.TypeSyntaxOrSymbol is MemberSymbolOverload overloads)
                //            mmethod = overloads.ResolveMethod(this, (TypeArgumentListSyntax?)node.ChildNodes().FirstOrDefault(e => e.IsKind(SyntaxKind.TypeArgumentList)), node.ArgumentList, out _);
                //        //var meta = mmethod != null ? global.ReversedSymbols[mmethod.OriginalDefinition] : null;
                //        WriteMethodInvocation(node, mmethod, node.Expression, null, parameterArgs?.Arguments, null, null, false);
                //        //Writer.Write(node, meta?.InvocationName ?? nid.Identifier.ValueText);
                //        //WriteMethodInvocationParameter(node, mmethod, null, node.ArgumentList);
                //        return;
                //    }
                //}
            }
            var children = node.ChildNodes().Where(e => !e.IsKind(SyntaxKind.ArgumentList)/* is not ArgumentListSyntax*/);
            CodeSymbol lhsType = default;
            TypeArgumentListSyntax? explicitGenericArgs = null;
            ISymbol? lhsSymbol = null;
            string? rhsName = null;
            ExpressionSyntax? lhsExpression = null;
            ExpressionSyntax? rhsExpression = null;
            ISymbol? rhsSymbol = null;
            bool conditionallyInvoke = false;
            //if (conditionally && node.Parent is ConditionalAccessExpressionSyntax cd) //conditional expression was skipped for this 
            //{
            //    var ma = node.Expression as MemberAccessExpressionSyntax;
            //    var mb = node.Expression as MemberBindingExpressionSyntax;
            //    lhsExpression = cd.Expression;
            //    rhsExpression = ma?.Name ?? mb.Name;
            //    rhsName = (ma?.Name ?? mb.Name).Identifier.ValueText;
            //    if ((ma?.Name ?? mb.Name) is GenericNameSyntax gnn)
            //    {
            //        explicitGenericArgs = gnn.TypeArgumentList;
            //    }
            //    lhsType = GetExpressionReturnType(lhsExpression);
            //    if (lhsType.TypeSyntaxOrSymbol != null)
            //    {
            //        lhsSymbol = GetTypeSymbol(lhsType, out _);
            //    }
            //    conditionallyInvoke = true;
            //}
            //else
            var lhsTypeSymbol = lhsSymbol != null ? _global.GetTypeSymbol(lhsSymbol) : null;
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                lhsExpression = memberAccess.Expression;
                rhsExpression = memberAccess.Name;
                rhsName = memberAccess.Name.Identifier.ValueText;
                if (memberAccess.Name is GenericNameSyntax gn)
                {
                    explicitGenericArgs = gn.TypeArgumentList;
                }
                //CodeType? parentType = null;
                //if (node.Parent is ConditionalAccessExpressionSyntax cd)
                //{
                //    parentType = GetExpressionReturnType(cd.Expression);
                //    conditionallyInvoke = true;
                //}
                lhsType = GetExpressionReturnSymbol(lhsExpression);
                if (lhsType.TypeSyntaxOrSymbol != null)
                {
                    lhsSymbol = _global.ResolveSymbol(lhsType, this/*, out _, out _*/);
                }
                if (lhsTypeSymbol != null)
                {
                    var members = lhsTypeSymbol.GetMembers(rhsName, _global).ToList();
                    if (members.Count() == 1)
                        rhsSymbol = members.Single();
                }
            }
            else if (node.Expression is IdentifierNameSyntax id)
            {
                var boundedTo = GetExpressionBoundTarget(id);
                if (boundedTo.TypeSyntaxOrSymbol is IMethodSymbol smethod)
                {
                    lhsSymbol = smethod!.ContainingSymbol;
                    lhsType = CodeSymbol.From(lhsSymbol);
                }
                else
                {
                    var target = GetIdentifierTypeInScope(id.Identifier.ValueText);
                    if (target.TypeSyntaxOrSymbol != null)
                    {
                        if (target.TypeSyntaxOrSymbol is MemberSymbolOverload overload)
                        {
                            var symbol = overload.ResolveMethod(this, explicitGenericArgs, node.ArgumentList, out _);
                            if (symbol != null)
                            {
                                lhsSymbol = symbol!.ContainingSymbol;
                                lhsType = CodeSymbol.From(lhsSymbol);
                            }
                        }
                        else
                        {
                            var symbol = _global.ResolveSymbol(target, this/*, out var declaringType, out var declaringKind*/);
                            if (symbol is IMethodSymbol)
                            {
                                lhsSymbol = symbol!.ContainingSymbol;
                                lhsType = CodeSymbol.From(lhsSymbol);
                            }
                        }
                    }
                    else
                    {
                        var targetType = node.FindClosestParent<BaseTypeDeclarationSyntax>() ?? throw new InvalidOperationException("Invocation must happen within the scope of a class, albeit static");
                        lhsSymbol = _global.GetTypeSymbol(targetType, this);
                        lhsType = CodeSymbol.From(targetType);
                    }
                }
                rhsExpression = id;
                rhsName = id.Identifier.ValueText;
            }
            else if (node.Expression is GenericNameSyntax gn)
            {
                var targetType = node.FindClosestParent<BaseTypeDeclarationSyntax>() ?? throw new InvalidOperationException("Invocation must happen within the scope of a class, albeit static");
                lhsSymbol = _global.GetTypeSymbol(targetType, this);
                lhsType = CodeSymbol.From(targetType);
                rhsExpression = gn;
                rhsName = gn.Identifier.ValueText;
                explicitGenericArgs = gn.TypeArgumentList;
            }
            else
            {
                rhsExpression = node.Expression;
            }
            IMethodSymbol? method = _global.TryGetSymbol(node, this) as IMethodSymbol;
            if ((method?.IsExtensionMethod ?? false) && method.ReducedFrom != null)
            {
                var mmethod = method.ReducedFrom;
                if (method.IsGenericMethod)
                {
                    mmethod = mmethod!.Construct(method.TypeArguments.ToArray());
                }
                method = mmethod;
            }
            if (method != null)
            {
                int ix = 0;
                foreach (var parameter in method.Parameters)
                {
                    if (parameter.RefKind == RefKind.Out || parameter.RefKind == RefKind.Ref)
                    {
                        var matchingArgument = parameterArgs?.Arguments.FirstOrDefault(e => e.NameColon?.Name.Identifier.ValueText == parameter.Name) ?? parameterArgs?.Arguments.ElementAtOrDefault(ix);
                        if (matchingArgument != null)
                        {
                            if (matchingArgument.Expression is DeclarationExpressionSyntax dv && dv.Designation is SingleVariableDesignationSyntax sv)
                            {
                                var localSymbol = _global.TryGetSymbol(sv, this/*, out _, out _*/);
                                if (localSymbol != null)
                                    CurrentClosure.DefineIdentifierType(sv.Identifier.ValueText, CodeSymbol.From(localSymbol));
                                else
                                    CurrentClosure.DefineIdentifierType(sv.Identifier.ValueText, CodeSymbol.From(parameter.Type));
                            }
                            //else if (matchingArgument.Expression is IdentifierNameSyntax id)
                            //{
                            //    var localSymbol = _global.TryGetTypeSymbol(id, this, out _, out _);
                            //    if (localSymbol != null)
                            //        CurrentClosure.DefineIdentifierType(id.Identifier.ValueText, CodeType.From(localSymbol));
                            //    else
                            //        CurrentClosure.DefineIdentifierType(id.Identifier.ValueText, CodeType.From(parameter.Type));
                            //}
                        }
                        //CurrentClosure.DefineIdentifierType(pv.Value.OutName, p.Type);
                    }
                    ix++;
                }
            }
            MethodOverloadResult overloadResult = default;
            if (lhsSymbol != null && rhsExpression != null && rhsName != null)
            {
                if (lhsType.TypeSyntaxOrSymbol is MemberSymbolOverload overload)
                {
                    method = method ?? overload.ResolveMethod(this, explicitGenericArgs, parameterArgs, out overloadResult);
                }
                else
                {
                    method = method ?? rhsSymbol as IMethodSymbol ?? (lhsTypeSymbol != null ? GetBestOverloadMethod(lhsTypeSymbol!, rhsName, explicitGenericArgs, parameterArgs?.Arguments, null, out overloadResult) : null);
                }
                if (method != null)
                {
                    //if (overloadResult.GenericTypeSubstitutions != null)
                    //method = (IMethodSymbol)method.SubstituteGenericType(overloadResult.GenericTypeSubstitutions, global);
                    if (overloadResult.ParameterValueSubstitutions != null)
                    {
                        foreach (var pv in overloadResult.ParameterValueSubstitutions)
                        {
                            if (pv.Value.OutName != null && pv.Value.OutType.TypeSyntaxOrSymbol != null)
                            {
                                CurrentClosure.DefineIdentifierType(pv.Value.OutName, pv.Value.OutType);
                            }
                        }
                    }
                }
            }
            WriteMethodInvocation(node, method, rhsExpression, parameterArgs?.Arguments.Select(a => new CodeNode(a)), lhsExpression, lhsSymbol, explicitGenericArgs, conditionallyInvoke, null, overloadResult);
        }

        bool ShouldExportMethod(CSharpSyntaxNode node)
        {
            if (node is BaseMethodDeclarationSyntax bm && bm.Modifiers.IsExtern())
                return false;
            //var symbol = _global.TryGetTypeSymbol(node, this/*, out _, out _*/);
            //if (symbol is IMethodSymbol methodSymbol)
            //{
            //    ITypeSymbol[] types = [methodSymbol.ReturnType, .. methodSymbol.Parameters.Select(p => p.Type)];
            //    if (types.Any(e => e.IsType("System.Runtime.Intrinsics.Vector512<>", matchGenerics: true) || e.IsType("System.Runtime.Intrinsics.Vector256<>", matchGenerics: true) || e.IsType("System.Runtime.Intrinsics.Vector128<>", matchGenerics: true) || e.IsType("System.Runtime.Intrinsics.Vector64<>", matchGenerics: true)))
            //    {
            //        return false;
            //    }
            //}
            return true;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!ShouldExportMethod(node))
                return;
            if (node.Modifiers.IsPartial() && node.Body == null && node.ExpressionBody == null)
                return;
            //if (node.Body == null && node.ExpressionBody == null)
            //    return;
            var methodSymbol = (IMethodSymbol)OpenClosure(node);// _global.TryGetTypeSymbol(node, this, out _, out _);
            if (_global.LinkTrimOutMethod(methodSymbol))
                return;
            //if (methodSymbol.Name== "FilterHelper")
            //{
            //}
            bool external = _global.HasAttribute(methodSymbol, typeof(TemplateAttribute).FullName!, this, false, out _);
            bool export = _global.ShouldExportType(methodSymbol, this);
            if (export && !external && !node.Modifiers.IsAbstract() && (node.Body != null || node.ExpressionBody != null))
            {
                if (methodSymbol.Arity > 0)
                {
                    foreach (var tp in methodSymbol.TypeParameters)
                    {
                        CurrentClosure.DefineIdentifierType(tp.Name, CodeSymbol.From(tp));
                    }
                }
                //CurrentClosure.DefineIdentifierType($"{methodSymbol.Name}{(methodSymbol.IsGenericMethod ? $"<{string.Join(",", Enumerable.Range(1, methodSymbol.Arity).Select(e => ""))}>" : "")}", CodeType.From(methodSymbol));
                var metadata = _global.GetRequiredMetadata(methodSymbol);
                //closures.Push(new CodeBlockClosure(global, semanticModel, node, methodSymbol));
                EnsureImported(node.ReturnType);
                string? modifier = GetMethodModifier(node, node.Modifiers, node.ReturnType);
                string? smodifier = null;

                if (!methodSymbol.IsStatic && methodSymbol.IsStaticCallConvention(_global))
                {
                    smodifier = "static/*conventional*/ ";
                }

                var methodName = metadata.OverloadName ?? Utilities.ResolveMethodName(node);
                if (node.ExplicitInterfaceSpecifier != null)
                {
                    //var implementingType = GetTypeSymbol(node.ExplicitInterfaceSpecifier.Name);                
                    //methodName = implementingType.ToString()!.Replace(".", "$").Replace("<", "$").Replace(">", "$") + "$" + .;
                }
                //Writer.WriteLine($"{modifier}{node.Identifier.Text.Trim()}({requiredParameters}{(requiredParameters.Length > 0 ? ", " : "")}{optionalParameters})", true);
                CurrentTypeWriter.Write(node, $"{smodifier}{modifier} {methodName}", true);
                bool writeGenerics = node.Arity > 0 &&
                    (methodSymbol == null || !_global.HasAttribute(methodSymbol, typeof(IgnoreGenericAttribute).FullName, this, false, out _)) &&
                    (node.TypeParameterList?.Parameters.Any() ?? false);
                if (Constants.GenericMethodAsFactory && writeGenerics)
                {
                    CurrentTypeWriter.Write(node, " = (", false);
                    int i = 0;
                    foreach (var p in node.TypeParameterList!.Parameters)
                    {
                        if (i > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        CurrentTypeWriter.Write(node, p.Identifier.ValueText);
                        i++;
                    }
                    CurrentTypeWriter.Write(node, $") => {(node.Modifiers.IsAsync() ? "async " : "")}", false);
                }
                CurrentTypeWriter.Write(node, $"(", false);
                if (!Constants.GenericMethodAsFactory && writeGenerics)
                {
                    int i = 0;
                    foreach (var p in node.TypeParameterList!.Parameters)
                    {
                        if (i > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        CurrentTypeWriter.Write(node, p.Identifier.ValueText);
                        i++;
                    }
                    if (node.ParameterList.Parameters.Count > 0)
                        CurrentTypeWriter.Write(node, ", ");
                }
                WriteMethodDeclarationParameters(node, node.ParameterList.Parameters);
                CurrentTypeWriter.Write(node, $")");
                if (Constants.GenericMethodAsFactory && writeGenerics)
                {
                    CurrentTypeWriter.WriteLine(node, $" =>");
                }
                else
                {
                    CurrentTypeWriter.WriteLine(node, $"");
                }
                WriteMethodBody(node, node.ReturnType, node.TypeParameterList?.Parameters, node.ParameterList.Parameters);
                if (methodSymbol != null && !methodSymbol.IsStatic && methodSymbol.IsStaticCallConvention(_global))
                {
                    CurrentTypeWriter.WriteLine(node, $"//Static convention instance redirect", true);
                    CurrentTypeWriter.WriteLine(node, $"{modifier} {methodName}() {{ return {metadata.InvocationName}.apply(this, arguments); }}", true);
                }
            }
            //if (!external)
            //{
            //    //if this method is not an explicit interface implementation, but this method implements an interface method,
            //    //we need to create an alias to this method with the interface name
            //    TryWriteImplementedMethod(node, node.ExplicitInterfaceSpecifier, node.ParameterList, methodSymbol);
            //}
            CloseClosure();
            if (methodSymbol != null && methodSymbol.IsStatic && _global.HasAttribute(methodSymbol, "System.Runtime.CompilerServices.ModuleInitializerAttribute", this, false, out _))
            {
                var metadata = _global.GetRequiredMetadata(methodSymbol);
                _global.RegisterModuleInitializer($"{metadata.InvocationName}();");
            }
            //base.VisitMethodDeclaration(node);
        }


        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            if (!ShouldExportMethod(node))
                return;
            if (node.Modifiers.Any(m => m.ValueText == "extern"))
                return;
            if (node.Body == null && node.ExpressionBody == null)
                return;
            //closures.Push(new CodeBlockClosure(global, semanticModel, node));
            string? modifier = GetMethodModifier(node, node.Modifiers, node.ReturnType);
            var operatorSymbol = OpenClosure(node);// _global.GetTypeSymbol(node, this, out _, out _);
            var metadata = _global.GetRequiredMetadata(operatorSymbol);
            CurrentTypeWriter.Write(node, $"{modifier} {metadata.OverloadName ?? node.OperatorToken.ValueText.ResolveOperatorMethodName(node.ParameterList.Parameters.Count)}(", true);
            WriteMethodDeclarationParameters(node, node.ParameterList.Parameters);
            CurrentTypeWriter.WriteLine(node, $")");
            WriteMethodBody(node, node.ReturnType, null, node.ParameterList.Parameters);
            CloseClosure();
            //closures.Pop();
            //base.VisitOperatorDeclaration(node);
        }

        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            if (!ShouldExportMethod(node))
                return;
            EnsureImported(node.Type);
            var symbol = OpenClosure(node);
            var meta = _global.GetRequiredMetadata(symbol);
            //closures.Push(new CodeBlockClosure(global, semanticModel, node));
            string? modifier = GetMethodModifier(node, node.Modifiers, node.Type);
            //var name = Utilities.ResolveTypeName(node.Type, stripGenericName: true);
            //if (node.Type is GenericNameSyntax g)
            //{
            //    name = $"{name}${g.Arity}";
            //}
            CurrentTypeWriter.Write(node, $"{modifier} {(meta?.OverloadName ?? "op_Implicit")}(", true);
            WriteMethodDeclarationParameters(node, node.ParameterList.Parameters);
            CurrentTypeWriter.WriteLine(node, $")");
            WriteMethodBody(node, node.Type, null, node.ParameterList.Parameters);
            CloseClosure();
            //closures.Pop();
            //base.VisitConversionOperatorDeclaration(node);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            if (!ShouldExportMethod(node))
                return;
            if (node.ExpressionBody == null && node.Body == null)
                return;
            var symbol = _global.TryGetSymbol(node, this/*, out _, out _*/);
            if (symbol != null)
                CurrentClosure.DefineIdentifierType(node.Identifier.ValueText, CodeSymbol.From(symbol));
            OpenClosure(node);
            EnsureImported(node.ReturnType);
            string? modifier = GetMethodModifier(node, node.Modifiers, node.ReturnType, true);
            CurrentTypeWriter.Write(node, $"{modifier} function {node.Identifier.ValueText}(", true);
            WriteMethodDeclarationParameters(node, node.ParameterList.Parameters);
            CurrentTypeWriter.WriteLine(node, $")");
            //WriteMethodBody(node);
            if (node.ExpressionBody != null)
            {
                CurrentTypeWriter.WriteLine(node, "{", true);
            }
            //VisitChildren(node.ChildNodes().Where(e => e is not IdentifierNameSyntax && e is not ParameterListSyntax && e is not TypeParameterConstraintClauseSyntax && e is not ExplicitInterfaceSpecifierSyntax).Except(node.ReturnType != null ? [node.ReturnType] : []));

            TryWrapInYieldingGetEnumerable(
                node,
                (node.ReturnType as GenericNameSyntax)?.TypeArgumentList.Arguments,
                [node.Body ?? (SyntaxNode)node.ExpressionBody!],
                isAsync: node.Modifiers.IsAsync()
            //node.ChildNodes().Where(e => !e.IsKind(SyntaxKind.IdentifierName)/* is not IdentifierNameSyntax*/ && !e.IsKind(SyntaxKind.ParameterList) /*is not ParameterListSyntax */&& !e.IsKind(SyntaxKind.TypeParameterConstraintClause)/* is not TypeParameterConstraintClauseSyntax*/ && !e.IsKind(SyntaxKind.ExplicitInterfaceSpecifier) /*is not ExplicitInterfaceSpecifierSyntax*/).Except(node.ReturnType != null ? [node.ReturnType] : [])
            );

            //base.VisitMethodDeclaration(node);
            if (node.ExpressionBody != null)
            {
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
            CloseClosure();
            //base.VisitLocalFunctionStatement(node);
        }

        public override void VisitExtensionBlockDeclaration(ExtensionBlockDeclarationSyntax node)
        {
            if (!ShouldExportMethod(node))
                return;
            List<IDisposable> dispose = new List<IDisposable>();
            if (node.ParameterList != null)
            {
                foreach (var parameter in node.ParameterList.Parameters)
                {
                    if (parameter.Type != null)
                    {
                        var d = CurrentClosure.DefineIdentifierType(parameter.Identifier.ValueText, parameter.Type, SymbolKind.Parameter);
                        dispose.Add(d);
                    }
                }
            }
            VisitChildren(node.Members);
            dispose.ForEach(d => d.Dispose());
            //base.VisitExtensionBlockDeclaration(node);
        }

        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            if (!ShouldExportMethod(node))
                return;
            CurrentTypeWriter.WriteLine(node, "$dtor()", true);
            if (node.ExpressionBody != null)
            {
                CurrentTypeWriter.WriteLine(node, "{", true);
            }
            base.VisitDestructorDeclaration(node);
            if (node.ExpressionBody != null)
            {
                CurrentTypeWriter.WriteLine(node, "}", true);
            }
        }
    }
}
