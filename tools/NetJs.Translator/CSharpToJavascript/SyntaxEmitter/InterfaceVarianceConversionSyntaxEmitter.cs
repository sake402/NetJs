using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    sealed class InterfaceVarianceConversionSyntaxEmitter : SyntaxEmitter<ExpressionSyntax>
    {
        public static bool IsCovarianceInPlay(
            ExpressionSyntax expression,
            TranslatorSyntaxVisitor visitor,
            out ITypeSymbol fromType,
            out ITypeSymbol toType)
        {
            fromType = null!;
            toType = null!;

            // Grab the expression's actual compile-time source type (e.g., IEnumerable<string> or List<string>)
            TypeInfo typeInfo = default;
            foreach (var sm in visitor.SemanticModels)
            {
                if (sm.SyntaxTree == expression.SyntaxTree)
                {
                    typeInfo = sm.GetTypeInfo(expression);
                    break;
                }
            }
            var source = typeInfo.Type;
            if (source == null) return false;

            // Grab the target type the compiler is implicitly converting it to (e.g., IEnumerable<object>)
            var target = typeInfo.ConvertedType;
            if (target == null) return false;

            // Exact type matches mean zero variance shifting is taking place
            if (SymbolEqualityComparer.Default.Equals(source, target)) return false;

            // Covariance strictly acts on generic interfaces and delegates
            if (target is INamedTypeSymbol targetNamedType && targetNamedType.IsGenericType)
            {
                // Find the matching interface definition layout on our source type
                var matchingSourceInterface = source.AllInterfaces
                    .FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, targetNamedType.OriginalDefinition));

                // If the source type *is* already the interface itself, extract its casting form
                if (matchingSourceInterface == null && SymbolEqualityComparer.Default.Equals(source.OriginalDefinition, targetNamedType.OriginalDefinition))
                {
                    matchingSourceInterface = source as INamedTypeSymbol;
                }

                if (matchingSourceInterface != null)
                {
                    // Trace the type arguments looking for covariant ('out') markers
                    for (int i = 0; i < targetNamedType.TypeArguments.Length; i++)
                    {
                        var targetArg = targetNamedType.TypeArguments[i];
                        var sourceArg = matchingSourceInterface.TypeArguments[i];

                        // Check if the leaf arguments vary across the conversion boundary
                        if (!SymbolEqualityComparer.Default.Equals(targetArg, sourceArg))
                        {
                            var typeParamDefinition = targetNamedType.OriginalDefinition.TypeParameters[i];

                            // C# Covariance utilizes VarianceKind.Out
                            if (typeParamDefinition.Variance == VarianceKind.Out)
                            {
                                // C# variance strictly requires BOTH generic arguments to be reference types
                                if (targetArg.IsReferenceType && sourceArg.IsReferenceType)
                                {
                                    // Success! Populate the original concrete interface layout vs target blueprint
                                    fromType = matchingSourceInterface; // e.g., IEnumerable<string>
                                    toType = targetNamedType;           // e.g., IEnumerable<object>
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        public static bool IsContravarianceInPlay(
            ExpressionSyntax expression,
            TranslatorSyntaxVisitor visitor,
            out ITypeSymbol fromType,
            out ITypeSymbol toType)
        {
            fromType = null!;
            toType = null!;

            // Get the expression's actual compile-time source type
            TypeInfo typeInfo = default;
            foreach (var sm in visitor.SemanticModels)
            {
                if (sm.SyntaxTree == expression.SyntaxTree)
                {
                    typeInfo = sm.GetTypeInfo(expression);
                    break;
                }
            }
            var source = typeInfo.Type;
            if (source == null) return false;

            // Get the converted type (what the compiler implicitly converts it to)
            var target = typeInfo.ConvertedType;
            if (target == null) return false;

            // If the types match exactly, no variance conversion is happening
            if (SymbolEqualityComparer.Default.Equals(source, target)) return false;

            // Variance only applies to generic interfaces and delegates
            if (target is INamedTypeSymbol targetNamedType && targetNamedType.IsGenericType)
            {
                // Find the matching interface definition on the source type
                var matchingSourceInterface = source.AllInterfaces
                    .FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, targetNamedType.OriginalDefinition));

                // If the source doesn't implement it, check if the source itself is the interface
                if (matchingSourceInterface == null && SymbolEqualityComparer.Default.Equals(source.OriginalDefinition, targetNamedType.OriginalDefinition))
                {
                    matchingSourceInterface = source as INamedTypeSymbol;
                }

                if (matchingSourceInterface != null)
                {
                    // Inspect the type parameters for contravariant ('in') markers
                    for (int i = 0; i < targetNamedType.TypeArguments.Length; i++)
                    {
                        var targetArg = targetNamedType.TypeArguments[i];
                        var sourceArg = matchingSourceInterface.TypeArguments[i];

                        // If the specific type arguments differ, check the definition's variance rules
                        if (!SymbolEqualityComparer.Default.Equals(targetArg, sourceArg))
                        {
                            var typeParamDefinition = targetNamedType.OriginalDefinition.TypeParameters[i];

                            if (typeParamDefinition.Variance == VarianceKind.In)
                            {
                                // C# variance strictly requires BOTH generic arguments to be reference types
                                if (targetArg.IsReferenceType && sourceArg.IsReferenceType)
                                {
                                    // Contravariance detected! Output the structural types involved
                                    fromType = matchingSourceInterface; // e.g., IEqualityComparer<object>
                                    toType = targetNamedType;           // e.g., IEqualityComparer<string>
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        public override bool TryEmit(ExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (_processing.Value!.Contains(node))
                return false;
            if (IsContravarianceInPlay(node, visitor, out var fromType, out var toType))
            {
                _processing.Value.Push(node);
                try
                {
                    visitor.WriteMethodInvocation(node, "NetJs.InterfaceVarianceStubHandler.Create", arguments: [
                        node,
                        new CodeNode(()=>visitor.CurrentTypeWriter.Write(node, fromType.ComputeOutputTypeName(visitor.Global))),
                        new CodeNode(()=>visitor.CurrentTypeWriter.Write(node, toType.ComputeOutputTypeName(visitor.Global))),
                    ]);
                }
                finally
                {
                    _processing.Value.Pop();
                }
                return true;
            }
            else if (IsCovarianceInPlay(node, visitor, out var fromType2, out var toType2))
            {
                _processing.Value.Push(node);
                try
                {
                    visitor.WriteMethodInvocation(node, "NetJs.InterfaceVarianceStubHandler.Create", arguments: [
                        node,
                        new CodeNode(()=>visitor.CurrentTypeWriter.Write(node, fromType2.ComputeOutputTypeName(visitor.Global))),
                        new CodeNode(()=>visitor.CurrentTypeWriter.Write(node, toType2.ComputeOutputTypeName(visitor.Global))),
                    ]);
                }
                finally
                {
                    _processing.Value.Pop();
                }
                return true;
            }
            return false;
        }
    }
}
