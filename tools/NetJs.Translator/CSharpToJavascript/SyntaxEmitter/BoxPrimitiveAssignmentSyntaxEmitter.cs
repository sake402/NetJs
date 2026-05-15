using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace NetJs.Translator.CSharpToJavascript.SyntaxEmitter
{
    /// <summary>
    /// Provides a syntax emitter that handles assignment expressions where a JS primitive is assigned to an
    /// interface it implements in C#. eg IEnumerable enumerable = "foo" or object e = "foo";
    /// All we need is to box the js primitive into the .net type
    /// </summary>
    sealed class BoxPrimitiveAssignmentSyntaxEmitter : SyntaxEmitter<CSharpSyntaxNode>
    {
        static List<CSharpSyntaxNode?> _disabled = new List<CSharpSyntaxNode?>();
        public static IDisposable Disable(CSharpSyntaxNode? node)
        {
            _disabled.Add(node);
            return new DelegateDispose(() => _disabled.Remove(node));
        }
        Stack<CSharpSyntaxNode> _processing = new Stack<CSharpSyntaxNode>();
        public override bool TryEmit(CSharpSyntaxNode node, TranslatorSyntaxVisitor visitor)
        {
            if (_disabled.Contains(node))
                return false;
            if (_processing.TryPeek(out var top) && top == node)
                return false;
            foreach (var sm in visitor.SemanticModels)
            {
                if (node.SyntaxTree == sm.SyntaxTree)
                {
                    var conversion = sm.GetConversion(node);
                    if (conversion.Exists &&
                        conversion.IsImplicit &&
                        conversion.IsReference)
                    {
                        var thisOperation = sm.GetOperation(node);
                        var operation = thisOperation?.Parent as IConversionOperation;
                        var fromType = thisOperation?.Type;
                        var toType = operation?.Type;
                        if (thisOperation != null &&
                            operation != null &&
                            fromType != null &&
                            toType != null &&
                            visitor.NeedBoxing(toType, fromType)
                            ///*Box type parameter too(if converted to object or interface) as the T at runtime type may be a value type. The runtime type will check if boxing is really neccessary */
                            //((fromType?.IsJsPrimitive() ?? false) || fromType?.TypeKind == TypeKind.TypeParameter) &&
                            //!fromType.IsAbstract &&
                            //(toType?.TypeKind == TypeKind.Interface || SymbolEqualityComparer.Default.Equals(toType, visitor.Global.SystemObject))
                            )
                        {
                            //If we are passing this as a parameter to an external js code, dont box
                            if (SymbolEqualityComparer.Default.Equals(toType, visitor.Global.SystemObject))
                            {
                                var invocation = thisOperation.FindClosestParent<IInvocationOperation>();
                                if (invocation != null && invocation.TargetMethod.IsExtern)
                                {
                                    return false;
                                }
                            }
                            _processing.Push(node);
                            try
                            {
                                visitor.CurrentTypeWriter.Write(node, visitor.Global.GlobalName);
                                visitor.CurrentTypeWriter.Write(node, ".");
                                visitor.CurrentTypeWriter.Write(node, Constants.BoxName);
                                visitor.CurrentTypeWriter.Write(node, "(");
                                visitor.Visit(node);
                                visitor.CurrentTypeWriter.Write(node, ", ");
                                visitor.CurrentTypeWriter.Write(node, fromType.ComputeOutputTypeName(visitor.Global));
                                visitor.CurrentTypeWriter.Write(node, ")");
                                return true;
                            }
                            finally
                            {
                                _processing.Pop();
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
