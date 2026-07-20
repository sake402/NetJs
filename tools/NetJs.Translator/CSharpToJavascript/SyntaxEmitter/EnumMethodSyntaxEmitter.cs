using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    //ENum.Field.ToString() should be inlined to System.Int32.$type nameof(Field)
    sealed class EnumMethodSyntaxEmitter : SyntaxEmitter<InvocationExpressionSyntax>
    {
        public override bool TryEmit(InvocationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var memberAccess = (MemberAccessExpressionSyntax)node.Expression;
                var lhs = visitor.Global.GetSymbol(memberAccess.Expression, visitor);
                var lhsType = visitor.Global.GetTypeSymbol(lhs);
                var methodSymbol = visitor.Global.GetSymbol(node, visitor) as IMethodSymbol;
                if (methodSymbol != null)
                {
                    if (lhsType.TypeKind == TypeKind.Enum ||
                        (lhsType.TypeKind == TypeKind.TypeParameter && ((ITypeParameterSymbol)lhsType).ConstraintTypes.Any(c => SymbolEqualityComparer.Default.Equals(c, visitor.Global.SystemEnum))))
                    {
                        if (lhsType.TypeKind == TypeKind.Enum &&//TODO: handle TypeParameter with enum constraint
                            methodSymbol.Name == "ToString" &&
                            methodSymbol.Parameters.Length == 0 &&
                            SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, visitor.Global.SystemEnum) &&
                            SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, visitor.Global.SystemString))
                        {
                            if (lhs.Kind == SymbolKind.Field && ((IFieldSymbol)lhs).HasConstantValue)
                            {
                                visitor.CurrentTypeWriter.Write(node, "\"");
                                visitor.CurrentTypeWriter.Write(node, lhs.Name);
                                visitor.CurrentTypeWriter.Write(node, "\"");
                            }
                            else
                            {
                                //Write Enum.ToStringT<TEnum, TStorage>(TStorage value)
                                var enumToString = (IMethodSymbol)visitor.Global.SystemEnum
                                    .GetMembers("ToStringT")
                                    .Single(e => e is IMethodSymbol ms && ms.TypeParameters.Length == 2 && ms.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, ms.TypeParameters[1]));
                                //Map signed underlying type to unsigned type
                                var underlyingType = ((INamedTypeSymbol)lhsType).EnumUnderlyingType!.SpecialType switch
                                {
                                    SpecialType.System_SByte => visitor.Global.SystemByte,
                                    SpecialType.System_Int16 => visitor.Global.SystemUInt16,
                                    SpecialType.System_Int32 => visitor.Global.SystemUInt32,
                                    SpecialType.System_Int64 => visitor.Global.SystemUInt64,
                                    _ => ((INamedTypeSymbol)lhsType).EnumUnderlyingType!
                                };
                                enumToString = enumToString.Construct(lhsType, underlyingType);
                                visitor.WriteMethodInvocation(node, enumToString, null, [new CodeNode(() => {
                                    visitor.Visit(memberAccess.Expression);
                                })], null, null);
                            }
                            return true;
                        }
                        else if (methodSymbol.Name == "GetHashCode" &&
                            methodSymbol.Parameters.Length == 0 &&
                            SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, visitor.Global.SystemEnum) &&
                            SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, visitor.Global.SystemInt32))
                        {
                            if (lhsType.TypeKind == TypeKind.TypeParameter)
                            {
                                //Write Enum.GetHashCodeT<TStorage>(TStorage enumValue)
                                var enumGetHashCode = (IMethodSymbol)visitor.Global.SystemEnum
                                    .GetMembers("GetHashCodeT")
                                    .Single(e => e is IMethodSymbol ms && ms.TypeParameters.Length == 1 && ms.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, ms.TypeParameters[0]));
                                enumGetHashCode = enumGetHashCode.Construct(lhsType);
                                visitor.WriteMethodInvocation(node, enumGetHashCode, null, [new CodeNode(() => {
                                    visitor.Visit(memberAccess.Expression);
                                })], null, null);
                            }
                            else
                            {
                                var underlyingType = ((INamedTypeSymbol)lhsType).EnumUnderlyingType!;
                                if (underlyingType.IsLongNumericType())
                                {
                                    visitor.CurrentTypeWriter.Write(node, $"{visitor.Global.GlobalName}.{Constants.CastName}(");
                                    visitor.Visit(memberAccess.Expression);
                                    visitor.CurrentTypeWriter.Write(node, $", ");
                                    visitor.CurrentTypeWriter.Write(node, visitor.Global.SystemInt32.ComputeOutputTypeName(visitor.Global));
                                    visitor.CurrentTypeWriter.Write(node, $") ^ ");
                                    visitor.CurrentTypeWriter.Write(node, $"{visitor.Global.GlobalName}.{Constants.CastName}(");
                                    visitor.Visit(memberAccess.Expression);
                                    visitor.CurrentTypeWriter.Write(node, $" >> 32n, ");
                                    visitor.CurrentTypeWriter.Write(node, visitor.Global.SystemInt32.ComputeOutputTypeName(visitor.Global));
                                    visitor.CurrentTypeWriter.Write(node, $")");
                                }
                                else if (underlyingType.IsUnsignedNumericType())
                                {
                                    visitor.CurrentTypeWriter.Write(node, $"{visitor.Global.GlobalName}.{Constants.CastName}(");
                                    visitor.Visit(memberAccess.Expression);
                                    visitor.CurrentTypeWriter.Write(node, $", ");
                                    visitor.CurrentTypeWriter.Write(node, visitor.Global.SystemInt32.ComputeOutputTypeName(visitor.Global));
                                    visitor.CurrentTypeWriter.Write(node, $")");
                                }
                                else
                                {
                                    visitor.Visit(memberAccess.Expression);
                                }
                            }
                            return true;
                        }
                        else if (methodSymbol.Name == "CompareTo" &&
                            methodSymbol.Parameters.Length == 1 &&
                            SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, visitor.Global.SystemEnum) &&
                            SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, visitor.Global.SystemInt32))
                        {
                            //Write Enum.CompareToT<TStorage>(TStorage enumValue1, TStorage enumValue2)
                            var enumCompare = (IMethodSymbol)visitor.Global.SystemEnum
                                .GetMembers("CompareToT")
                                .Single(e => e is IMethodSymbol ms && ms.TypeParameters.Length == 1 && ms.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(ms.Parameters[0].Type, ms.TypeParameters[0]) && SymbolEqualityComparer.Default.Equals(ms.Parameters[1].Type, ms.TypeParameters[0]));
                            enumCompare = enumCompare.Construct(lhsType);
                            visitor.WriteMethodInvocation(node, enumCompare, null, [new CodeNode(() => {
                                visitor.Visit(memberAccess.Expression);
                            }), new CodeNode(() => {
                                using (BoxPrimitiveAssignmentSyntaxEmitter.Disable(node.ArgumentList.Arguments[0].Expression))
                                {
                                    visitor.Visit(node.ArgumentList.Arguments[0].Expression);
                                }
                            })], null, null);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
