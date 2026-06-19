using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter.QCall
{
    /// <summary>
    /// Skips creation of StringHandleOnStack, ObjectHandleOnStack, ByteRef, ByteRefOnStack, StackCrawlMarkHandle, QCallModule, QCallAssembly and QCallTypeHandle
    /// Pass the refed parameter directly
    /// </summary>
    sealed class SkipCreateQCallSyntaxEmitter : SyntaxEmitter<ObjectCreationExpressionSyntax>
    {
        public override bool TryEmit(ObjectCreationExpressionSyntax node, TranslatorSyntaxVisitor visitor)
        {
            if (node.ArgumentList?.Arguments.Count == 1)
            {
                var type = visitor.Global.TryGetTypeSymbol(node.Type, visitor);
                if (type != null && (type.IsType("System.Runtime.CompilerServices.StringHandleOnStack") ||
                                      type.IsType("System.Runtime.CompilerServices.ObjectHandleOnStack") ||
                                      type.IsType("System.Runtime.CompilerServices.ByteRef") ||
                                      type.IsType("System.Runtime.CompilerServices.ByteRefOnStack") ||
                                      type.IsType("System.Runtime.CompilerServices.StackCrawlMarkHandle") ||
                                      type.IsType("System.Runtime.CompilerServices.QCallModule") ||
                                      type.IsType("System.Runtime.CompilerServices.QCallAssembly") ||
                                      type.IsType("System.Runtime.CompilerServices.QCallTypeHandle")))
                {
                    var arg = node.ArgumentList.Arguments[0];
                    if (type.IsType("System.Runtime.CompilerServices.ObjectHandleOnStack"))
                    {
                        visitor.Visit(arg);
                        return true;
                    }
                    else if (arg.RefOrOutKeyword.Text.Length > 0)
                    {
                        visitor.Visit(arg.Expression);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
