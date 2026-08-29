using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.Pointer
{
    //Handles expression like pointer[2] = value where pointer is a pointer type
    sealed class PointerArrayElementSetAccessSyntaxEmitter : SyntaxEmitter<AssignmentExpressionSyntax>
    {
        //TODO: Rewrite this to use GetAt or SetAt depending on whether being read or assigned
        //Current implementation do both automagically, but it allocate a temp reference on heap(returned by get_Item) and slower
        public override bool TryEmit(AssignmentExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.Left is ElementAccessExpressionSyntax elementAccess)
            {
                if (elementAccess.ArgumentList.Arguments.Count == 1)
                {
                    var symbol = visitor.Global.GetSymbol(elementAccess.Expression, visitor);
                    if (!visitor.Global.IsFixedSizeField(symbol, out _, out _))
                    {
                        var type = visitor.Global.GetTypeSymbol(symbol);
                        if (type.IsPointer(out _))
                        {
                            var argType = visitor.Global.GetTypeSymbol(elementAccess.ArgumentList.Arguments[0], visitor);
                            visitor.Visit(elementAccess.Expression);
                            visitor.CurrentTypeWriter.Write(node, ".");
                            visitor.WriteMemberName(node, visitor.Global.SystemPointer, "SetAt");
                            visitor.CurrentTypeWriter.Write(node, "(");
                            visitor.Visit(node.Right);
                            visitor.CurrentTypeWriter.Write(node, ", ");
                            if (argType.IsLongNumericType())
                            {
                                visitor.CurrentTypeWriter.Write(node, "Number(");
                            }
                            visitor.Visit(elementAccess.ArgumentList.Arguments[0]);
                            if (argType.IsLongNumericType())
                            {
                                visitor.CurrentTypeWriter.Write(node, ")");
                            }
                            visitor.CurrentTypeWriter.Write(node, ")");
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
