using NetJs.Translator.CSharpToJavascript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Translator.CSharpToJavascript
{
    public partial class TranslatorSyntaxVisitor
    {
        public bool TryDereference(CSharpSyntaxNode node)
        {
            if (!CurrentTypeWriter.EndsWith(Constants.RefValueName)) //skip if already derefed by the lower layer
            {
                CurrentTypeWriter.Write(node, ".");
                CurrentTypeWriter.Write(node, Constants.RefValueName);
                return true;
            }
            return false;
        }

        bool DereferenceIfReference(ExpressionSyntax node)
        {
            var expressionType = GetExpressionBoundTarget(node).TypeSyntaxOrSymbol as ISymbol;
            var refKind = expressionType?.GetRefKind();
            if (refKind != null && refKind != RefKind.None)
            {
                TryDereference(node);
                return true;
            }
            return false;
        }

        public void WriteCreateArrayRefOrPointer(CSharpSyntaxNode node, ITypeSymbol type, CodeNode arrayExpression, IEnumerable<CodeNode>? indexExpression)
        {
            WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateArrayReference", methodGenericTypes: [type], arguments: [arrayExpression, .. (indexExpression ?? Enumerable.Empty<CodeNode>())]);
            //var refStaticClass = (ITypeSymbol)_global.GetTypeSymbol("System.RefOrPointer", this);
            //var createMethod = (IMethodSymbol)refStaticClass.GetMembers("CreateFromArray").Single();
            //createMethod = createMethod.Construct(type);
            //WriteMethodInvocation(node, createMethod, null, null, [arrayExpression, .. indexExpression], null, null, false);
        }

        public void WriteCreateObjectRefOrPointer(CSharpSyntaxNode node, ITypeSymbol type, ExpressionSyntax objectTargetExpression)
        {
            //CurrentTypeWriter.Write(node, $"{{ get {Constants.RefValueName}(){{ return ");
            //Visit(objectTargetExpression);
            //CurrentTypeWriter.Write(node, $"; }}");
            //CurrentTypeWriter.Write(node, $", set {Constants.RefValueName}(v){{ ");
            //Visit(objectTargetExpression);
            //CurrentTypeWriter.Write(node, $" = v; }}");
            //CurrentTypeWriter.Write(node, $" }}");
            WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateObjectReference", methodGenericTypes: [type], arguments: [new CodeNode(() => {
                CurrentTypeWriter.Write(node, "() => ");
                Visit(objectTargetExpression);
            }),
            new CodeNode(() => {
                CurrentTypeWriter.Write(node, "($v) => ");
                if (objectTargetExpression.IsKind(SyntaxKind.ThisExpression))
                {
                    //The only time when c# allows this to be assigned is if it is a struct type.
                    //We clone the rhs into this
                    CurrentTypeWriter.Write(node, "$v.");
                    CurrentTypeWriter.Write(node, Constants.Clone);
                    CurrentTypeWriter.Write(node, "(");
                    Visit(objectTargetExpression);
                    CurrentTypeWriter.Write(node, ")");
                }
                else
                {
                    Visit(objectTargetExpression);
                    CurrentTypeWriter.Write(node, " = $v");
                }
            })]);
            //var refStaticClass = (ITypeSymbol)_global.GetTypeSymbol("System.RefOrPointer", this);
            //var createMethod = (IMethodSymbol)refStaticClass.GetMembers("CreateFromObject").Single();
            //createMethod = createMethod.Construct(type);
            //WriteMethodInvocation(node, createMethod, null, null, [objectTargetExpression], null, null, false);
        }

        public void WriteCreateRef(CSharpSyntaxNode node, ITypeSymbol type, string fieldName, string? prefix = null, string? suffix = null, bool _readOnly = false, bool inCurrentClosure = true)
        {
            if (inCurrentClosure)
            {
                CurrentTypeWriter.InsertAbove(node, () =>
                {
                    if (prefix != null)
                        CurrentTypeWriter.Write(node, prefix);
                    WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateObjectReference", methodGenericTypes: [type], arguments: [new CodeNode(() => {
                        CurrentTypeWriter.Write(node, "() => ");
                        CurrentTypeWriter.Write(node, fieldName);
                    }),
                    new CodeNode(() => {
                        CurrentTypeWriter.Write(node, "($v) => ");
                        CurrentTypeWriter.Write(node, fieldName);
                        CurrentTypeWriter.Write(node, " = $v");
                    })]);
                    if (suffix != null)
                        CurrentTypeWriter.Write(node, suffix);
                }, true);
            }
            else
            {
                if (prefix != null)
                    CurrentTypeWriter.Write(node, prefix);
                WriteMethodInvocation(node, "System.Runtime.CompilerServices.RuntimeHelpers.CreateObjectReference", methodGenericTypes: [type], arguments: [new CodeNode(() => {
                    CurrentTypeWriter.Write(node, "() => ");
                    CurrentTypeWriter.Write(node, fieldName);
                }),
                new CodeNode(() => {
                    CurrentTypeWriter.Write(node, "($v) => ");
                    CurrentTypeWriter.Write(node, fieldName);
                    CurrentTypeWriter.Write(node, " = $v");
                })]);
                if (suffix != null)
                    CurrentTypeWriter.Write(node, suffix);
            }
            //var str = $"{prefix}{{ get {Constants.RefValueName}(){{ return {fieldName}; }}";
            //if (!_readOnly)
            //{
            //    str += $", set {Constants.RefValueName}(v){{ {fieldName} = v; }}";
            //}
            //str += " }";
            //str += suffix;
            //if (inCurrentClosure)
            //    CurrentTypeWriter.InsertAbove(node, str, true);
            //else
            //    CurrentTypeWriter.Write(node, str, true);
        }

        public void WriteCreateRef(CSharpSyntaxNode node, ExpressionSyntax expression, ITypeSymbol? type/*, string? prefix = null, string? suffix = null, bool _readOnly = false*/)
        {
            type ??= _global.GetTypeSymbol(expression, this);
            if (expression.IsKind(SyntaxKind.ElementAccessExpression))
            {
                var element = ((ElementAccessExpressionSyntax)expression).Expression;
                var indexes = ((ElementAccessExpressionSyntax)expression).ArgumentList.Arguments.Select(e => new CodeNode(e));
                WriteCreateArrayRefOrPointer(node, type, element, indexes);
            }
            else
            {
                WriteCreateObjectRefOrPointer(node, type, expression);
            }
            //if (prefix != null)
            //    Writer.Write(node, prefix);
            //Writer.Write(node, $"{{ get {Constants.RefValueName}(){{ return ");
            //Visit(expression);
            //Writer.Write(node, $"; }}");
            //if (!_readOnly)
            //{
            //    Writer.Write(node, $", set {Constants.RefValueName}(v){{ ");
            //    if (expression.IsKind(SyntaxKind.ElementAccessExpression))
            //    {

            //    }
            //    else
            //    {
            //        Visit(expression);
            //    }
            //    Writer.Write(node, $" = v; }}");
            //}
            //Writer.Write(node, $" }}");
            //if (suffix != null)
            //    Writer.Write(node, suffix);
        }

        void WriteCreateRef(CSharpSyntaxNode node, Action expression, string? prefix = null, string? suffix = null, bool _readOnly = false)
        {
            CurrentTypeWriter.Write(node, $"{prefix}");
            CurrentTypeWriter.Write(node, $"{{ get {Constants.RefValueName}(){{ return ");
            expression();
            CurrentTypeWriter.Write(node, $"; }}");
            if (!_readOnly)
            {
                CurrentTypeWriter.Write(node, $", set {Constants.RefValueName}(v){{ ");
                expression();
                CurrentTypeWriter.Write(node, $" = v; }} ");
            }
            CurrentTypeWriter.Write(node, $"}}{suffix}");
        }

        public override void VisitRefExpression(RefExpressionSyntax node)
        {
            var refTarget = _global.GetSymbol(node.Expression, this);
            //Allows a type like string which is simply an array of chars on the heap to reference the firstChar and also able to increment the ref/pointer to other chars in the string
            if (node.Expression.IsKind(SyntaxKind.IdentifierName) &&
                refTarget is IFieldSymbol mfield &&
                mfield.RefKind == RefKind.None &&
                !mfield.IsStatic &&
                IsFieldStructLayout(null, mfield, out var fieldOffset, out var fieldSize))
            {
                WriteCreateArrayRefOrPointer(node, mfield.Type, new CodeNode(() =>
                {
                    CurrentTypeWriter.Write(node, $"this.{Constants.StructFieldsLayoutName}");
                }), [new CodeNode(() =>
                {
                    CurrentTypeWriter.Write(node, fieldOffset.ToString());
                })]);
            }
            else if (node.Expression.IsKind(SyntaxKind.FieldExpression) ||
                (refTarget is ILocalSymbol local && local.RefKind == RefKind.None) ||
                (refTarget is IFieldSymbol field && field.RefKind == RefKind.None) ||
                (refTarget is IParameterSymbol parameter && parameter.RefKind == RefKind.None))
            {
                WriteCreateRef(node, node.Expression, _global.GetTypeSymbol(refTarget));
            }
            //if we have an array ref expression like ref _array[byteIndex],
            //we need to create a ref than can read and write the array at the specified index
            else if (node.Expression is ElementAccessExpressionSyntax elementAccess)
            {
                var target = elementAccess.Expression;
                var index = elementAccess.ArgumentList.Arguments.Select(e => new CodeNode(e));
                ITypeSymbol? arrayElementType = null;
                var isArrayType = _global.GetTypeSymbol(target, this).IsArray(out arrayElementType);
                if (isArrayType && arrayElementType != null)
                {
                    WriteCreateArrayRefOrPointer(node, arrayElementType, target, index);
                    //var refStaticClass = (ITypeSymbol)_global.GetTypeSymbol("System.RefOrPointer", this);
                    //var createMethod = (IMethodSymbol)refStaticClass.GetMembers("CreateFromArray").Single();
                    //createMethod = createMethod.Construct(arrayElementType);
                    //WriteMethodInvocation(node, createMethod, null, null, [target, .. index], null, null, false);
                }
                else
                {
                    Visit(node.Expression);
                }
            }
            //if we have an array ref expression like ref *pointer,
            //we need to create a ref than can read and write the array at a specified index
            //A ref and pointer are implemented using the same runtime strutute though, so there are assignable and doesnt need any conversion
            else if (node.Expression is PrefixUnaryExpressionSyntax prefix && prefix.IsKind(SyntaxKind.PointerIndirectionExpression))
            {
                Visit(prefix.Operand);
                //var target = prefix.Operand;
                ////ITypeSymbol? objectType = _global.ResolveSymbol(GetExpressionReturnSymbol(target), this)!.GetTypeSymbol();
                ////if (objectType == null)
                ////throw new InvalidOperationException("Cannot infer refed type");
                ////WriteCreateRefOrPointer(node, objectType, target);
                //WriteCreateObjectRefOrPointer(node, target);
                ////var refStaticClass = (ITypeSymbol)_global.GetTypeSymbol("System.RefOrPointer", this);
                ////var createMethod = (IMethodSymbol)refStaticClass.GetMembers("CreateFromPointer").Single();
                ////createMethod = createMethod.Construct(objectType);
                ////WriteMethodInvocation(node, createMethod, null, null, [target], null, null, false);
            }
            else
            {
                Visit(node.Expression);
            }
            //base.VisitRefExpression(node);
        }
    }
}
