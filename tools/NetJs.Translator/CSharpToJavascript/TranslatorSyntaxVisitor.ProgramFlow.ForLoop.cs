using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        void WriteForEach(CommonForEachStatementSyntax node, object variable)
        {
            OpenClosure(node);
            var enumerableTypeSymbol = (INamedTypeSymbol)_global.GetTypeSymbol("System.Collections.Generic.IEnumerable<>", this/*, out _, out _*/);
            var enumerableGetEnumerator = (IMethodSymbol)(enumerableTypeSymbol.GetMembers("GetEnumerator", _global).First());
            var enumerableGetEnumeratorMethodMetadata = _global.GetRequiredMetadata(enumerableGetEnumerator);
            string getEnumeratorInvocationName = enumerableGetEnumeratorMethodMetadata.InvocationName ?? enumerableGetEnumerator.Name;

            var enumerationRhsType = GetExpressionReturnSymbol(node.Expression);
            var enumerationTargetRhsTypeSymbol = _global.ResolveSymbol(enumerationRhsType, this/*, out _, out _*/)?.GetTypeSymbol();

            ITypeSymbol? enumerableItemSymbol = null;
            string? enumeratorMoveNextInvocationName = null;
            string? enumeratorCurrentInvocationName = null;

            IMethodSymbol? directGetEnumerator = null;

            IMethodSymbol? enumeratorMoveNext = null;
            IPropertySymbol? enumeratorCurrent = null;

            if (enumerationTargetRhsTypeSymbol != null)
            {
                //enumerableTypeSymbol = enumerableTypeSymbol.Construct([enumerationTargetRhsTypeSymbol]);
                //If we can get the enumerator directly on the target, we dont need to call the interface method
                //This is espcially useful if the target doesn't actually implement the IEnumerable Interface
                directGetEnumerator = enumerationTargetRhsTypeSymbol.TypeKind != TypeKind.Interface ?
                   (IMethodSymbol?)(enumerationTargetRhsTypeSymbol.GetMembers("GetEnumerator", _global).FirstOrDefault()) : null;
                if (directGetEnumerator != null)
                {
                    //var directGetENumeratorMetadata = _global.GetRequiredMetadata(directGetEnumerator);
                    //getEnumeratorInvocationName = directGetENumeratorMetadata.InvocationName ?? directGetEnumerator.Name;
                    getEnumeratorInvocationName = "GetEnumerator";
                    enumeratorMoveNextInvocationName = "MoveNext";
                    enumeratorCurrentInvocationName = "Current";
                    var enumeratorType = directGetEnumerator.GetTypeSymbol();
                    enumeratorMoveNext = (IMethodSymbol)(enumeratorType.GetMembers("MoveNext", _global).FirstOrDefault());
                    enumeratorCurrent = (IPropertySymbol)(enumeratorType.GetMembers("Current", _global).FirstOrDefault());
                }
                else
                {
                    if (enumerationTargetRhsTypeSymbol.IsArray(out var elementType))
                    {
                        enumerableItemSymbol = elementType;
                    }
                    else if (!enumerationTargetRhsTypeSymbol.IsEnumerable(out _) && !enumerationTargetRhsTypeSymbol.IsEnumerable())
                    {
                        var ienumerable = enumerationTargetRhsTypeSymbol.AllInterfaces.FirstOrDefault(o => o.IsEnumerable(out _)) ??
                            enumerationTargetRhsTypeSymbol.AllInterfaces.First(o => o.IsEnumerable());
                        enumerableItemSymbol = ienumerable.TypeArguments.FirstOrDefault() ?? _global.Compilation.ObjectType;
                    }
                    else if (enumerationTargetRhsTypeSymbol.IsEnumerable())
                    {
                        enumerableItemSymbol = _global.Compilation.ObjectType;
                    }
                    else
                    {
                        enumerableItemSymbol = ((INamedTypeSymbol)enumerationTargetRhsTypeSymbol).TypeArguments[0];
                    }
                    enumerableTypeSymbol = enumerableTypeSymbol.Construct([enumerableItemSymbol]);

                    var enumeratorSymbol = (INamedTypeSymbol)_global.GetTypeSymbol("System.Collections.Generic.IEnumerator<>", this/*, out _, out _*/)!.GetTypeSymbol();
                    if (enumerableItemSymbol != null)
                    {
                        enumeratorSymbol = enumeratorSymbol.Construct([enumerableItemSymbol]);
                    }
                    enumeratorMoveNext = (IMethodSymbol)(enumeratorSymbol.GetMembers("MoveNext", _global).First());
                    var enumeratorMoveNextMethodMetadata = _global.GetRequiredMetadata(enumeratorMoveNext);
                    enumeratorMoveNextInvocationName = enumeratorMoveNextMethodMetadata.InvocationName ?? enumeratorMoveNext.Name;

                    enumeratorCurrent = (IPropertySymbol)(enumeratorSymbol.GetMembers("Current", _global).First());
                    var enumeratorCurrentMethodMetadata = _global.GetRequiredMetadata(enumeratorCurrent);
                    enumeratorCurrentInvocationName = enumeratorCurrentMethodMetadata.InvocationName ?? enumeratorCurrent.Name;

                }
                var enumeratorVariableSymbol = _global.TryGetTypeSymbol(node, this/*, out _, out _*/) as ILocalSymbol;
                if (enumeratorVariableSymbol != null)
                {
                    if (variable is SyntaxToken identifierName)
                        CurrentClosure.DefineIdentifierType(identifierName.ValueText, CodeSymbol.From(enumeratorVariableSymbol));
                }
            }


            var enumarableName = $"$en{CurrentTypeWriter.ClosureDepth}";
            CurrentTypeWriter.Write(node, $"var {enumarableName} = ", true);

            if (directGetEnumerator != null)
            {
                WriteMethodInvocation(node, directGetEnumerator, null, null, node.Expression, enumerableTypeSymbol);
                CurrentTypeWriter.WriteLine(node, $";");
            }
            else
            {
                WriteVariableAssignment(node, null, enumerableTypeSymbol, null, node.Expression, enumerationTargetRhsTypeSymbol);
                //Visit(node.Expression);
                CurrentTypeWriter.WriteLine(node, $".{getEnumeratorInvocationName}();");
            }

            CurrentTypeWriter.WriteLine(node, $"while ({enumarableName}.{enumeratorMoveNextInvocationName}())", true);
            //if (!node.Statement.IsKind(SyntaxKind.Block))
            CurrentTypeWriter.WriteLine(node, "{", true);
            if (variable is SyntaxToken identifierName2)
            {
                CurrentTypeWriter.WriteLine(node, $"var {identifierName2.ValueText} = {enumarableName}.{enumeratorCurrentInvocationName}{((enumeratorCurrent?.GetRefKind() ?? RefKind.None) != RefKind.None ? $".{Constants.RefValueName}" : "")};", true);
            }
            else if (variable is TupleExpressionSyntax tp)
            {
                var i = ++CurrentTypeWriter.CurrentClosure.NameManglingSeed;
                CurrentTypeWriter.WriteLine(node, $"var $t{i} = {enumarableName}.{enumeratorCurrentInvocationName};", true);
                int item = 0;
                foreach (var t in tp.Arguments)
                {
                    item++;
                    if (t.Expression.IsKind(SyntaxKind.DiscardPattern))
                        continue;
                    if (t.Expression is IdentifierNameSyntax id && id.Identifier.ValueText == "_")
                        continue;
                    CurrentTypeWriter.Write(t.Expression, "", true);
                    Visit(t.Expression);
                    CurrentTypeWriter.WriteLine(node, $" = $t{i}.Item{item};");
                }
            }
            Visit(node.Statement);
            //if (!node.Statement.IsKind(SyntaxKind.Block))
            CurrentTypeWriter.WriteLine(node, "}", true);
            CloseClosure();
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            WriteForEach(node, node.Identifier);
            //base.VisitForEachStatement(node);
        }

        public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
        {
            WriteForEach(node, node.Variable);
            //base.VisitForEachVariableStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            ExpressionSyntax? rewittenCondition = null;
            if (node.Condition != null)
            {
                bool conditionIsAlwaysFalse = _global.EvaluateConditionalExpressionAsConstant(node.Condition, this, out rewittenCondition) == false;
                if (conditionIsAlwaysFalse)
                {
                    CurrentTypeWriter.WriteLine(node, $"//for {node.Condition.ToString().Replace("\r", "").Replace("\n", "")} {{ ... }}", true);
                    return;
                }
            }
            OpenClosure(node);
            CurrentTypeWriter.Write(node, "for(", true);
            if (node.Declaration != null)
                Visit(node.Declaration);
            else if (node.Initializers.Count > 0)
            {
                int ix = 0;
                foreach (var init in node.Initializers)
                {
                    if (ix > 0)
                        CurrentTypeWriter.Write(node, ", ");
                    Visit(init);
                    ix++;
                }
            }
            CurrentTypeWriter.Write(node, "; ");
            if (node.Condition != null)
                Visit(rewittenCondition ?? node.Condition);
            CurrentTypeWriter.Write(node, "; ");
            int i = 0;
            foreach (var inc in node.Incrementors)
            {
                if (i > 0)
                    CurrentTypeWriter.Write(node, ", ");
                Visit(inc);
                i++;
            }
            CurrentTypeWriter.WriteLine(node, ")");
            if (!node.Statement.IsKind(SyntaxKind.Block))
                CurrentTypeWriter.WriteLine(node, "{", true);
            Visit(node.Statement);
            if (!node.Statement.IsKind(SyntaxKind.Block))
                CurrentTypeWriter.WriteLine(node, "}", true);
            CloseClosure();
        }

    }
}
