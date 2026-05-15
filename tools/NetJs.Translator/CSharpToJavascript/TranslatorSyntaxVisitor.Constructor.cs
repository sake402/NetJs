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
using System.Xml;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        enum ConstructorCallConvention
        {
            PrototypeCall,
            StaticCall,
            InstanceCall
        }

        ConstructorCallConvention constructorCallConvention = ConstructorCallConvention.InstanceCall;
        List<CSharpSyntaxNode> _primaryConstructorInitialized = new List<CSharpSyntaxNode>();
        void MarkMemberAsInitializedByPrimaryConstructor(CSharpSyntaxNode member)
        {
            _primaryConstructorInitialized.Add(member);
        }

        bool MemberWasInitializedByPrimaryConstructor(CSharpSyntaxNode member, EqualsValueClauseSyntax? initializer)
        {
            if (_primaryConstructorInitialized.Contains(member))
                return true;
            if (initializer?.Value is IdentifierNameSyntax id)
            {
                var initValue = CurrentClosure.GetIdentifierType(id.Identifier.ValueText);
                if (initValue.Kind == SymbolKind.Field && initValue.Tag is string ss && ss == nameof(WritePrimaryConstructor))
                {
                    return true;
                }
            }
            return false;
        }

        void WritePrimaryConstructor(BaseTypeDeclarationSyntax node, INamedTypeSymbol typeSymbol, IEnumerable<ParameterSyntax> primaryConstructorParameters)
        {
            CurrentTypeWriter.WriteLine(node, "//Begin primary constructor", true);
            IMethodSymbol? constructorSymbol = null;
            foreach (var parameter in primaryConstructorParameters)
            {
                //if there is a field with same name as this primary constructor parameter, dont write the firld for the parameter
                var possibleField = typeSymbol.GetMembers(parameter.Identifier.ValueText).SingleOrDefault();
                if (possibleField != null && possibleField.Kind == SymbolKind.Field)
                {
                    continue;
                }
                var parameterSymbol = _global.GetSymbol(parameter, this/*, out _, out _*/);
                constructorSymbol = parameterSymbol.ContainingSymbol as IMethodSymbol;
                if (parameterSymbol.Kind == SymbolKind.Property ||
                    parameterSymbol.Kind == SymbolKind.Method ||
                    typeSymbol.GetMembers(parameterSymbol.Name).Any())
                {
                }
                else
                {
                    CurrentClosure.DefineIdentifierType(parameter.Identifier.ValueText, CodeSymbol.From(parameterSymbol) with
                    {
                        //convert the constructor parameters to field
                        Kind = SymbolKind.Field,
                        Tag = nameof(WritePrimaryConstructor)
                    });
                }
                //We cant be sure if there will be a method/property that accesses this primary constructor parameter
                //So we create is as a field always 
                CurrentTypeWriter.Write(parameter, $"/*{parameter.Type}*/ ", true);
                CurrentTypeWriter.Write(parameter, parameter.Identifier.ValueText);
                CurrentTypeWriter.WriteLine(parameter, ";");
            }
            constructorSymbol ??= typeSymbol.GetMembers(".ctor").Cast<IMethodSymbol>().Where(e => e.IsPrimaryConstructor(_global)).Single();
            var metadata = _global.GetRequiredMetadata(constructorSymbol);
            CurrentTypeWriter.Write(node, metadata.OverloadName!, true);
            CurrentTypeWriter.Write(node, "(");
            int i = 0;
            foreach (var parameter in primaryConstructorParameters)
            {
                if (i > 0)
                    CurrentTypeWriter.Write(node, ", ");
                CurrentTypeWriter.Write(parameter, $"/*{parameter.Type}*/");
                CurrentTypeWriter.Write(parameter, $"$");
                CurrentTypeWriter.Write(parameter, parameter.Identifier.ValueText);
                i++;
            }
            CurrentTypeWriter.WriteLine(node, ")");
            CurrentTypeWriter.WriteLine(node, "{", true);
            //We cant be sure if there will be a method/property that accesses this primary constructor parameter
            //So we create is as a field always and intialize it from the passed parameter
            foreach (var parameter in primaryConstructorParameters)
            {
                CurrentTypeWriter.Write(parameter, $"this.", true);
                CurrentTypeWriter.Write(parameter, parameter.Identifier.ValueText);
                CurrentTypeWriter.Write(parameter, $" = $");
                CurrentTypeWriter.Write(parameter, parameter.Identifier.ValueText);
                CurrentTypeWriter.WriteLine(parameter, ";");
                i++;
            }
            //Find all fields and properties that are initialized using a primary constructor parameter and initialize such in this constructor
            var constructorParameterNames = primaryConstructorParameters.Select(cp => cp.Identifier.ValueText).ToList();

            bool MemberReferencesConstructorParameter(SyntaxNode node)
            {
                if (node is IdentifierNameSyntax id && constructorParameterNames.Contains(id.Identifier.ValueText))
                    return true;
                return node.ChildNodes().Any(e => MemberReferencesConstructorParameter(e));
            }

            var fields = node.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(f => f.Declaration.Variables)
                .Where(v => v.Initializer?.Value != null)
                .Where(v => MemberReferencesConstructorParameter(v));
            foreach (var field in fields)
            {
                CurrentTypeWriter.Write(field, $"this.", true);
                CurrentTypeWriter.Write(field, field.Identifier.ValueText);
                CurrentTypeWriter.Write(field, $" = $");
                //Writer.Write(field, ((IdentifierNameSyntax)field.Initializer!.Value).Identifier.ValueText);
                Visit(field.Initializer);
                CurrentTypeWriter.WriteLine(field, ";");
                MarkMemberAsInitializedByPrimaryConstructor(field);
                i++;
            }
            var properties = node.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(v => v.Initializer?.Value != null)
                .Where(v => MemberReferencesConstructorParameter(v));
            foreach (var property in properties)
            {
                CurrentTypeWriter.Write(property, $"this.", true);
                CurrentTypeWriter.Write(property, property.Identifier.ValueText);
                CurrentTypeWriter.Write(property, $" = $");
                //Writer.Write(property, ((IdentifierNameSyntax)property.Initializer!.Value).Identifier.ValueText);
                Visit(property.Initializer!.Value);
                CurrentTypeWriter.WriteLine(property, ";");
                MarkMemberAsInitializedByPrimaryConstructor(property);
                i++;
            }
            CurrentTypeWriter.WriteLine(node, "}", true);
            CurrentTypeWriter.WriteLine(node, "//End primary constructor", true);
        }

        void WriteConstructorDeclaration(BaseMethodDeclarationSyntax node, IMethodSymbol constructorSymbol, IEnumerable<ParameterSyntax> parameters, ConstructorInitializerSyntax? baseInitializer)
        {
            string? modifier = GetMethodModifier(node, node.Modifiers, null);
            //if (node.Modifiers.Any(e => e.ValueText == "async"))
            //{
            //    modifier += "async ";
            //}
            var typeSymbol = (INamedTypeSymbol)constructorSymbol.ContainingSymbol;
            var constructors = typeSymbol.Constructors;
            //A class may have multiple constructor, use a named ($ctor) method as the constructor
            var meta = _global.GetRequiredMetadata(constructorSymbol);
            //need to make sure the parameters of a constructor are available early before body, as base clas call may use it
            //this will be repeated uneccassrily by WriteMethodBody though
            var cParameters = string.Join(", ", parameters.Select(p => $"{GetMethodParameterModifier(p)} {Utilities.ResolveIdentifierName(p.Identifier)}"));
            DefineParametersInClosure(parameters, constructorSymbol);
            var baseType = ((ITypeSymbol)constructorSymbol.ContainingSymbol).BaseType;
            var boundBaseConstructor = baseInitializer != null ? GetExpressionBoundTarget(baseInitializer).TypeSyntaxOrSymbol as IMethodSymbol : null;
            var baseConstructor = baseType != null && baseInitializer != null ? boundBaseConstructor ?? GetBestOverloadMethod(baseType, ".ctor", null, baseInitializer.ArgumentList.Arguments, null, out _) : null;
            var useNativeConstructor = baseConstructor != null ? _global.IsExternal(baseConstructor.ContainingType, this) : false;
            if (_global.HasAnyAttribute(constructorSymbol, this, false, typeof(NativeConstructorAttribute).FullName))
            {
                useNativeConstructor = true;
            }
            if (useNativeConstructor)
            {
                CurrentTypeWriter.WriteLine(node, $"constructor({cParameters})", true);
            }
            else
            {
                CurrentTypeWriter.WriteLine(node, $"{modifier}{(constructorCallConvention == ConstructorCallConvention.StaticCall ? "static " : "")}{meta.OverloadName}({cParameters})", true);
            }
            WriteMethodBody(node, null, null, parameters, writePrologue: () =>
            {
                var systemObject = _global.GetSymbol("System.Object", this/*, out _, out _*/);
                //MethodOverloadResult overloadResult = default;
                //bool baseIsExternal = false;

                if (useNativeConstructor)
                {
                    CurrentTypeWriter.Write(node, $"super", true);
                    WriteMethodInvocationParameter(node, baseConstructor, null, baseInitializer!.ArgumentList.Arguments.Select(a => new CodeNode(a)), constructorCallConvention != ConstructorCallConvention.InstanceCall ? (Action)(() => CurrentTypeWriter.Write(node, "this")) : null);
                    CurrentTypeWriter.WriteLine(node, $";");
                }
                else
                {
                    if (baseConstructor != null)
                    {
                        var baseConstructorMeta = _global.GetRequiredMetadata(baseConstructor);
                        EnsureImported(baseType);
                        if (baseConstructor.ContainingSymbol.Equals(typeSymbol, SymbolEqualityComparer.Default))
                        {
                            CurrentTypeWriter.Write(node, $"this.{(baseConstructorMeta?.OverloadName ?? "$ctor")}", true);
                        }
                        else if (baseConstructorMeta != null)
                        {
                            CurrentTypeWriter.Write(node, $"super.{(baseConstructorMeta.OverloadName ?? "$ctor")}", true);
                        }
                        WriteMethodInvocationParameter(node, baseConstructor, null, baseInitializer!.ArgumentList.Arguments.Select(a => new CodeNode(a)), constructorCallConvention != ConstructorCallConvention.InstanceCall ? (Action)(() => CurrentTypeWriter.Write(node, "this")) : null);
                        CurrentTypeWriter.WriteLine(node, $";");
                    }
                    else if (baseType == null || !baseType.Equals(systemObject, SymbolEqualityComparer.Default))
                    {
                        //implicit call to base constructor
                        //Writer.WriteLine(node, $"super.$ctor();", true);
                    }
                }
            }, writeEpilogue: useNativeConstructor ? null : () =>
            {
                CurrentTypeWriter.WriteLine(node, $"return this;", true);
            });
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.Modifiers.IsExtern())
            {
                return;
            }
            if (node.Body == null && node.ExpressionBody == null)
                return;
            var constructorSymbol = (IMethodSymbol)OpenClosure(node);// _global.GetTypeSymbol(node, this, out _, out _);
            if (_global.LinkTrimOutMethod(constructorSymbol))
                return;
            bool _static = node.Modifiers.IsStatic();
            if (_static)
            {
                CurrentTypeWriter.WriteLine(node, "/*Static constructor*/ static $cctor()", true);
                WriteMethodBody(node, null, null, node.ParameterList.Parameters);
            }
            else
            {
                WriteConstructorDeclaration(node, constructorSymbol, node.ParameterList.Parameters, node.Initializer);
                //string? modifier = null;
                //if (node.Modifiers.Any(e => e.ValueText == "async"))
                //{
                //    modifier += "async ";
                //}
                //var typeSymbol = (INamedTypeSymbol)constructorSymbol.ContainingSymbol;
                //var constructors = typeSymbol.Constructors;
                ////A class may have multiple constructor, use a named ($ctor) method as the constructor
                //var meta = _global.GetRequiredMetadata(constructorSymbol);
                ////need to make sure the parameters of a constructor are available early before body, as base clas call may use it
                ////this will be repeated uneccassrily by WriteMethodBody though
                //DefineParametersInClosure(node.ParameterList.Parameters, constructorSymbol);
                //Writer.WriteLine(node, $"{modifier}{(constructorCallConvention == ConstructorCallConvention.StaticCall ? "static " : "")}{meta.OverloadName}({parameters})", true);
                //WriteMethodBody(node, null, null, node.ParameterList.Parameters, writePrologue: () =>
                //{
                //    var systemObject = _global.GetTypeSymbol("System.Object", this, out _, out _);
                //    MethodOverloadResult overloadResult = default;
                //    var baseType = ((ITypeSymbol)constructorSymbol.ContainingSymbol).BaseType;
                //    var boundBaseConstructor = node.Initializer != null ? GetExpressionBoundMember(node.Initializer).TypeSyntaxOrSymbol as IMethodSymbol : null;
                //    var baseConstructor = baseType != null && node.Initializer != null ? boundBaseConstructor ?? GetBestOverloadMethod(baseType, ".ctor", null, node.Initializer.ArgumentList.Arguments, null, out overloadResult) : null;
                //    if (baseConstructor != null)
                //    {
                //        var baseConstructorMeta = _global.GetRequiredMetadata(baseConstructor);
                //        EnsureImported(baseType);
                //        if (baseConstructor.ContainingSymbol.Equals(typeSymbol, SymbolEqualityComparer.Default))
                //        {
                //            Writer.Write(node, $"this.{(baseConstructorMeta?.OverloadName ?? "$ctor")}", true);
                //        }
                //        else if (baseConstructorMeta != null)
                //        {
                //            Writer.Write(node, $"super.{(baseConstructorMeta.OverloadName ?? "$ctor")}", true);
                //        }
                //        WriteMethodInvocationParameter(node, baseConstructor, null, node.Initializer!.ArgumentList.Arguments, constructorCallConvention != ConstructorCallConvention.InstanceCall ? "this" : null);
                //        Writer.WriteLine(node, $";");
                //    }
                //    else if (baseType == null || !baseType.Equals(systemObject, SymbolEqualityComparer.Default))
                //    {
                //        //implicit call to base constructor
                //        //Writer.WriteLine(node, $"super.$ctor();", true);
                //    }
                //}, writeEpilogue: () =>
                //{
                //    Writer.WriteLine(node, $"return this;", true);
                //});
            }
            CloseClosure();
        }

        public void WriteConstructorCall(
            CSharpSyntaxNode node,
            ITypeSymbol typeSymbol,
            IMethodSymbol targetConstructor,
            TypeArgumentListSyntax? genericArgs = null,
            IEnumerable<CodeNode>? parameterArgs = null,
            CodeNode? suffixArguments = null,
            MethodOverloadResult overloadResult = default)
        {
            var typeMetadata = typeSymbol.Kind != SymbolKind.TypeParameter ? _global.GetRequiredMetadata(typeSymbol) : null;
            void CallDefaultConstructor()
            {
                CurrentTypeWriter.Write(node, "new ");
                if (genericArgs != null || (typeSymbol is INamedTypeSymbol nt && nt.IsGenericType))
                {
                    CurrentTypeWriter.Write(node, "(");
                }
                if (targetConstructor?.IsExtern ?? false)
                    CurrentTypeWriter.Write(node, typeSymbol.Name);
                else
                    CurrentTypeWriter.Write(node, typeMetadata?.InvocationName ?? typeSymbol.Name);
                //if (genericArgs != null)
                //{
                //    Writer.Write(node, "(");
                //    int ix = 0;
                //    foreach (var t in genericArgs.Arguments)
                //    {
                //        if (ix > 0)
                //            Writer.Write(node, ", ");
                //        Visit(t);
                //        ix++;
                //    }
                //    Writer.Write(node, ")");
                //}
                if (genericArgs != null || (typeSymbol is INamedTypeSymbol nt2 && nt2.IsGenericType))
                {
                    CurrentTypeWriter.Write(node, ")");
                }
                CurrentTypeWriter.Write(node, "()");
            }
            //if (targetConstructor.IsExtern/* || typeSymbol.Kind == SymbolKind.TypeParameter*/)
            //{
            //    CallDefaultConstructor();
            //}
            //else
            {
                var constructorMetadata = _global.GetRequiredMetadata(targetConstructor);
                if (constructorCallConvention == ConstructorCallConvention.PrototypeCall)
                {
                    CurrentTypeWriter.Write(node, $"{typeMetadata?.InvocationName ?? typeSymbol.Name}");
                    CurrentTypeWriter.Write(node, $".prototype.{constructorMetadata?.OverloadName ?? ((targetConstructor == null && parameterArgs?.Count() > 0) ? "$ctor$$1" : null) ?? "$ctor"}.call");
                }
                else if (constructorCallConvention == ConstructorCallConvention.StaticCall)
                {
                    CurrentTypeWriter.Write(node, $"{typeMetadata?.InvocationName ?? typeSymbol.Name}.{constructorMetadata?.OverloadName ?? ((targetConstructor == null && parameterArgs?.Count() > 0) ? "$ctor$$1" : null) ?? "$ctor"}.call");
                }
                else
                {
                    if (targetConstructor.IsExtern)
                    {
                        CurrentTypeWriter.Write(node, "new ");
                        CurrentTypeWriter.Write(node, typeSymbol.Name);
                    }
                    else
                    {
                        CallDefaultConstructor();
                        //if the compiler generated this constructor, then it doenst really exist in our js code
                        if (targetConstructor.Parameters.Count() == 0 && targetConstructor.IsImplicitlyDeclared)
                        {
                            return;
                        }
                        CurrentTypeWriter.Write(node, $".{constructorMetadata.OverloadName ?? ((targetConstructor == null && parameterArgs?.Count() > 0) ? "$ctor$1" : null) ?? "$ctor"}");
                    }
                }
                WriteMethodInvocationParameter(node, targetConstructor, genericArgs, parameterArgs,
                    prefixArguments: constructorCallConvention == ConstructorCallConvention.PrototypeCall || constructorCallConvention == ConstructorCallConvention.StaticCall ? (Action)(() =>
                {
                    CallDefaultConstructor();
                }) : null, suffixArguments: suffixArguments, overloadResult: overloadResult);
            }
        }

        void WriteInitializer(CSharpSyntaxNode node, string instanceName, ITypeSymbol instanceType, IEnumerable<CSharpSyntaxNode> expressions)
        {
            bool isLiteralObject = _global.HasAttribute(instanceType, typeof(ObjectLiteralAttribute).FullName!, this, false, out _);
            foreach (var expression in expressions)
            {
                if (expression is AssignmentExpressionSyntax assignment && assignment.Left is ImplicitElementAccessSyntax imp)
                {
                    if (isLiteralObject)
                    {
                        CurrentTypeWriter.Write(node, $"{instanceName}", true);
                        //If the literal name is a valid js name, convert the [""] to . operation
                        if (imp.ArgumentList.Arguments.Count == 1)
                        {
                            var argExpression = imp.ArgumentList.Arguments.First().Expression;
                            if (argExpression.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                var litetal = (LiteralExpressionSyntax)argExpression;
                                var valueName = litetal.Token.ValueText;
                                if (valueName.IsValidateJsName())
                                {
                                    CurrentTypeWriter.Write(node, ".");
                                    CurrentTypeWriter.Write(node, valueName);
                                    CurrentTypeWriter.Write(node, " = ");
                                    Visit(assignment.Right);
                                    CurrentTypeWriter.WriteLine(node, ";");
                                    continue;
                                }
                            }
                        }
                        Visit(assignment);
                        CurrentTypeWriter.WriteLine(node, ";");
                        continue;
                    }
                    else
                    {
                        var disposable = CurrentClosure.DefineIdentifierType(instanceName, CodeSymbol.From(instanceType));
                        //rewrite new a { [b] = .. } as instance[b] = ..
                        var rewrite = SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(instanceName), imp.ArgumentList),
                            assignment.Right
                            );
                        CurrentTypeWriter.Write(node, "", true);
                        Visit(rewrite);
                        disposable.Dispose();
                        CurrentTypeWriter.WriteLine(node, ";");
                        continue;
                    }
                }
                else if (expression is InitializerExpressionSyntax init)
                {
                    CurrentTypeWriter.Write(node, $"{instanceName}.Add(", true);
                    int ix = 0;
                    foreach (var exp in init.Expressions)
                    {
                        if (ix > 0)
                            CurrentTypeWriter.Write(node, ", ");
                        Visit(exp);
                        ix++;
                    }
                    CurrentTypeWriter.WriteLine(node, ");");
                    continue;
                }
                else if (expression is ExpressionElementSyntax element)
                {
                    CurrentTypeWriter.Write(node, $"{instanceName}.Add(", true);
                    Visit(element);
                    CurrentTypeWriter.WriteLine(node, ");");
                    continue;
                }
                else if (expression is SpreadElementSyntax spread)
                {
                    CurrentTypeWriter.Write(node, $"{instanceName}.AddRange(", true);
                    Visit(spread.Expression);
                    CurrentTypeWriter.WriteLine(node, ");");
                    continue;
                }
                else if (expression is AssignmentExpressionSyntax assignment2)
                {
                    CurrentTypeWriter.Write(node, $"{instanceName}.", true);
                    Visit(expression);
                    CurrentTypeWriter.WriteLine(node, ";");
                }
                else
                {
                    var addMethod = (IMethodSymbol)instanceType.GetMembers("Add").Single(e => e is IMethodSymbol ms && ms.Parameters.Length == 1);
                    CurrentTypeWriter.Write(node, "", true);
                    WriteMethodInvocation(node, addMethod, null, [expression], new CodeNode(() =>
                    {
                        CurrentTypeWriter.Write(node, $"{instanceName}");
                    }), instanceType);
                    CurrentTypeWriter.WriteLine(node, ";");
                }
            }
        }

        void WriteObjectCreation(CSharpSyntaxNode node, TypeSyntax? type, ITypeSymbol? typeSymbol, IMethodSymbol? boundConstructor, IEnumerable<CodeNode>? arguments, InitializerExpressionSyntax? initializer)
        {
            if (type == null && typeSymbol == null)
                throw new InvalidOperationException("ONE of type or typeSymbol is required");
            typeSymbol ??= _global.GetTypeSymbol(type!, this)!;
            if (typeSymbol.TypeKind == TypeKind.Delegate)
            {
                if (arguments != null)
                {
                    foreach (var arg in arguments)
                    {
                        VisitNode(arg);
                    }
                }
                return;
            }
            bool isLiteralObject = _global.HasAttribute(typeSymbol, typeof(ObjectLiteralAttribute).FullName!, this, false, out _);
            bool hasInitializer = initializer?.Expressions != null;
            void WrapInExpression(Action<string, string> _continue)
            {
                var i = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                string classTargetName = $"$t{i}";
                var instanceName = $"$i{i}";
                WrapStatementsInExpression(node, () =>
                {
                    _continue(classTargetName, instanceName);
                    CurrentTypeWriter.WriteLine(node, $"return {instanceName};", true);
                });
                //CurrentTypeWriter.WriteLine(node, $"{_global.GlobalName}.{Constants.Expression}(function()");
                //CurrentTypeWriter.WriteLine(node, $"{{", true);
                //_continue(classTargetName, instanceName);
                //CurrentTypeWriter.WriteLine(node, $"return {instanceName};", true);
                //CurrentTypeWriter.Write(node, $"}}.bind(this))", true);
            }
            if (!isLiteralObject)
            {
                MethodOverloadResult overloadResult = default;
                TypeArgumentListSyntax? genericArgs = null;
                if (type is GenericNameSyntax gn)
                {
                    genericArgs = gn.TypeArgumentList;
                }
                boundConstructor ??= GetExpressionBoundTarget(node).TypeSyntaxOrSymbol as IMethodSymbol;
                var targetConstructor = boundConstructor ?? GetBestOverloadMethod(typeSymbol, ".ctor", null, arguments.Select(a => a.AsT0), null, out overloadResult) ?? throw new InvalidOperationException("Cannot find the constructor");
                bool isCompilerGeneratedConstructor = targetConstructor.Parameters.Count() == 0 && targetConstructor.IsImplicitlyDeclared;
                if (_global.HasAttribute(targetConstructor, typeof(TemplateAttribute).FullName!, this, false, out _))
                {
                    WriteMethodInvocation(node, targetConstructor, null, arguments, null, null, genericArgs, false);
                }
                else
                {
                    if (!hasInitializer)
                    {
                        WriteConstructorCall(node, typeSymbol, targetConstructor, genericArgs, arguments, overloadResult: overloadResult);
                    }
                    else
                    {
                        var meta = _global.GetRequiredMetadata(typeSymbol);
                        var constructorMeta = _global.GetRequiredMetadata(targetConstructor);
                        WrapInExpression((classTargetName, instanceName) =>
                        {
                            CurrentTypeWriter.WriteLine(node, $"//new {meta.InvocationName ?? typeSymbol.Name}()", true);
                            CurrentTypeWriter.Write(node, $"const {classTargetName} = ", true);
                            CurrentTypeWriter.Write(node, meta.InvocationName ?? typeSymbol.Name);
                            if (meta.InvocationName == null && genericArgs != null)
                            {
                                CurrentTypeWriter.Write(node, "(");
                                int ix = 0;
                                foreach (var t in genericArgs.Arguments)
                                {
                                    if (ix > 0)
                                        CurrentTypeWriter.Write(node, ", ");
                                    Visit(t);
                                    ix++;
                                }
                                CurrentTypeWriter.Write(node, ")");
                            }
                            CurrentTypeWriter.WriteLine(node, ";");

                            CurrentTypeWriter.WriteLine(node, $"const {instanceName} = new {classTargetName}();", true);
                            if (constructorCallConvention == ConstructorCallConvention.PrototypeCall)
                            {
                                CurrentTypeWriter.Write(node, classTargetName);
                                CurrentTypeWriter.Write(node, $".prototype.{constructorMeta?.InvocationName ?? "$ctor"}.call", true);
                            }
                            else if (constructorCallConvention == ConstructorCallConvention.StaticCall)
                            {
                                CurrentTypeWriter.Write(node, $"{classTargetName}.{constructorMeta?.InvocationName ?? "$ctor"}.call");
                            }
                            else
                            {
                                if (!isCompilerGeneratedConstructor)
                                {
                                    CurrentTypeWriter.Write(node, $"{instanceName}", true);
                                    CurrentTypeWriter.Write(node, $".{(constructorMeta?.OverloadName ?? "$ctor")}");
                                }
                            }
                            ITypeSymbol? delegateReturnType = null;
                            IEnumerable<ITypeSymbol>? delegateParameterTypes = null;
                            typeSymbol.IsDelegate(out delegateReturnType, out delegateParameterTypes);
                            IDisposable? disposeAnonymousMethodParameter = null;
                            if (delegateParameterTypes != null)
                            {
                                disposeAnonymousMethodParameter = CurrentClosure.DefineAnonymousMethodParameterTypes(delegateReturnType == null ? delegateParameterTypes : [.. delegateParameterTypes, delegateReturnType]);
                            }
                            if (!isCompilerGeneratedConstructor)
                            {
                                WriteMethodInvocationParameter(node,
                                    targetConstructor,
                                    genericArgs,
                                    arguments, prefixArguments: constructorCallConvention == ConstructorCallConvention.InstanceCall ? null : (Action)(() => CurrentTypeWriter.Write(node, $"{instanceName}")),
                                    overloadResult: overloadResult);
                                CurrentTypeWriter.WriteLine(node, ";");
                            }
                            disposeAnonymousMethodParameter?.Dispose();
                            if (hasInitializer)
                            {
                                WriteInitializer(node, instanceName, typeSymbol, initializer!.Expressions);
                            }
                            VisitChildren(node.ChildNodes().Where((e => !e.IsKind(SyntaxKind.ArgumentList)/* is not ArgumentListSyntax*/ && e is not InitializerExpressionSyntax)).Except([type])!);
                        });
                    }
                }
            }
            else
            {
                if (hasInitializer)
                {
                    if (initializer!.Expressions.All(e => e is AssignmentExpressionSyntax assignment && assignment.Left is not ImplicitElementAccessSyntax))
                    {
                        CurrentTypeWriter.Write(node, "{");
                        int i = 0;
                        foreach (var e in initializer!.Expressions)
                        {
                            if (i > 0)
                                CurrentTypeWriter.Write(node, ", ");
                            if (e is AssignmentExpressionSyntax assign)
                            {
                                if (assign.Left is IdentifierNameSyntax id)
                                {
                                    var member = typeSymbol.GetMembers(id.Identifier.ValueText, _global).First();
                                    //var idType = GetTypeSymbol(id);
                                    var metadata = _global.GetRequiredMetadata(member);
                                    CurrentTypeWriter.Write(node, metadata.InvocationName ?? Utilities.ResolveIdentifierName(id.Identifier));
                                }
                                else
                                {
                                    Visit(assign.Left);
                                }
                                CurrentTypeWriter.Write(node, ": ");
                                Visit(assign.Right);
                            }
                            else
                                Visit(e);
                            i++;
                        }
                        CurrentTypeWriter.Write(node, "}");
                    }
                    else
                    {
                        WrapInExpression((className, instanceName) =>
                        {
                            CurrentTypeWriter.WriteLine(node, $"const {instanceName} = {{}};", true);
                            WriteInitializer(node, instanceName, typeSymbol, initializer!.Expressions);
                        });
                    }
                }
                else
                {
                    CurrentTypeWriter.Write(node, "{}");
                }
            }
        }
        public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            CurrentTypeWriter.Write(node, "{ ");
            int ix = 0;
            foreach (var i in node.Initializers)
            {
                if (ix > 0)
                    CurrentTypeWriter.Write(node, ", ");
                if (i.NameEquals != null)
                    CurrentTypeWriter.Write(node, i.NameEquals.Name.Identifier.ValueText);
                else
                    CurrentTypeWriter.Write(node, i.Expression.ToString().Split('.').Last());
                CurrentTypeWriter.Write(node, " : ");
                Visit(i.Expression);
                ix++;
            }
            CurrentTypeWriter.Write(node, " }");
            //base.VisitAnonymousObjectCreationExpression(node);
        }

        public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            var tSymbol = _global.TryGetTypeSymbol(node, this);
            var type = tSymbol == null ? InferType(node) : default;
            WriteObjectCreation(node, type.type, tSymbol ?? type.typeSymbol, null, node.ArgumentList.Arguments.Select(e => new CodeNode(e)), node.Initializer);
            //base.VisitImplicitObjectCreationExpression(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            EnsureImported(node.Type);
            var type = CodeSymbol.From(node.Type, SymbolKind.NamedType);
            WriteObjectCreation(node, node.Type, null, null, node.ArgumentList?.Arguments.Select(e => new CodeNode(e)), node.Initializer);
            memberAccesChainCurrentType = type;
            //base.VisitObjectCreationExpression(node);
        }
    }
}
