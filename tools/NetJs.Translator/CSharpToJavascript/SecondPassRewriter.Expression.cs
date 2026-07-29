using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class SecondPassRewriter : CSharpSyntaxRewriter
    {
        public ExpressionSyntax ConvertLambdaToCachedBlock(
             ExpressionSyntax originalNode,
             ParameterListSyntax parameters,
             SyntaxNode body,
             INamedTypeSymbol? expressionTypeSymbol)
        {
            var statements = new List<StatementSyntax>();
            var parameterVariables = new List<string>();

            // 1. Resolve parameter types from the INamedTypeSymbol (Expression<Func<T, TResult>>)
            var resolvedTypeNames = ExtractParameterTypeNames(expressionTypeSymbol, parameters.Parameters.Count);

            // 2. Declare and cache each ParameterExpression into a local variable
            for (int i = 0; i < parameters.Parameters.Count; i++)
            {
                var param = parameters.Parameters[i];
                string paramName = param.Identifier.ValueText;

                // Fallback sequence: Explicit syntax type -> Resolved semantic type symbol name -> default object string
                string typeName = param.Type?.ToString() ?? resolvedTypeNames.ElementAtOrDefault(i) ?? "object";

                string varName = $"param_{paramName}";
                parameterVariables.Add(varName);

                // Generates with spaces and correct type: var param_n = System.Linq.Expressions.Expression.Parameter(typeof(TargetType), "n");
                var paramDeclaration = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var").WithTrailingTrivia(SyntaxFactory.Space),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(varName),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.Token(SyntaxKind.EqualsToken)
                                        .WithLeadingTrivia(SyntaxFactory.Space)
                                        .WithTrailingTrivia(SyntaxFactory.Space),
                                    CreateFactoryCall("Parameter",
                                        SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(typeName))),
                                        SyntaxFactory.Argument(CreateStringLiteral(paramName))
                                    )
                                )
                            )
                        )
                    )
                ).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                statements.Add(paramDeclaration);
            }

            // 3. Convert the body, passing our tracking list
            var bodyExpressionCode = ConvertNodeToFactory(body, parameterVariables);

            // 4. Assemble the final System.Linq.Expressions.Expression.Lambda(body, params)
            var lambdaArguments = new List<ArgumentSyntax> { SyntaxFactory.Argument(bodyExpressionCode) };
            foreach (var varName in parameterVariables)
            {
                lambdaArguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(varName)));
            }

            var lambdaCall = CreateFactoryCall("Lambda", lambdaArguments.ToArray());

            // Generates: return System.Linq.Expressions.Expression.Lambda(...);
            var returnStatement = SyntaxFactory.ReturnStatement(
                SyntaxFactory.Token(SyntaxKind.ReturnKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                lambdaCall,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken)
            ).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            statements.Add(returnStatement);

            // 5. Wrap in System.Func<System.Linq.Expressions.LambdaExpression>
            var wrapperBlock = SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed),
                SyntaxFactory.List(statements),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
            );

            var inlineLambda = SyntaxFactory.ParenthesizedLambdaExpression(wrapperBlock);

            // Constructs: new System.Func<System.Linq.Expressions.LambdaExpression>
            var delegateTypeNode = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("System.Func"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                        SyntaxFactory.ParseTypeName("System.Linq.Expressions.LambdaExpression")
                    )
                )
            );

            var objectCreationExpression = SyntaxFactory.ObjectCreationExpression(
                delegateTypeNode,
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(inlineLambda))),
                null
            );

            // Invoke the Func: new System.Func<...>(() => { ... })()
            var functionInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParenthesizedExpression(objectCreationExpression)
            );

            // 6. Cast the outer final result to (dynamic) to allow implicit conversion to generic Expressions
            //var finalCastExpression = SyntaxFactory.CastExpression(
            //    SyntaxFactory.ParseTypeName("dynamic"),
            //    SyntaxFactory.ParenthesizedExpression(functionInvocation)
            //);

            //return finalCastExpression.NormalizeWhitespace().WithTriviaFrom(originalNode);

            // 6. Cast the final result using the .As<T>() extension method instead of native casting

            // Fallback safely to "dynamic" if the symbol cannot be semantically resolved
            string targetTypeName = expressionTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "dynamic";

            // Represents the generic method identifier: .As<System.Linq.Expressions.Expression<...>>
            var genericAsMethod = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("As"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                        SyntaxFactory.ParseTypeName(targetTypeName)
                    )
                )
            );

            // Chains the method call to the existing parenthesized invocation expression:
            // (...your invocation node...).As<TargetType>()
            var extensionMethodCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParenthesizedExpression(functionInvocation), // your existing wrapper node
                    genericAsMethod
                )
            );

            // Apply clean structural formatting while preserving original layout locations
            return extensionMethodCall.NormalizeWhitespace().WithTriviaFrom(originalNode);
        }


        // Helper logic to traverse the INamedTypeSymbol safely
        private List<string> ExtractParameterTypeNames(INamedTypeSymbol? typeSymbol, int expectedCount)
        {
            var typeNames = new List<string>();
            if (typeSymbol == null) return typeNames;

            // Step A: Unwrap System.Linq.Expressions.Expression<TDelegate> -> get TDelegate
            INamedTypeSymbol? delegateType = typeSymbol;
            if (typeSymbol.ToDisplayString().StartsWith("System.Linq.Expressions.Expression<") && typeSymbol.TypeArguments.Length > 0)
            {
                delegateType = typeSymbol.TypeArguments[0] as INamedTypeSymbol;
            }

            // Step B: Drill into System.Func<T1, T2, TResult> -> extract the generic input parameters
            if (delegateType != null && delegateType.TypeArguments.Length > 0)
            {
                // In a Func, the last argument is the return type. Take everything up to the last index.
                int parameterCount = Math.Min(delegateType.TypeArguments.Length - 1, expectedCount);
                for (int i = 0; i < parameterCount; i++)
                {
                    typeNames.Add(delegateType.TypeArguments[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }

            return typeNames;
        }

        private ExpressionSyntax ConvertNodeToFactory(SyntaxNode node, List<string> parameterVariables)
        {
            return node switch
            {
                ParenthesizedExpressionSyntax parenthesized =>
                    ConvertNodeToFactory(parenthesized.Expression, parameterVariables),

                CastExpressionSyntax cast =>
                   CreateFactoryCall("Convert",
                       SyntaxFactory.Argument(ConvertNodeToFactory(cast.Expression, parameterVariables)),
                       SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(cast.Type))
                   ),

                IdentifierNameSyntax identifier =>
                    ResolveParameterIdentifier(identifier, parameterVariables),

                MemberAccessExpressionSyntax memberAccess =>
                    CreateFactoryCall("Property",
                        SyntaxFactory.Argument(ConvertNodeToFactory(memberAccess.Expression, parameterVariables)),
                        SyntaxFactory.Argument(CreateStringLiteral(memberAccess.Name.Identifier.ValueText))
                    ),

                PredefinedTypeSyntax predefinedType =>
                    SyntaxFactory.TypeOfExpression(predefinedType),

                LiteralExpressionSyntax literal =>
                    CreateFactoryCall("Constant", SyntaxFactory.Argument(literal)),

                BinaryExpressionSyntax binary =>
                    CreateFactoryCall(GetBinaryFactoryMethod(binary.Kind()),
                        SyntaxFactory.Argument(ConvertNodeToFactory(binary.Left, parameterVariables)),
                        SyntaxFactory.Argument(ConvertNodeToFactory(binary.Right, parameterVariables))
                    ),

                PrefixUnaryExpressionSyntax unary when unary.IsKind(SyntaxKind.LogicalNotExpression) =>
                    CreateFactoryCall("Not",
                        SyntaxFactory.Argument(ConvertNodeToFactory(unary.Operand, parameterVariables))
                    ),

                InvocationExpressionSyntax invocation =>
                   CreateFactoryCall("Call",
                       SyntaxFactory.Argument(ConvertNodeToFactory(
                           ((MemberAccessExpressionSyntax)invocation.Expression).Expression, parameterVariables)),
                       SyntaxFactory.Argument(CreateStringLiteral(
                           ((MemberAccessExpressionSyntax)invocation.Expression).Name.Identifier.ValueText)),
                       SyntaxFactory.Argument(SyntaxFactory.ParseExpression("Type.EmptyTypes")),
                       SyntaxFactory.Argument(SyntaxFactory.ArrayCreationExpression(
                            (ArrayTypeSyntax)SyntaxFactory.ParseTypeName("System.Linq.Expressions.Expression[]"),
                            SyntaxFactory.InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression,
                                SyntaxFactory.SeparatedList(
                                    invocation.ArgumentList.Arguments.Select(arg => ConvertNodeToFactory(arg.Expression, parameterVariables))
                                )
                            )
                        ))
                   ),

                _ => throw new NotSupportedException($"The syntax layout '{node.Kind()}' is not supported yet.")
            };
        }

        private ExpressionSyntax ResolveParameterIdentifier(IdentifierNameSyntax identifier, List<string> parameterVariables)
        {
            string name = identifier.Identifier.ValueText;
            string expectedVarName = $"param_{name}";

            if (parameterVariables.Contains(expectedVarName))
            {
                return SyntaxFactory.IdentifierName(expectedVarName);
            }

            return CreateFactoryCall("Constant", SyntaxFactory.Argument(identifier));
        }

        private ExpressionSyntax CreateFullyQualifiedExpressionNode()
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("System"),
                        SyntaxFactory.IdentifierName("Linq")
                    ),
                    SyntaxFactory.IdentifierName("Expressions")
                ),
                SyntaxFactory.IdentifierName("Expression")
            );
        }

        private InvocationExpressionSyntax CreateFactoryCall(string methodName, params ArgumentSyntax[] arguments)
        {
            // Build argument list with comma separator spaces
            var separatorList = new List<SyntaxToken>();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                separatorList.Add(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space));
            }

            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateFullyQualifiedExpressionNode(),
                    SyntaxFactory.IdentifierName(methodName)
                ),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    SyntaxFactory.SeparatedList(arguments, separatorList),
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken)
                )
            );
        }

        private LiteralExpressionSyntax CreateStringLiteral(string text) =>
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(text));

        private string GetBinaryFactoryMethod(SyntaxKind kind) => kind switch
        {
            // --- Arithmetic Operators ---
            SyntaxKind.AddExpression => "Add",
            SyntaxKind.SubtractExpression => "Subtract",
            SyntaxKind.MultiplyExpression => "Multiply",
            SyntaxKind.DivideExpression => "Divide",
            SyntaxKind.ModuloExpression => "Modulo",

            // --- Bitwise & Shift Operators ---
            SyntaxKind.BitwiseAndExpression => "And",
            SyntaxKind.BitwiseOrExpression => "Or",
            SyntaxKind.ExclusiveOrExpression => "ExclusiveOr",
            SyntaxKind.LeftShiftExpression => "LeftShift",
            SyntaxKind.RightShiftExpression => "RightShift",
            SyntaxKind.UnsignedRightShiftExpression => "UnsignedRightShift", // Introduced in .NET 7+ / C# 11

            // --- Logical Operators ---
            SyntaxKind.LogicalAndExpression => "AndAlso",
            SyntaxKind.LogicalOrExpression => "OrElse",

            // --- Relational & Comparison Operators ---
            SyntaxKind.EqualsExpression => "Equal",
            SyntaxKind.NotEqualsExpression => "NotEqual",
            SyntaxKind.GreaterThanExpression => "GreaterThan",
            SyntaxKind.LessThanExpression => "LessThan",
            SyntaxKind.GreaterThanOrEqualExpression => "GreaterThanOrEqual",
            SyntaxKind.LessThanOrEqualExpression => "LessThanOrEqual",

            // --- Special Types & Null Handling ---
            SyntaxKind.CoalesceExpression => "Coalesce", // Maps to: ??
            SyntaxKind.AsExpression => "TypeAs",   // Maps to: expr as Type

            // --- Fallback & Type Checking Operators ---
            SyntaxKind.IsExpression => "TypeIs",   // Maps to: expr is Type

            _ => throw new NotSupportedException($"Binary operator mapping for '{kind}' is unsupported or missing.")
        };

    }
}
