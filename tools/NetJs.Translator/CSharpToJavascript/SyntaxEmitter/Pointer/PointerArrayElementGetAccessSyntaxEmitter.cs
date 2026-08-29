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
                var symbol = visitor.Global.GetSymbol(node.Expression, visitor);
                if (!visitor.Global.IsFixedSizeField(symbol, out _, out _))
                {
                    var type = visitor.Global.GetTypeSymbol(symbol);
                    if (type.IsPointer(out var pointedType))
                    {
                        var argType = visitor.Global.GetTypeSymbol(node.ArgumentList.Arguments[0], visitor);
                        bool isGet = node.IsReadOnlyOperation();
                        if (isGet)
                        {
                            visitor.Visit(node.Expression);
                            visitor.CurrentTypeWriter.Write(node, ".");
                            visitor.WriteMemberName(node, visitor.Global.SystemPointer, "GetAt");
                            visitor.CurrentTypeWriter.Write(node, "(");
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
            }
            return false;
        }
    }
}
