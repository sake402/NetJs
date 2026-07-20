using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    /// <summary>
    /// ref of field of a struct should reference the backing field array. eg "(byte*)&guid.a" should reference the backing field of guid offset properly on a
    /// </summary>
    public class PointerFromAddressOfStructCastToNumericSyntaxEmitter : SyntaxEmitter<CastExpressionSyntax>
    {
        public override bool TryEmit(CastExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Expression.IsKind(SyntaxKind.AddressOfExpression))
            {
                var addressOfTarget = ((PrefixUnaryExpressionSyntax)node.Expression).Operand;
                var fieldContainer = addressOfTarget;
                //If operand is &guid.a make sure fieldContainer is guid. if &guid => guid
                if (addressOfTarget.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    fieldContainer = ((MemberAccessExpressionSyntax)addressOfTarget).Expression;
                }
                var castToType = visitor.Global.TryGetTypeSymbol(node.Type, visitor);
                if (castToType?.IsPointer(out var pointedType) ?? false)
                {
                    if (pointedType.IsNumericType())
                    {
                        var addressed = visitor.Global.TryGetSymbol(addressOfTarget, visitor);
                        if (addressed != null)
                        {
                            var addressedType = visitor.Global.GetTypeSymbol(addressed);
                            if (addressed.Kind == SymbolKind.Field/*matches &guid.a*/ || visitor.Global.IsPureStructType(addressedType)/*matches &guid*/)
                            {
                                var structType = addressedType;
                                int fieldOffset = 0;
                                int fieldSize = -1;
                                if (addressed.Kind == SymbolKind.Field)
                                {
                                    structType = addressed.ContainingType;
                                    if (!visitor.IsFieldStructLayout(null, addressed, out fieldOffset, out fieldSize))
                                    {
                                        return false;
                                    }
                                }
                                if (visitor.Global.IsPureStructType(structType))
                                {
                                    if (addressed.Kind == SymbolKind.Local)
                                    {
                                        visitor.CurrentTypeWriter.InsertAbove(node, () =>
                                        {
                                            //local may not be initialized yet
                                            visitor.WriteMethodInvocation(node, "System.Runtime.CompilerServices.Unsafe.SkipInit", null, null, [structType], arguments: [new CodeNode(() => {
                                                visitor.WriteCreateObjectRefOrPointer(node, addressedType, addressOfTarget);
                                            })]);
                                            visitor.CurrentTypeWriter.Write(node, ";");
                                        }, true);
                                    }
                                    var getFieldsAsPointerMethod = (IMethodSymbol)visitor.Global.SystemObject.GetMembers("GetFieldRefOrPointer").Single();
                                    getFieldsAsPointerMethod = getFieldsAsPointerMethod.Construct(pointedType);
                                    visitor.WriteMethodInvocation(node, getFieldsAsPointerMethod, null, [
                                        new CodeNode(() => visitor.CurrentTypeWriter.Write(node, fieldOffset.ToString())),
                                        new CodeNode(() => visitor.CurrentTypeWriter.Write(node, "true")) //create pointer type
                                    ], addressOfTarget, null);
                                    //visitor.WriteCreateObjectRefOrPointer(node, pointedType, addressOfTarget, byteOffset: new CodeNode(() => visitor.CurrentTypeWriter.Write(node, fieldOffset.ToString())));
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
