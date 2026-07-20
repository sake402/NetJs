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
    public partial class TranslatorSyntaxVisitor
    {
        bool TryCastUsingExternalInterface(CSharpSyntaxNode node, ITypeSymbol? lhsType, ITypeSymbol? rhsType, CSharpSyntaxNode rhsExpression)
        {
            //if we are trying to assign an Extern type like native js Array to a dotnet type like IEnumerable. Let the runtime cast system handle the conversion
            //if (lhsType is ITypeSymbol lt && rhsType is ITypeSymbol rt && rhsType.CanConvertTo(lhsType, _global, null, out _) < 0)
            //{
            //if we are trying to assign an Extern type like native js Array to a dotnet type like IEnumerable
            INamedTypeSymbol? castHandler = null;
            TypeArgumentListSyntax? castGenericArguments = null;
            if (lhsType != null &&
                rhsType != null &&
                lhsType.SpecialType != SpecialType.System_Object &&
                !lhsType.Equals(_global.Compilation.DynamicType, SymbolEqualityComparer.Default) &&
                !lhsType.Equals(rhsType, SymbolEqualityComparer.Default) &&
                _global.HasAttribute(rhsType, typeof(ExternalInterfaceImplementationAttribute).FullName!, this, true, out var constructorsArgs))
            {
                var castType = (INamedTypeSymbol)constructorsArgs![0]!;
                if (castType.IsUnboundGenericType)
                {
                    rhsType.IsArray(out var elementType);
                    if (elementType != null)
                    {
                        castType = castType.OriginalDefinition.Construct(elementType);
                    }
                }
                if (castType.CanConvertTo(lhsType, _global, null, out _) > 0)
                {
                    castHandler = castType;
                    //var systemObject = (ITypeSymbol)_global.GetTypeSymbol("System.Object", this, out _, out _);
                    //castHandler = castHandler.OriginalDefinition.Construct(Enumerable.Range(1, castHandler.Arity).Select(i => systemObject).ToArray());
                    //castGenericArguments = SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(Enumerable.Range(1, castHandler.Arity).Select(e => SyntaxFactory.ParseTypeName("object"))));
                }
            }
            if (castHandler != null)
            {
                WriteConstructorCall(node, castHandler, castHandler.GetMembers(".ctor").Cast<IMethodSymbol>().First(), castGenericArguments, [rhsExpression as ArgumentSyntax ?? SyntaxFactory.Argument((ExpressionSyntax)rhsExpression)]);
                return true;
            }
            return false;
        }

        public override void VisitCastExpression(CastExpressionSyntax node)
        {
            var castToType = _global.GetTypeSymbol(node.Type, this);
            var parentIsEnum = node.FindClosestParent<EnumDeclarationSyntax>();
            //Don't generate cast for enum value initialization
            //var type = _global.GetSymbol(node.Type, this/*, out _, out _*/);
            if (parentIsEnum != null ||
                node.Expression.IsKind(SyntaxKind.NullLiteralExpression) ||
                !_global.ShouldExportType(castToType, this) ||
                SymbolEqualityComparer.Default.Equals(castToType, _global.Compilation.DynamicType))
            {
                Visit(node.Expression);
            }
            else
            {
                var fromType = _global.TryGetTypeSymbol(node.Expression, this);

                //Casting a type to its implicit type is unneccessary at runtime
                if (fromType != null &&
                    SymbolEqualityComparer.Default.Equals(fromType, castToType))
                {
                    Visit(node.Expression);
                    return;
                }

                //Casting a type to a base it inherits from or interface it already implements is unneccessary at runtime
                if (fromType != null &&
                    !fromType.IsJsPrimitive() &&
                    (SymbolEqualityComparer.Default.Equals(fromType.BaseType, castToType) || fromType.AllInterfaces.Any(it => SymbolEqualityComparer.Default.Equals(it, castToType))))
                {
                    Visit(node.Expression);
                    return;
                }

                var numFromType = fromType?.TypeKind == TypeKind.Enum ? ((INamedTypeSymbol)fromType).EnumUnderlyingType : fromType;
                if (numFromType != null &&
                    //castToType != null &&
                    ((numFromType.IsIntegerNumericType() && castToType.IsIntegerNumericType()) ||
                    (numFromType.IsLongNumericType() && castToType.IsLongNumericType())))
                {
                    //this type can just be assigned, no cast/conversion neccessary in js
                    int fromRank = numFromType.GetNumericRangeRank();
                    int toRank = castToType.GetNumericRangeRank();
                    if (numFromType.IsSignedNumericType() == castToType.IsSignedNumericType())
                    {
                        if (toRank >= fromRank)
                        {
                            Visit(node.Expression);
                            return;
                        }
                    }
                    else if (toRank == fromRank &&
                        (SymbolEqualityComparer.Default.Equals(castToType, _global.SystemInt32) || SymbolEqualityComparer.Default.Equals(numFromType, _global.SystemInt32))) //same rank but differ in signess. eg int and uint
                    {
                        var toMask = castToType.GetNumericMask();
                        var fromMask = numFromType.GetNumericMask();
                        bool useMask = toMask != fromMask;
                        CurrentTypeWriter.Write(node, "(");
                        Visit(node.Expression);
                        if (castToType.IsSignedNumericType())
                        {
                            CurrentTypeWriter.Write(node, " | 0)");
                        }
                        else
                        {
                            CurrentTypeWriter.Write(node, " >>> 0)");
                        }
                        return;
                    }
                }

                if (TryInvokeMethodOperator(node, ExplicitOperatorName, null, node.Type, null, [node.Expression]))
                    return;
                if (TryCastUsingExternalInterface(node, castToType, fromType, node.Expression))
                    return;
                //if ((toType?.Equals(_global.Compilation.ObjectType, SymbolEqualityComparer.Default) ?? false) && fromType != null && !fromType.IsValueType)
                //{
                //cast reference type to object can just be assigned, no cast/conversion neccessary in js
                //if (fromType != null && toType != null && fromType.CanConvertTo(toType, _global, null, out _) > 0)
                //{
                //    Visit(node.Expression);
                //    return;
                //}
                EnsureImported(node.Type);
                if (
                    //castToType != null && 
                    fromType != null && 
                    NeedBoxing(castToType, fromType))
                {
                    var metadata = fromType.Kind != SymbolKind.TypeParameter ? _global.GetRequiredMetadata(fromType) : null;
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.BoxName}(");
                    Visit(node.Expression);
                    CurrentTypeWriter.Write(node, ", ");
                    CurrentTypeWriter.Write(node, metadata?.InvocationName ?? fromType.ComputeOutputTypeName(_global));
                    //Visit(node.Type);
                    CurrentTypeWriter.Write(node, ")");
                }
                else
                {
                    var metadata = _global.GetMetadata(castToType);
                    CurrentTypeWriter.Write(node, $"{_global.GlobalName}.{Constants.CastName}(");
                    Visit(node.Expression);
                    CurrentTypeWriter.Write(node, ", ");
                    //if (node.Type.ToString() == "delegate* unmanaged[Cdecl]<void>")
                    //{

                    //}
                    CurrentTypeWriter.Write(node, metadata?.InvocationName ?? castToType.ComputeOutputTypeName(_global));
                    //Visit(node.Type);
                    if (fromType?.TypeKind == TypeKind.TypeParameter) //if casting from a generic parameter to Object, let the runtime decide if it need to box it into this T or not
                    {
                        CurrentTypeWriter.Write(node, ", ");
                        CurrentTypeWriter.Write(node, fromType.Name);
                    }
                    CurrentTypeWriter.Write(node, ")");
                }
            }
            //base.VisitCastExpression(node);
        }

    }
}
