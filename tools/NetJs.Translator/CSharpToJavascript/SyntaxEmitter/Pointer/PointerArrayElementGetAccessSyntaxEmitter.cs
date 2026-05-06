using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like pointer[2] where pointer is a pointer type
    sealed class PointerArrayElementGetAccessSyntaxEmitter : SyntaxEmitter<ElementAccessExpressionSyntax>
    {
        public override bool TryEmit(ElementAccessExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.ArgumentList.Arguments.Count == 1)
            {
                var type = visitor.Global.GetTypeSymbol(node.Expression, visitor).GetTypeSymbol();
                if (type.IsPointer(out var pointedType))
                {
                    var argType = visitor.Global.GetTypeSymbol(node.ArgumentList.Arguments[0], visitor).GetTypeSymbol();
                    bool isGet = node.IsReadOnlyOperation();
                    if (isGet)
                    {
                        visitor.Visit(node.Expression);
                        visitor.CurrentTypeWriter.Write(node, ".GetAt(");
                        if (argType.IsLongNumericType())
                        {
                            visitor.CurrentTypeWriter.Write(node, "Number(");
                        }
                        visitor.Visit(node.ArgumentList.Arguments[0]);
                        if (argType.IsLongNumericType())
                        {
                            visitor.CurrentTypeWriter.Write(node, ")");
                        }
                        visitor.CurrentTypeWriter.Write(node, ")");
                    }
                    else
                    {
                        visitor.Visit(node.Expression);
                        visitor.CurrentTypeWriter.Write(node, ".get_Item(");
                        if (argType.IsLongNumericType())
                        {
                            visitor.CurrentTypeWriter.Write(node, "Number(");
                        }
                        visitor.Visit(node.ArgumentList.Arguments[0]);
                        if (argType.IsLongNumericType())
                        {
                            visitor.CurrentTypeWriter.Write(node, ")");
                        }
                        visitor.CurrentTypeWriter.Write(node, ")");
                        visitor.TryDereference(node);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
