using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetJs.Translator.CSharpToJavascript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        bool HasYield(CSharpSyntaxNode node)
        {
            var kk = node.ToString();
            if (kk.Contains("Node? current = head;"))
            {
                if (kk.Contains("while (current != null)"))
                {
                    if (kk.Contains("yield return current._value;"))
                    {

                    }
                }
            }
            bool has = false;
            node.VisitHierachy((n, d) =>
            {
                if (n.IsKind(SyntaxKind.MethodDeclaration) && n != node)
                {
                    return false;
                }
                if (n.IsKind(SyntaxKind.LocalFunctionStatement) && n != node)
                {
                    return false;
                }
                var mhas = n.IsKind(SyntaxKind.YieldReturnStatement) || n.IsKind(SyntaxKind.YieldBreakStatement);
                if (mhas)
                {
                    has = true;
                    return false;
                }
                return true;
            });
            return has;
            //return node.DescendantNodes().Any(n => n.IsKind(SyntaxKind.YieldReturnStatement) || n.IsKind(SyntaxKind.YieldBreakStatement));
        }

        void TryWrapInYieldingGetEnumerable(CSharpSyntaxNode node, IEnumerable<TypeSyntax>? typeparameters, IEnumerable<SyntaxNode> body, bool isAsync = false)
        {
            if (HasYield(node))
            {
                bool isObjectEnumerable = false;
                bool isObjectEnumerator = false;
                ITypeSymbol? type = null;
                //if (typeparameters == null)
                //{
                var methodInfo = _global.GetSymbol(node, this) as IMethodSymbol;
                if (methodInfo != null)
                {
                    var returnType = methodInfo.ReturnType;
                    if (returnType.SpecialType == SpecialType.System_Collections_IEnumerable || returnType.IsEnumerable(out type))
                    {
                        isObjectEnumerable = true;
                    }
                    if (returnType.SpecialType == SpecialType.System_Collections_IEnumerator || returnType.IsEnumerator(out type))
                    {
                        isObjectEnumerator = true;
                    }
                }
                //if (!isObjectEnumerable && !isObjectEnumerator)
                //throw new InvalidOperationException("Type parameters are required for a yielding enumerable");
                //}
                bool bodyIsBlock = body.Count() == 1 && body.Single().IsKind(SyntaxKind.Block);
                if (bodyIsBlock)
                    CurrentTypeWriter.WriteLine(node, "{", true);
                var typeParameter = isObjectEnumerable || isObjectEnumerator ? (type ?? _global.SystemObject) : (ITypeSymbol)_global.GetSymbol(typeparameters.First(), this/*, out _, out _*/);
                var yieldClass = ((INamedTypeSymbol)_global.GetSymbol("NetJs.YieldToIterator<>", this)).Construct(typeParameter);
                var constructor = (IMethodSymbol)yieldClass.GetMembers(".ctor").Single();
                CurrentTypeWriter.Write(node, $"return ", true);
                WriteObjectCreation(node, null, yieldClass, constructor, [new CodeNode(() =>
                {
                    CurrentTypeWriter.WriteLine(node, $"{(isAsync ? "async " : "")}function*()");
                    if (!bodyIsBlock)
                        CurrentTypeWriter.WriteLine(node, "{", true);
                    VisitChildren(body);
                    if (!bodyIsBlock)
                        CurrentTypeWriter.Write(node, "}");
                    CurrentTypeWriter.Write(node, ".bind(this)");
                })], null);
                if (isObjectEnumerator)
                {
                    CurrentTypeWriter.Write(node, ".GetEnumerator()");
                }
                CurrentTypeWriter.WriteLine(node, ";");
                //var systemObject = (ITypeSymbol)_global.GetTypeSymbol("System.Object", this);
                //var genericArgs = $"({string.Join(", ", isObjectEnumerable || isObjectEnumerator ? [systemObject.ComputeOutputTypeName(_global)] : typeparameters.Select(parameter =>
                //{
                //    var symbol = _global.GetTypeSymbol(parameter, this/*, out _, out _*/);
                //    return symbol.ComputeOutputTypeName(_global);
                //}))})";
                //var metatada = _global.GetRequiredMetadata(yieldClass);
                //CurrentTypeWriter.WriteLine(node, $"return new {metatada.OverloadName}{genericArgs}({(isAsync ? "async " : "")}function*()", true);
                //if (!bodyIsBlock)
                //    CurrentTypeWriter.WriteLine(node, "{", true);
                //if (bodyIsBlock)
                //{
                //    VisitChildren(body.Single().ChildNodes());
                //}
                //else
                //{
                //VisitChildren(body);
                ////}
                //if (bodyIsBlock)
                //    CurrentTypeWriter.WriteLine(node, ")", true); //end return
                //else
                //    CurrentTypeWriter.WriteLine(node, "})", true); //end return
                //if (isObjectEnumerator)
                //{
                //    CurrentTypeWriter.WriteLine(node, ".GetEnumerator()", true);
                //}
                //CurrentTypeWriter.WriteLine(node, ";", true);
                if (bodyIsBlock)
                    CurrentTypeWriter.WriteLine(node, "}", true);
                return;
            }
            VisitChildren(body);
        }

        public override void VisitYieldStatement(YieldStatementSyntax node)
        {
            if (node.IsKind(SyntaxKind.YieldBreakStatement))
            {
                CurrentTypeWriter.WriteLine(node, $"return;", true);
            }
            else
            {
                CurrentTypeWriter.Write(node, $"yield ", true);
                Visit(node.Expression);
                CurrentTypeWriter.WriteLine(node, $";");
            }
            //base.VisitYieldStatement(node);
        }
    }
}
